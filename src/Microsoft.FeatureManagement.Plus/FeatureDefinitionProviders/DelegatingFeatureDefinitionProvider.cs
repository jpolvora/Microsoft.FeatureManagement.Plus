using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.Plus.FeatureDefinitionProviders
{

    public class DelegatingFeatureDefinitionProvider<TDelegate> : IFeatureDefinitionProvider
        where TDelegate : IFeatureDefinitionProvider
    {
        private readonly TDelegate featureDefinitionService;

        public DelegatingFeatureDefinitionProvider(TDelegate service)
        {
            this.featureDefinitionService = service;
        }

        public IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
        {
            return this.featureDefinitionService.GetAllFeatureDefinitionsAsync();
        }

        public Task<FeatureDefinition> GetFeatureDefinitionAsync(string featureName)
        {
            return this.featureDefinitionService.GetFeatureDefinitionAsync(featureName);
        }

    }
}