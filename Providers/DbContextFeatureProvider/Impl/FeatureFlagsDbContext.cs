using System.Data.Common;
using System.Data.Entity;

namespace FeatureManagement.Providers.DbContextFeatureProvider.Impl
{
    public class FeatureFlagsDbContext : DbContext, IFeatureFlagsDbContext<FeatureEntity, FeatureTenantEntity>
    {
        public FeatureFlagsDbContext(DbConnection connection) : base(connection, false)
        {

        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FeatureEntity>()
                .ToTable("Features")
                .HasKey(x => x.Id);

            modelBuilder.Entity<FeatureTenantEntity>()
                .ToTable("FeaturesTenants")
                .HasKey(x => new { x.FeatureId, x.TenantId });

        }

        public DbSet<FeatureEntity> Features { get; set; }
        public DbSet<FeatureTenantEntity> FeaturesTenants { get; set; }

    }
}