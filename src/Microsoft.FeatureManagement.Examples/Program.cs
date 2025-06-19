using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement.Plus.Extensions;

namespace Microsoft.FeatureManagement.Plus
{
    internal abstract class Program
    {
        public static async Task Main(string[] args)
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
            //loading configuration from features.json
            builder.Configuration.AddJsonFile("features.json", optional: false, reloadOnChange: false);
            builder.Logging.AddConsole();

            IConfigurationRoot configuration = builder.Configuration;
            IServiceCollection services = builder.Services;
            services
                .AddSingletonFeatureManagementPlus(configuration, options => { options.TrackCacheItemEviction = true; })
                .AddFeatureFilter<CustomFilter>();
            
            IHost host = builder.Build();

            await host.StartAsync();
            await RunExample(host.Services);
            await host.StopAsync();
        }

        private static async Task RunExample(IServiceProvider provider)
        {
            var featureManager = provider.GetRequiredService<FeatureManager>();

            //testing the feature manager on high concurrency demand

            var tasks = new List<Task>();
            for (int i = 0; i < 20; i++)
            {
                tasks.Add(Loop(featureManager)); // Directly add the async method
            }

            await Task.WhenAll(tasks);

            Console.WriteLine("All tasks completed.");
        }

        private static async Task Loop(FeatureManager featureManager)
        {
            var ctx = new CustomFilterContext();
            for (int i = 0; i < 10; i++)
            {
                ctx.ToggleReturnValue();
                await foreach (var featureName in featureManager.GetFeatureNamesAsync())
                {
                    var isEnabled = await featureManager.IsEnabledAsync(featureName, ctx);
                    Console.WriteLine($"Iteration: {i}:  Feature: {featureName}, Enabled: {isEnabled}");
                }
            }

            await featureManager.ResetCache();
        }
    }
}