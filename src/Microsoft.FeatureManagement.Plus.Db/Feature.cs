using System;

namespace Microsoft.FeatureManagement.Plus.Entities
{
    public class Feature : IFeatureEntity
    {
        public string Id { get; }
        public string Description { get; set; }
        public bool Enabled { get; set; }
        public string Filters { get; set; }
        public int RequirementType { get; set; }
        public DateTime? Modified { get; set; }

        public Feature(string id)
        {
            Id = id;
            Description = id;
        }

        public Feature()
        {

        }
    }
}