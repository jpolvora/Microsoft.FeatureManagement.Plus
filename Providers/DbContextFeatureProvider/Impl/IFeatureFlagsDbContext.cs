using System.Data.Entity;

namespace FeatureManagement.Providers.DbContextFeatureProvider.Impl
{
    public interface IFeatureFlagsDbContext
    {
        DbSet<FeatureEntity> Features { get; set; }
        DbSet<FeatureTenantEntity> FeaturesTenants { get; set; }
    }
}