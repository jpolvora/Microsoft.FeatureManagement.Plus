using System;
using System.Threading.Tasks;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement.Plus.Extensions;
using Microsoft.FeatureManagement.Plus.Services;

namespace Microsoft.FeatureManagement.Plus
{
    internal abstract class Program
    {
        public static async Task Main(string[] args)
        {


            var builder = Host.CreateApplicationBuilder(args);
            builder.Configuration.AddJsonFile("features.json", optional: false, reloadOnChange: false);
            builder.Configuration.AddEnvironmentVariables();
            builder.Logging.AddConsole();


            IConfigurationRoot configuration = builder.Configuration;
            IServiceCollection services = builder.Services;

            services.AddSingleton<DbFeatureService>();

            services
                .AddSingletonFeatureManagementPlus(configuration,
                    sp => sp.GetRequiredService<DbFeatureService>(),
                    options => { options.TrackCacheItemEviction = true; })
                .AddFeatureFilter<CustomFilter>();

            services.AddHostedService<TimedHostedService>();

            //end of registration, now build the host

            using IHost host = builder.Build();
            await host.StartAsync();

            using (var scope = host.Services.CreateScope())
            {
                var featureManager = scope.ServiceProvider.GetRequiredService<FeatureManager>();
                await RunFeatureManagerDemo(featureManager);
            }

            var token = host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
            await host.WaitForShutdownAsync(token);

            Console.WriteLine("Shutdown ?");
        }

        private static async Task RunFeatureManagerDemo(FeatureManager featureManager)
        {
            var tasks = new Task[20];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Loop(featureManager);
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