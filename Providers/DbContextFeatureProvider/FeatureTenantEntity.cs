using System;

namespace FeatureManagement.Providers.DbContextFeatureProvider
{
    public class FeatureTenantEntity : IFeatureTenantEntity
    {
        public string FeatureId { get; set; }
        public Guid TenantId { get; set; }
        public bool Enabled { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }
    }
}