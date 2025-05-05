using System;

namespace FeatureManagement.Filters
{
    public interface ITenantFilter
    {
        Guid TenantId { get; set; }
    }
}