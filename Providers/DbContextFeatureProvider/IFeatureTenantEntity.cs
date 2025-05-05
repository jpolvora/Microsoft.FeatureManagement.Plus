using System;

namespace FeatureManagement.Providers.DbContextFeatureProvider
{
    public interface IFeatureTenantEntity
    {
        string FeatureId { get; set; }
        Guid TenantId { get; set; }
        bool Enabled { get; set; }
        DateTime Created { get; set; }
        DateTime? Modified { get; set; }
    }
}