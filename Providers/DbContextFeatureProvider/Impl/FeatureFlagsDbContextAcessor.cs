namespace FeatureManagement.Providers.DbContextFeatureProvider.Impl
{
    public class FeatureFlagsDbContextAcessor : DbContextAccessor<FeatureFlagsDbContext, FeatureEntity, FeatureTenantEntity>
    {
        public FeatureFlagsDbContextAcessor(FeatureFlagsDbContext context) : base(context)
        {
        }
    }
}