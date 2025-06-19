namespace Microsoft.FeatureManagement.Plus.Options
{
    public class SqlFeatureDefinitionProviderOptions
    {
        public const string SectionName = "SqlFeatureDefinitionProvider";
        public string ConnectionStringName { get; set; } = "FeatureManagement"; // Default connection string name
        public string TableName { get; set; } = "Features"; // Default table name
    }
}