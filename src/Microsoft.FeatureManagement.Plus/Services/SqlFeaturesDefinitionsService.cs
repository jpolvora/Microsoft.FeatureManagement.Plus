using System;
using System.Collections.Generic;
using System.Data;
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
    public class SqlFeaturesDefinitionsService : IFeaturesDefinitionsService
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlFeaturesDefinitionsService> _logger;
        private readonly string _tableName;

        public SqlFeaturesDefinitionsService(IConfiguration configuration, IOptions<FeatureManagementPlusOptions> options, ILogger<SqlFeaturesDefinitionsService> logger)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var opts = options.Value?.SqlFeatureDefinitionProvider;
            _tableName = !string.IsNullOrWhiteSpace(opts?.TableName) ? opts.TableName : "Features";
            string connectionStringName = opts?.ConnectionStringName;
            _connectionString = configuration.GetConnectionString(connectionStringName);
        }

        public async Task<FeatureDefinition> GetFeatureDefinitionAsync(string featureName)
        {
            if (string.IsNullOrEmpty(featureName))
            {
                throw new ArgumentNullException(nameof(featureName));
            }

            _logger.LogTrace("Going to database looking up feature definition for feature {featureName}", featureName);

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
                            return ConvertToFeatureDefinition(reader);
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
                        yield return ConvertToFeatureDefinition(reader);
                    }
                }
            }
        }

        private FeatureDefinition ConvertToFeatureDefinition(SqlDataReader reader)
        {
            var id = reader["Id"]?.ToString();
            var enabled = reader["Enabled"] != DBNull.Value && Convert.ToBoolean(reader["Enabled"]);
            var requirementType = reader["RequirementType"] != DBNull.Value ? Convert.ToInt32(reader["RequirementType"]) : 0;

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