namespace Microsoft.FeatureManagement.Plus.Options
{
    public class FeatureManagementPlusOptions
    {
        public const string SectionName = "FeatureManagementPlus";
        public const string AddDebugKey = SectionName + ":AddDebug";
        public const string EnableMemoryCacheKey = SectionName + ":EnableMemoryCache";

        public SqlFeatureDefinitionProviderOptions SqlFeatureDefinitionProvider { get; set; } = new SqlFeatureDefinitionProviderOptions();
        public bool AddDebug { get; set; } = false;

        public bool EnableMemoryCache { get; set; } = true; // Default to true for backward compatibility

        public bool TrackCacheItemEviction { get; set; } = false; // Default to false for backward compatibility
    }
}