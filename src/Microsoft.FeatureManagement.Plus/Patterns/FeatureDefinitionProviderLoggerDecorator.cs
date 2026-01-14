using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement.Plus.Services;

namespace Microsoft.FeatureManagement.Plus.Patterns
{
    [SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates")]
    public class FeatureDefinitionProviderLoggerDecorator : IGenericDecorator<IFeatureDefinitionProvider>, IFeatureService
    {
        private readonly ILogger<FeatureDefinitionProviderLoggerDecorator> _logger;

        public IFeatureDefinitionProvider Target { get; }

        public FeatureDefinitionProviderLoggerDecorator(IFeatureDefinitionProvider target, ILogger<FeatureDefinitionProviderLoggerDecorator> logger)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<FeatureDefinition> GetFeatureDefinitionAsync(string featureName)
        {
            try
            {
                _logger.LogDebug("Getting feature definition for '{FeatureName}'", featureName);
                var result = await Target.GetFeatureDefinitionAsync(featureName).ConfigureAwait(false);
                _logger.LogDebug("Feature definition for '{FeatureName}' {Result}", featureName, result != null ? "found" : "not found");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feature definition for '{FeatureName}'", featureName);
                throw;
            }
        }

        public async IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
        {
            _logger.LogDebug("Getting all feature definitions");
            IAsyncEnumerable<FeatureDefinition> features;
            try
            {
                features = Target.GetAllFeatureDefinitionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all feature definitions");
                throw;
            }

            var enumerator = features.GetAsyncEnumerator();
            try
            {
                bool hasNext = true;
                while (hasNext)
                {
                    FeatureDefinition feature = null;
                    try
                    {
                        hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);
                        if (hasNext)
                        {
                            feature = enumerator.Current;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error enumerating feature definitions");
                        throw;
                    }

                    if (feature != null)
                    {
                        yield return feature;
                    }
                }
            }
            finally
            {
                await enumerator.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}