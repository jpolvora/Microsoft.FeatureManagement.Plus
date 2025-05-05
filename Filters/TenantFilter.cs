using System;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FeatureManagement.Providers.DbContextFeatureProvider;
using FeatureManagement.Providers.DbContextFeatureProvider.Impl;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace FeatureManagement.Filters
{
    [FilterAlias("CheckDatabase")]
    public class TenantFilter : IContextualFeatureFilter<TenantFilterContext>, ICacheManager
    {
        private readonly IDbContextAccessor<FeatureFlagsDbContext> accessor;
        private readonly IMemoryCache cache;
        private readonly ILogger<TenantFilter> logger;
        private CancellationTokenSource cacheResetTokenSource = new CancellationTokenSource();

        public TenantFilter(IDbContextAccessor<FeatureFlagsDbContext> accessor, IMemoryCache memoryCache, ILogger<TenantFilter> logger)
        {
            this.accessor = accessor;
            this.cache = memoryCache;
            this.logger = logger;
        }

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext featureFilterContext, TenantFilterContext appContext)
        {
            var cacheKey = GetCacheKey(featureFilterContext.FeatureName, appContext.TenantId);

            return cache.ExecuteWithCache(cacheKey, entry => this.accessor
                .GetFeaturesTenantsQuery()
                .Where(x => x.FeatureId == featureFilterContext.FeatureName && x.TenantId == appContext.TenantId)
                .AnyAsync(),
                this.logger, cacheResetTokenSource.Token);
        }


        private static string GetCacheKey(string featureName, Guid tenantId)
        {
            return $"TenantFilter:{featureName}:{tenantId}";
        }


        public void Expire()
        {
            this.logger.TriggerTokenCancellation(ref cacheResetTokenSource);
        }

    }
}