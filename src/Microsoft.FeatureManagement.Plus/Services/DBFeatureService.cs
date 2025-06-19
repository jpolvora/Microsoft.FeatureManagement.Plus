using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.Plus.Entities;
using Microsoft.FeatureManagement.Plus.Extensions;
using Microsoft.FeatureManagement.Plus.Options;

namespace Microsoft.FeatureManagement.Plus.Services
{
    [SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates")]
    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities")]
    public sealed class DbFeatureService : IFeatureService
    {
        private readonly string _connectionString;
        private readonly ILogger<DbFeatureService> _logger;
        private readonly string _tableName;

        public DbFeatureService(IConfiguration configuration, IOptions<SqlFeatureDefinitionProviderOptions> options, ILogger<DbFeatureService> logger)
        {
            SqlFeatureDefinitionProviderOptions sqlFeatureDefinitionProviderOptions = options != null
                ? options.Value
                : throw new ArgumentNullException(nameof(options));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _tableName = !string.IsNullOrWhiteSpace(sqlFeatureDefinitionProviderOptions.TableName) ? sqlFeatureDefinitionProviderOptions.TableName : "Features";
            _connectionString = configuration != null 
                ? configuration.GetConnectionString(sqlFeatureDefinitionProviderOptions.ConnectionStringName) 
                : throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<FeatureDefinition> GetFeatureDefinitionAsync(string featureName)
        {
            if (string.IsNullOrEmpty(featureName))
            {
                throw new ArgumentNullException(nameof(featureName));
            }

            _logger.LogTrace("Going to database looking up feature definition for feature {FeatureName}", featureName);

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand($"SELECT * FROM {_tableName} WHERE Id = @FeatureId", connection))
                {
                    command.Parameters.Add(new SqlParameter("@FeatureId", SqlDbType.NVarChar) { Value = featureName });
                    await connection.OpenAsync().ConfigureAwait(false);
                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        if (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            return DbFeatureService.ConvertToFeatureDefinition(reader);
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feature definition {FeatureName} from database", featureName);
                throw new FeatureManagementException(FeatureManagementError.MissingFeature, $"Failed to retrieve feature definition for {featureName}");
            }
        }

        public async IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
        {
            _logger.LogTrace("Fetching all feature definitions from database");

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand($"SELECT * FROM {_tableName} order by Id", connection))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        yield return DbFeatureService.ConvertToFeatureDefinition(reader);
                    }
                }
            }
        }

        private static FeatureDefinition ConvertToFeatureDefinition(SqlDataReader reader)
        {
            var id = reader["Id"]?.ToString();
            var enabled = reader["Enabled"] != DBNull.Value && Convert.ToBoolean(reader["Enabled"], CultureInfo.CurrentCulture);
            var requirementType = reader["RequirementType"] != DBNull.Value ? Convert.ToInt32(reader["RequirementType"], CultureInfo.CurrentCulture) : 0;

            IFeatureEntity entity = new Feature(id)
            {
                Description = reader["Description"]?.ToString() ?? id,
                Modified = reader["Modified"] as DateTime?,
                Enabled = enabled,
                RequirementType = requirementType,
                Filters = reader["Filters"]?.ToString() ?? string.Empty
            };

            return entity.MapToFeatureDefinition();
        }
    }
}