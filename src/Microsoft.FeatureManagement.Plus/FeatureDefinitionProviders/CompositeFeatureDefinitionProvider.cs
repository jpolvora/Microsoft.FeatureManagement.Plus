using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.FeatureManagement.Plus.FeatureDefinitionProviders
{
    public class CompositeFeatureDefinitionProvider : IFeatureDefinitionProvider, IEnumerable<IFeatureDefinitionProvider>
    {
        private readonly List<IFeatureDefinitionProvider> _providers;
        private readonly ILogger<CompositeFeatureDefinitionProvider> _logger;

        public CompositeFeatureDefinitionProvider(IEnumerable<IFeatureDefinitionProvider> providers, ILogger<CompositeFeatureDefinitionProvider> logger = null)
        {
            if (providers == null)
            {
                throw new ArgumentNullException(nameof(providers));
            }

            // Avoid self-reference and nulls, and materialize to a list for efficient enumeration
            _providers = new List<IFeatureDefinitionProvider>();
            foreach (var p in providers)
            {
                if (p != null && p != this)
                {
                    _providers.Add(p);
                }
            }
            _logger = logger;
        }

        public async IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
        {
            foreach (var provider in _providers)
            {
                IAsyncEnumerator<FeatureDefinition> enumerator = null;
                try
                {
                    enumerator = provider.GetAllFeatureDefinitionsAsync().GetAsyncEnumerator();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to create enumerator for provider {ProviderType}", provider.GetType().Name);
                    continue;
                }

                if (enumerator != null)
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
                            _logger?.LogError(ex, "Error enumerating features from provider {ProviderType}", provider.GetType().Name);
                            hasNext = false; // Stop this provider on error
                        }

                        if (feature != null)
                        {
                            yield return feature;
                        }
                    }

                    await enumerator.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task<FeatureDefinition> GetFeatureDefinitionAsync(string featureName)
        {
            if (string.IsNullOrEmpty(featureName))
            {
                throw new ArgumentNullException(nameof(featureName));
            }

            foreach (var provider in _providers)
            {
                try
                {
                    var featureDefinition = await provider.GetFeatureDefinitionAsync(featureName).ConfigureAwait(false);
                    if (featureDefinition != null)
                    {
                        return featureDefinition;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error retrieving feature '{FeatureName}' from provider {ProviderType}", featureName, provider.GetType().Name);
                    // Continue to next provider
                }
            }
            return null;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<IFeatureDefinitionProvider> GetEnumerator() => _providers.GetEnumerator();
    }
}