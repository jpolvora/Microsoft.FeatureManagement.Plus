using System;
using System.Data.Entity;
using System.Linq;
using FeatureManagement.Providers.DbContextFeatureProvider.Impl;

namespace FeatureManagement.Providers.DbContextFeatureProvider
{
    public class DbContextAccessor<TContext> : IDbContextAccessor<TContext> where TContext : DbContext, IFeatureFlagsDbContext
    {
        protected readonly TContext _context;
        private bool disposedValue;

        public DbContextAccessor(TContext context)
        {
            _context = context;
        }

        public IQueryable<IFeatureEntity> GetFeaturesQuery()
        {
            return _context.Features.AsNoTracking().AsQueryable();
        }

        public IQueryable<IFeatureTenantEntity> GetFeaturesTenantsQuery()
        {
            return _context.FeaturesTenants.AsNoTracking().AsQueryable();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    _context?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DbContextAccessor()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}