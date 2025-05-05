using System;
using System.Data.Entity;
using System.Linq;
using FeatureManagement.Providers.DbContextFeatureProvider.Impl;

namespace FeatureManagement.Providers.DbContextFeatureProvider
{
    /// <summary>
    /// Provides access to the DbContext restricted to the feature flags entities.
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public interface IDbContextAccessor<TContext> : IDisposable
        where TContext : DbContext, IFeatureFlagsDbContext
    {
        IQueryable<IFeatureEntity> GetFeaturesQuery();

        IQueryable<IFeatureTenantEntity> GetFeaturesTenantsQuery();
    }
}