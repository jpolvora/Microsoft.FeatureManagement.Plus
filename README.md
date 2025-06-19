# Microsoft.FeatureManagement.Plus

**FeatureManagement.Plus** is a C# library that extends Microsoft.FeatureManagement library, with robust result/error handling, flexible feature definition providers, caching, and integrated logging support.

## Features
- **CompositeFeatureDefinitionProvider**  
  Allows combining multiple feature definition providers, enabling fallback and prioritization strategies.

- **SQL Database as Source of FeatureDefintions and ContextualFeatureFilter evaluation**  
    Provides a SQL-based implementation for storing and evaluating feature definitions and contextual filters, allowing for dynamic feature management.

- **Result and Fault Types**  
  Encapsulate operation outcomes as `Result` (success/failure) and `Fault` (error details), reducing exception-based control flow.

- **Generic Result Support**  
  Use `Result<T>` to return values or errors from methods, making APIs more expressive and safer.

- **Feature Definition Providers**  
  Easily plug in custom or built-in `FeatureDefinitionProvider` implementations to load feature flags from various sources (e.g., configuration, database, remote services).

- **Caching**  
  Built-in caching mechanisms to reduce overhead and improve performance when resolving feature definitions and states.

- **Logging Integration**  
  All key operations support structured logging via `ILogger`, making it easy to trace feature evaluation and error handling.

- **Utility Methods**  
  Helpers for executing actions/functions with automatic error capture, async support, and more.

## Basic Usage

```csharp
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
            
            // Registering the FeatureManagerPlus with custom options and filters
            
            services
                .AddSingletonFeatureManagementPlus(configuration, options => 
                {
                    options.TrackCacheItemEviction = true;
                })
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

```

## Feature Definition Providers

Register and use custom or built-in providers to load feature definitions:

```csharp
IFeatureDefinitionProvider provider = new ConfigurationFeatureDefinitionProvider(configuration);
```

## Caching

Feature definitions and states are cached for performance. You can configure cache duration and invalidation as needed.

## Logging

All major operations accept an `ILogger` for diagnostics:

```csharp
await (() => SomeAsyncMethod())
    .ExecuteWithLogger(logger, throwError: true);
```

## Async Support

```csharp
Result<int> result = await Result.TryAwait(async () => await SomeAsyncMethod());
```

## License

This project is licensed under the MIT License.

---

For more details, see the source code and inline documentation.