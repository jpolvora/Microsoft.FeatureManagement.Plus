using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.Plus.FeatureDefinitionProviders
{
    public class CompositeFeatureDefinitionProvider : IFeatureDefinitionProvider, IEnumerable<IFeatureDefinitionProvider>
    {
        private readonly List<IFeatureDefinitionProvider> _providers;

        public CompositeFeatureDefinitionProvider(IEnumerable<IFeatureDefinitionProvider> providers)
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
        }

        public async IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
        {
            foreach (var provider in _providers)
            {
                await foreach (var feature in provider.GetAllFeatureDefinitionsAsync().ConfigureAwait(false))
                {
                    yield return feature;
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
                var featureDefinition = await provider.GetFeatureDefinitionAsync(featureName).ConfigureAwait(false);
                if (featureDefinition != null)
                {
                    return featureDefinition;
                }
            }
            return null;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<IFeatureDefinitionProvider> GetEnumerator() => _providers.GetEnumerator();
    }
}