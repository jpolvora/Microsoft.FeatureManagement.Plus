using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement.Plus.Extensions;
using Microsoft.FeatureManagement.Plus.FeatureDefinitionProviders;
using Microsoft.FeatureManagement.Plus.Services;

namespace Microsoft.FeatureManagement.Plus.Config
{
    public static class FeatureFlagsConfig
    {
        public static void Initialize()
        {
            ServiceProvider serviceProvider = InitializeFeatures();
            FeatureManagementServices.SetServiceProvider(serviceProvider);
        }

        private static ServiceProvider InitializeFeatures()
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonStream(FeatureManagementExtensions.GetTestFeature());
            IConfiguration configuration = builder.Build();

            IServiceCollection services = new ServiceCollection();

            services
                .AddSingleton(configuration)
                .AddLogging(lb => lb.AddDebug().SetMinimumLevel(LogLevel.Trace))
                .AddMemoryCache()
                .AddTransient(sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(ILogger)))
                //.AddSingleton<TelemetryClient>(sp => new TelemetryClient())
                .AddScopedFeatureManagement()
                //.AddFeatureFilter<CheckDatabaseFilter>()
                //.AddFeatureFilter<CustomFilter>()
                //.AddApplicationInsightsTelemetry()
                .Services
                .AddSingleton<ConfigurationFeatureDefinitionProvider>()
                .AddSingleton<DelegatingFeatureDefinitionProvider<SqlConnectionFeatureService>>()
                .AddSingleton<IFeatureDefinitionProvider>(sp =>
                {
                    var dbProvider = sp.GetRequiredService<DelegatingFeatureDefinitionProvider<SqlConnectionFeatureService>>()
                        .WithMemoryCache()
                        .WithLogging();

                    var cfgProvider = sp.GetRequiredService<ConfigurationFeatureDefinitionProvider>();

                    return new CompositeFeatureDefinitionProvider(new[] { dbProvider, cfgProvider });
                })
                .AddSingleton(sp => (CompositeFeatureDefinitionProvider)sp.GetRequiredService<IFeatureDefinitionProvider>());

            var serviceProviderOptions = new ServiceProviderOptions()
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            };

            ServiceProvider serviceProvider = services.BuildServiceProvider(serviceProviderOptions);

            return serviceProvider;
        }

        // private static IServiceCollection ConfigureLogging(this IServiceCollection services)
        // {
        //     if (bool.TryParse(System.Configuration.ConfigurationManager.AppSettings["IsProduction"], out bool isProduction) && !isProduction)
        //     {
        //         return services.AddLogging(options => options.AddDebug().SetMinimumLevel(LogLevel.Debug));
        //     }
        //
        //     return services.AddLogging();
        // }
    }
}