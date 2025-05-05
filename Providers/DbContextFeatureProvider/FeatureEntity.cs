using System;

namespace FeatureManagement.Providers.DbContextFeatureProvider
{

    public class FeatureEntity : IFeatureEntity
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public bool Enabled { get; set; }
        public string Filters { get; set; }
        public int RequirementType { get; set; }
        public DateTime? Modified { get; set; }
    }
}