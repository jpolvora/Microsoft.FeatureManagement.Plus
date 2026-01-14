using System;

namespace Microsoft.FeatureManagement.Plus.Entities
{
    public class FeatureTenant : IFeatureTenantEntity
    {
        public FeatureTenant()
        {

        }

        public FeatureTenant(string featureId)
        {
            FeatureId = featureId;
        }

        public string FeatureId { get; set; }
        public Guid TenantId { get; set; }
        public bool Enabled { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }
    }
}