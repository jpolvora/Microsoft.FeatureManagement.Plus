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
using Microsoft.Extensions.ObjectPool;
using Microsoft.FeatureManagement.Plus.Entities;
using Microsoft.FeatureManagement.Plus.Extensions;
using Microsoft.FeatureManagement.Plus.Options;

namespace Microsoft.FeatureManagement.Plus.Services
{
    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities")]
    public sealed class DbFeatureService : IFeatureService
    {
        private readonly string _connectionString;
        private readonly ILogger<DbFeatureService> _logger;
        private readonly string _tableName;
        private readonly ObjectPool<SqlConnection> _connectionPool;

        public DbFeatureService(IConfiguration configuration, IOptions<SqlFeatureDefinitionProviderOptions> options, ILogger<DbFeatureService> logger)
        {
            SqlFeatureDefinitionProviderOptions sqlFeatureDefinitionProviderOptions = options?.Value 
                ?? throw new ArgumentNullException(nameof(options));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _tableName = !string.IsNullOrWhiteSpace(sqlFeatureDefinitionProviderOptions.TableName) 
                ? sqlFeatureDefinitionProviderOptions.TableName 
                : "Features";
            
            // Basic validation to prevent SQL Injection via configuration
            if (!_tableName.Equals("Features", StringComparison.OrdinalIgnoreCase) && !System.Text.RegularExpressions.Regex.IsMatch(_tableName, "^[a-zA-Z0-9_]+$"))
            {
                 throw new ArgumentException("Invalid table name. Only alphanumeric characters and underscores are allowed.", nameof(options));
            }

            if (configuration != null)
            {
                _connectionString = configuration.GetConnectionString(sqlFeatureDefinitionProviderOptions.ConnectionStringName);
            }
            else
            {
                _connectionString = "";
            }

            var provider = new DefaultObjectPoolProvider();
            _connectionPool = provider.Create(new SqlConnectionPooledObjectPolicy(_connectionString));
        }

        public async Task<FeatureDefinition> GetFeatureDefinitionAsync(string featureName)
        {
            if (string.IsNullOrEmpty(featureName))
            {
                throw new ArgumentNullException(nameof(featureName));
            }

            LoggerDelegates.LogFeatureDefinitionLookup(_logger, featureName);

            SqlConnection connection = null;
            try
            {
                connection = await GetOpenConnectionAsync().ConfigureAwait(false);

                using (var command = new SqlCommand($"SELECT * FROM [{_tableName}] WHERE Id = @FeatureId", connection))
                {
                    command.Parameters.Add("@FeatureId", SqlDbType.NVarChar).Value = featureName;
                    
                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        if (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            return ConvertToFeatureDefinition(reader);
                        }
                    }
                }                
                throw new FeatureManagementException(FeatureManagementError.MissingFeature, $"Feature definition for {featureName} not found");
            }
            catch (Exception ex)
            {
                LoggerDelegates.LogFeatureDefinitionError(_logger, ex, featureName);
                throw new FeatureManagementException(FeatureManagementError.MissingFeature, $"Failed to retrieve feature definition for {featureName}");
            }
            finally
            {
                if (connection != null)
                {
                    _connectionPool.Return(connection);
                }
            }
        }

        public async IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
        {
            LoggerDelegates.LogFetchingAllFeatures(_logger);

            SqlConnection connection = null;
            try
            {
                connection = await GetOpenConnectionAsync().ConfigureAwait(false);

                using (var command = new SqlCommand($"SELECT * FROM [{_tableName}] ORDER BY Id", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            yield return ConvertToFeatureDefinition(reader);
                        }
                    }
                }
            }
            finally
            {
                if (connection != null)
                {
                    _connectionPool.Return(connection);
                }
            }
        }

        private async Task<SqlConnection> GetOpenConnectionAsync()
        {
            var connection = _connectionPool.Get();
            
            if (connection.State == ConnectionState.Broken)
            {
                connection.Close();
            }

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync().ConfigureAwait(false);
            }

            return connection;
        }

        private static FeatureDefinition ConvertToFeatureDefinition(SqlDataReader reader)
        {
            var id = reader["Id"]?.ToString();
            
            var enabledObj = reader["Enabled"];
            var enabled = enabledObj != DBNull.Value && Convert.ToBoolean(enabledObj, CultureInfo.CurrentCulture);
            
            var reqTypeObj = reader["RequirementType"];
            var requirementType = reqTypeObj != DBNull.Value ? Convert.ToInt32(reqTypeObj, CultureInfo.CurrentCulture) : 0;

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

        private class SqlConnectionPooledObjectPolicy : IPooledObjectPolicy<SqlConnection>
        {
            private readonly string _connectionString;

            public SqlConnectionPooledObjectPolicy(string connectionString)
            {
                _connectionString = connectionString;
            }

            public SqlConnection Create()
            {
                return new SqlConnection(_connectionString);
            }

            public bool Return(SqlConnection obj)
            {
                if (obj.State == ConnectionState.Broken)
                {
                    try { obj.Close(); } catch { }
                    return false;
                }
                return true;
            }
        }
    }
}