using System;
using Microsoft.Extensions.Logging;

namespace FeatureManagement.Providers.DbContextFeatureProvider.Impl
{
    public class FeatureFlagsDbContextFeatureProvider : DbContextFeatureProvider<FeatureFlagsDbContext, FeatureFlagsDbContextAcessor, FeatureEntity, FeatureTenantEntity>
    {
        public FeatureFlagsDbContextFeatureProvider(Func<FeatureFlagsDbContextAcessor> dbContextAcessor, ILogger logger)
            : base(dbContextAcessor, logger)
        {
        }
    }
}