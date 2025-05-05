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
    public abstract class ContextFilterBase<TContext, TDbContext, TFeature, TFeatureTenant> : IContextualFeatureFilter<TContext>
        where TContext : class, ITenantFilter
        where TDbContext : DbContext, IFeatureFlagsDbContext<TFeature, TFeatureTenant>
        where TFeature : class, IFeatureEntity
        where TFeatureTenant : class, IFeatureTenantEntity
    {

        protected readonly IDbContextAccessor<TDbContext, TFeature, TFeatureTenant> accessor;
        protected readonly IMemoryCache cache;
        protected readonly ILogger<TenantFilter> logger;
        protected CancellationTokenSource cacheResetTokenSource = new CancellationTokenSource();

        protected ContextFilterBase(IDbContextAccessor<TDbContext, TFeature, TFeatureTenant> accessor, IMemoryCache cache, ILogger<TenantFilter> logger)
        {
            this.accessor = accessor;
            this.cache = cache;
            this.logger = logger;
        }

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext featureFilterContext, TContext appContext)
        {
            var cacheKey = GetCacheKey(featureFilterContext.FeatureName, appContext);

            return cache.ExecuteWithCache(cacheKey, entry => this.accessor
                .GetFeaturesTenantsQuery()
                .Where(x => x.FeatureId == featureFilterContext.FeatureName && x.TenantId == appContext.TenantId)
                .AnyAsync(),
                this.logger, cacheResetTokenSource.Token);
        }

        protected abstract string GetCacheKey(string featureName, TContext context);


        public void ExpireAllCacheItems()
        {
            this.logger.TriggerTokenCancellation(ref cacheResetTokenSource);
        }
    }
}