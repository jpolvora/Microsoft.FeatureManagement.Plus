namespace FeatureManagement.Providers.DbContextFeatureProvider.Impl
{
    public class FeatureFlagsDbContextAcessor : DbContextAccessor<FeatureFlagsDbContext>
    {
        public FeatureFlagsDbContextAcessor(FeatureFlagsDbContext context) : base(context)
        {
        }
    }
}