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
            builder.Configuration.AddJsonFile("features.json", optional: false, reloadOnChange: false);

            builder.Logging.AddConsole();

            IConfigurationRoot configuration = builder.Configuration;
            IServiceCollection services = builder.Services;

            services.AddSingletonFeatureManagementPlus(configuration, options =>
            {
                options.TrackCacheItemEviction = true;

            }).AddFeatureFilter<CustomFilter>();

            IHost host = builder.Build();

            //FeatureManagementServices.SetServiceProvider(serviceProvider);

            try
            {
                await host.StartAsync();
                await RunExample(host.Services);
                await host.StopAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static async Task RunExample(IServiceProvider provider)
        {
            // var cfg = provider.GetService<IConfiguration>();
            // var connectionStringName = cfg.GetValue<string>("FeatureManagementPlus:SqlFeatureDefinitionProvider:ConnectionStringName");
            // var connectionString = cfg.GetConnectionString(connectionStringName);
            // await using SqlConnection connection = new SqlConnection(connectionString);
            // connection.Open(); // Open the connection
            // var count = connection.ExecuteScalar<int>("select count(*) from features");
            // if (count == 0)
            // {
            //     //const string insertSql = "insert into Features(Id, Description, Enabled, Filters, RequirementType) values(@Id, @Description, @Enabled, @Filters, @RequirementType);";
            //
            //     //var rowsAffected = await connection.ExecuteAsync(insertSql, new { Id = dbFeature.Id, Description = dbFeature.Description, Enabled = true, Filters = dbFeature.Filters, RequirementType = dbFeature.RequirementType });
            // }


            var featureManager = provider.GetRequiredService<FeatureManager>();

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