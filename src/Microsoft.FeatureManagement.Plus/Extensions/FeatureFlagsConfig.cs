using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement.Plus.FeatureDefinitionProviders;
using Microsoft.FeatureManagement.Plus.Options;
using Microsoft.FeatureManagement.Plus.Services;

namespace Microsoft.FeatureManagement.Plus.Extensions
{
    /// <summary>
    /// Extension methods for configuring Feature Management Plus.
    /// </summary>
    public static class FeatureFlagsConfig
    {
        /// <summary>
        /// Adds Feature Management Plus services as singletons.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="configureOptions">Optional configuration for FeatureManagementPlusOptions.</param>
        /// <returns>The feature management builder.</returns>
        public static IFeatureManagementBuilder AddSingletonFeatureManagementPlus(
            this IServiceCollection services,
            IConfiguration configuration,
            Func<IServiceProvider, IFeatureService> featureServiceFactory,
            Action<FeatureManagementPlusOptions> configureOptions = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.AddOptions<FeatureManagementPlusOptions>()
                .BindConfiguration(FeatureManagementPlusOptions.SectionName)
                .Configure(configureOptions ?? DefaultConfigureOptions)
                .ValidateOnStart();

            ConfigureLogging(services, configuration);

            services.AddMemoryCache();
            services.TryAddSingleton<ConfigurationFeatureDefinitionProvider>();
            services.AddOptions<FeatureManagementOptions>()
                .Configure(options =>
                {
                    options.IgnoreMissingFeatureFilters = true;
                    options.IgnoreMissingFeatures = false;
                });
            
            return services
                .AddSingleton<IFeatureService>(sp => featureServiceFactory(sp))
                .AddSingleton<DelegatingFeatureDefinitionProvider<IFeatureService>>()
                .RemoveAll<IFeatureDefinitionProvider>()
                .AddSingleton<IFeatureDefinitionProvider>(sp =>
                    new CompositeFeatureDefinitionProvider(new IFeatureDefinitionProvider[]
                    {
                        sp.GetRequiredService<ConfigurationFeatureDefinitionProvider>(),
                        sp.GetRequiredService<DelegatingFeatureDefinitionProvider<IFeatureService>>()
                            .WithMemoryCache(sp, configuration.GetValue<bool>(FeatureManagementPlusOptions.EnableMemoryCacheKey))
                            .WithLogging(sp, configuration.GetValue<bool>(FeatureManagementPlusOptions.EnableLoggingKey))
                    }, sp.GetService<ILogger<CompositeFeatureDefinitionProvider>>()))
                .AddFeatureManagement();
        }

        private static void DefaultConfigureOptions(FeatureManagementPlusOptions options)
        {
            options.AddDebug = false;            
        }

        private static void ConfigureLogging(IServiceCollection services, IConfiguration configuration)
        {
            var minLevel = configuration.GetValue("Logging:MinimumLevel", LogLevel.Trace);
            services.AddLogging(lb =>
            {
                if (configuration.GetValue<bool>(FeatureManagementPlusOptions.AddDebugKey))
                {
                    lb.AddDebug();
                }
                lb.SetMinimumLevel(minLevel);
            });
        }
    }
}