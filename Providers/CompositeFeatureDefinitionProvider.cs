using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.FeatureManagement;

namespace FeatureManagement.Providers
{

    public class CompositeFeatureDefinitionProvider : IFeatureDefinitionProvider, IEnumerable<IFeatureDefinitionProvider>
    {
        private readonly IEnumerable<IFeatureDefinitionProvider> _providers;

        public CompositeFeatureDefinitionProvider(IEnumerable<IFeatureDefinitionProvider> providers)
        {
            if (providers == null)
            {
                throw new ArgumentNullException(nameof(providers));
            }

            //avoid self-reference
            _providers = providers.Where(p => p != null && p != this).ToList();
        }

        public IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
        {
            return _providers
                .Select(p => p.GetAllFeatureDefinitionsAsync())
                .Aggregate((current, next) => current.Concat(next));
        }

        public async Task<FeatureDefinition> GetFeatureDefinitionAsync(string featureName)
        {
            foreach (var provider in _providers)
            {
                var featureDefinition = await provider.GetFeatureDefinitionAsync(featureName);
                if (featureDefinition != null)
                {
                    return featureDefinition;
                }
            }

            return null;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<IFeatureDefinitionProvider> GetEnumerator()
        {
            return _providers.GetEnumerator();
        }
    }
}