using System;

namespace Microsoft.FeatureManagement.Plus.Entities
{
    public interface IFeatureEntity
    {
        string Id { get; }
        string Description { get; }
        bool Enabled { get; }
        string Filters { get; }
        int RequirementType { get; }
        DateTime? Modified { get; }
    }
}