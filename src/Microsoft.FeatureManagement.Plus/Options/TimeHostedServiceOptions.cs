namespace Microsoft.FeatureManagement.Plus.Options
{
    public class TimeHostedServiceOptions
    {
        public int IntervalInSeconds { get; set; } = 10; // Default interval for the hosted service
        public int TimeoutSeconds { get; set; } = 10; // Default timeout for the hosted service
        public int MaxRetryAttempts { get; set; } = 5;

    }
}