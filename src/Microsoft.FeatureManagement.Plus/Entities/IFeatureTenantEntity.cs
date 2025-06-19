using System;

namespace Microsoft.FeatureManagement.Plus.Entities
{
    public interface IFeatureTenantEntity
    {
        string FeatureId { get; }
        Guid TenantId { get; }
        bool Enabled { get; }
        DateTime Created { get; }
        DateTime? Modified { get; }
    }
}