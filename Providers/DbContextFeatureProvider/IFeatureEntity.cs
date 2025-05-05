using System;

namespace FeatureManagement.Providers.DbContextFeatureProvider
{
    public interface IFeatureEntity
    {
        string Id { get; set; }
        string Description { get; set; }
        bool Enabled { get; set; }
        string Filters { get; set; }
        int RequirementType { get; set; }
        DateTime? Modified { get; set; }
    }
}