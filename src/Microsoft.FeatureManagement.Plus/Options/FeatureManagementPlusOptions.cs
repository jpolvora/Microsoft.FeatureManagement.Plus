namespace Microsoft.FeatureManagement.Plus.Options
{
    public class FeatureManagementPlusOptions
    {
        public const string SectionName = "FeatureManagementPlus";
        public const string AddDebugKey = SectionName + ":AddDebug";
        public const string EnableMemoryCacheKey = SectionName + ":EnableMemoryCache";
        public const string EnableLoggingKey = SectionName + ":EnableLogging";        
        public bool AddDebug { get; set; }
        public bool EnableMemoryCache { get; set; } = true; // Default to true for backward compatibility
        public bool EnableLogging { get; set; }
        public bool TrackCacheItemEviction { get; set; }
    }
}