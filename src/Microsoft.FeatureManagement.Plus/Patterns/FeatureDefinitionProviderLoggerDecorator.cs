using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement.Plus.Services;

namespace Microsoft.FeatureManagement.Plus.Patterns
{
    public class FeatureDefinitionProviderLoggerDecorator : IGenericDecorator<IFeatureDefinitionProvider>, IFeatureDefinitionProvider, IFeaturesDefinitionsService
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

            await foreach (var feature in features.ConfigureAwait(false))
            {
                yield return feature;
            }
        }
    }
}