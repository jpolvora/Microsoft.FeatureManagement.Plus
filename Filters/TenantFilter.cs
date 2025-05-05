using FeatureManagement.Providers.DbContextFeatureProvider;
using FeatureManagement.Providers.DbContextFeatureProvider.Impl;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace FeatureManagement.Filters
{

    [FilterAlias("CheckDatabase")]
    public class TenantFilter : ContextFilterBase<TenantFilterContext, FeatureFlagsDbContext, FeatureEntity, FeatureTenantEntity>, ICacheManager
    {
        public TenantFilter(IDbContextAccessor<FeatureFlagsDbContext, FeatureEntity, FeatureTenantEntity> accessor, IMemoryCache cache, ILogger<TenantFilter> logger)
            : base(accessor, cache, logger)
        {
        }

        protected override string GetCacheKey(string featureName, TenantFilterContext context)
        {
            return $"TenantFilter:{featureName}:{context.TenantId}";
        }

    }
}