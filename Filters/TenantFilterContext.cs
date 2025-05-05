using System;

namespace FeatureManagement.Filters
{
    public class TenantFilterContext : ITenantFilter
    {
        public TenantFilterContext(Guid tenantId)
        {
            TenantId = tenantId;
        }

        public Guid TenantId { get; set; }
    }
}