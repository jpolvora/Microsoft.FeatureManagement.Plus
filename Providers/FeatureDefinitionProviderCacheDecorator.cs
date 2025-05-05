using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace FeatureManagement.Providers
{
    public class FeatureDefinitionProviderCacheDecorator : IGenericDecorator<IFeatureDefinitionProvider>, IFeatureDefinitionProvider, ICacheManager
    {
        private readonly IMemoryCache cache;
        private readonly ILogger<ICacheManager> logger;
        private CancellationTokenSource cacheResetTokenSource = new CancellationTokenSource();

        public IFeatureDefinitionProvider Target { get; }

        public FeatureDefinitionProviderCacheDecorator(IFeatureDefinitionProvider target, IMemoryCache cache, ILogger<FeatureDefinitionProviderCacheDecorator> logger)
        {
            this.Target = target;
            this.cache = cache;
            this.logger = logger;
        }

        public Task<FeatureDefinition> GetFeatureDefinitionAsync(string featureName)
        {
            return this.cache.ExecuteWithCache(GetCacheKey(featureName), async entry =>
            {
                var featureDefinition = await Target.GetFeatureDefinitionAsync(featureName);
                return featureDefinition;
            }, this.logger, cacheResetTokenSource.Token);
        }

        public IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
        {
            return this.cache.ExecuteWithCache(GetCacheKey("all"), async entry =>
            {
                var featureDefinitions = await Target.GetAllFeatureDefinitionsAsync().ToListAsync();
                return featureDefinitions;
            }, this.logger, cacheResetTokenSource.Token)
                .ToAsyncEnumerable()
                .SelectMany(features => features.ToAsyncEnumerable());
        }


        private static string GetCacheKey(string featureName)
        {
            return $"Features:{featureName}";
        }


        public void ExpireAllCacheItems()
        {
            this.logger.TriggerTokenCancellation(ref cacheResetTokenSource);
        }

    }
}

