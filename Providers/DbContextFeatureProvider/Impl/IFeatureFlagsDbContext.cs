using System.Data.Entity;

namespace FeatureManagement.Providers.DbContextFeatureProvider.Impl
{
    public interface IFeatureFlagsDbContext<TFeature, TFeatureTenant>
        where TFeature : class, IFeatureEntity
        where TFeatureTenant : class, IFeatureTenantEntity
    {
        DbSet<TFeature> Features { get; set; }
        DbSet<TFeatureTenant> FeaturesTenants { get; set; }
    }
}