using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.FeatureManagement.Plus.Entities;
using Microsoft.FeatureManagement.Plus.FeatureDefinitionProviders;
using Microsoft.FeatureManagement.Plus.Options;
using Microsoft.FeatureManagement.Plus.Patterns;
using Microsoft.FeatureManagement.Plus.Services;

namespace Microsoft.FeatureManagement.Plus.Extensions
{
    public static class FeatureManagementExtensions
    {

        private static readonly Action<ILogger, Exception> _logCacheResetTriggered =
            LoggerMessage.Define(
                LogLevel.Information,
                new EventId(0, nameof(LogCacheResetTriggered)),
                "Cache reset triggered.");

        private static readonly Action<ILogger, string, Exception> _logCacheMiss =
           LoggerMessage.Define<string>(
               LogLevel.Information,
               new EventId(0, nameof(LogCacheMiss)),
               "CacheMiss for key {Key}");

        private static readonly Action<ILogger, string, Exception> _logCacheHit =
         LoggerMessage.Define<string>(
             LogLevel.Information,
             new EventId(0, nameof(LogCacheHit)),
             "CacheHit for key {Key}");

        private static readonly Action<ILogger, string, string, Exception> _logCacheEntryEviction =
            LoggerMessage.Define<string, string>(
          LogLevel.Information,
          new EventId(0, nameof(LogCacheEviction)),
          "Cache item {Key} removed due to {Reason}");


        private static readonly char[] Separator = new[] { ',' };

        public static IFeatureDefinitionProvider WithMemoryCache(this IFeatureDefinitionProvider target, bool enabled, IServiceProvider sp) => enabled
            ? new FeatureDefinitionProviderCacheDecorator(
                target,
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<ILogger<FeatureDefinitionProviderCacheDecorator>>(),
                sp.GetRequiredService<IOptions<FeatureManagementPlusOptions>>())
            : target;

        public static T WithLogging<T>(this T target, IServiceProvider sp)
            where T : class, IFeatureDefinitionProvider
            => new FeatureDefinitionProviderLoggerDecorator(
                target,
                sp.GetRequiredService<ILogger<FeatureDefinitionProviderLoggerDecorator>>()).As<T>();


        public static T As<T>(this object instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance), "Instance cannot be null.");
            }

            if (instance is T typedInstance)
            {
                return typedInstance;
            }

            throw new InvalidCastException($"Cannot cast {instance.GetType().FullName} to {typeof(T).FullName}.");
        }

        public static Stream GetTestFeature()
        {
            string json = @"
            {
              ""AllowedHosts"": ""*"",            
              ""feature_management"": {
                ""feature_flags"": [
                  {
                    ""id"": ""TestFeature"",
                    ""enabled"": true,       
                    ""conditions"": {
                        ""client_filters"": [
                        {
                          ""name"": ""CheckDatabase""                       
                        }
                      ]
                    }
                  }               
                ]
              }
            }";

            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }

        public static FeatureDefinition MapToFeatureDefinition(this IFeatureEntity feature)
        {
            try
            {
                if (feature == null)
                {
                    throw new ArgumentNullException(nameof(feature));
                }

                var featureDefinition = new FeatureDefinition()
                {
                    Name = feature.Id,
                    Status = feature.Enabled ? FeatureStatus.Conditional : FeatureStatus.Disabled,
                    RequirementType = feature.RequirementType == 1
                        ? RequirementType.All
                        : RequirementType.Any
                };

                var filters = new List<FeatureFilterConfiguration>();

                if (!string.IsNullOrEmpty(feature.Filters))
                {
                    foreach (var filter in feature.Filters.Split(Separator, StringSplitOptions.RemoveEmptyEntries).Distinct())
                    {
                        filters.Add(new FeatureFilterConfiguration()
                        {
                            Name = filter.Trim()
                        });
                    }
                }

                featureDefinition.EnabledFor = filters;

                return featureDefinition;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to map feature entity '{feature?.Id}' to FeatureDefinition.", ex);
            }
        }

        private static readonly FieldInfo featureDefinitionProviderField = typeof(FeatureManager).GetField("_featureDefinitionProvider", BindingFlags.NonPublic | BindingFlags.Instance);

        public static async Task ResetCache(this FeatureManager featureManager)
        {
            if (featureManager == null)
            {
                throw new ArgumentNullException(nameof(featureManager), "FeatureManager cannot be null.");
            }

            IFeatureDefinitionProvider privateProviderValue = featureDefinitionProviderField?.GetValue(featureManager) as IFeatureDefinitionProvider;
            CompositeFeatureDefinitionProvider composite = privateProviderValue as CompositeFeatureDefinitionProvider;
            if (composite == null)
            {
                return;
            }

            foreach (IFeatureDefinitionProvider provider in composite)
            {
                await provider.RecursivelyClearCache().ConfigureAwait(false);
            }


            foreach (IFeatureFilterMetadata filter in featureManager.FeatureFilters.ToArray())
            {
                if (typeof(ICacheObjects).IsAssignableFrom(filter.GetType()))
                {
                    ((ICacheObjects)filter).InvalidateCache();
                }
            }
        }

        private static Task RecursivelyClearCache(this IFeatureDefinitionProvider provider)
        {
            while (true)
            {
                if (provider is ICacheObjects manager)
                {
                    manager.InvalidateCache();
                }

                if (provider is IGenericDecorator<IFeatureDefinitionProvider> decorator)
                {
                    provider = decorator.Target;
                    continue;
                }


                break;
            }

            return Task.CompletedTask;
        }

        public static async Task<T> ExecuteWithCache<T>(this IMemoryCache cache, string cacheKey, Func<ICacheEntry, Task<T>> factory, ILogger logger, Nullable<CancellationToken> token = null)
        {
            bool cacheMiss = false;
            T result = await cache.GetOrCreateAsync(cacheKey, entry =>
            {
                cacheMiss = true;
                if (token != null && token.HasValue)
                {
                    TrackCacheEntry(entry, logger, token.Value);
                }

                return factory(entry);

            }).ConfigureAwait(false);

            if (cacheMiss)
            {
                LogCacheMiss(logger, cacheKey);
            }
            else
            {
                LogCacheHit(logger, cacheKey);
            }

            return result;
        }

        private static void LogCacheMiss(ILogger logger, string cacheKey)
        {
            if (logger != null && logger.IsEnabled(LogLevel.Debug))
            {
                _logCacheMiss(logger, cacheKey, null);
            }
        }

        private static void LogCacheHit(ILogger logger, string cacheKey)
        {
            if (logger != null && logger.IsEnabled(LogLevel.Debug))
            {
                _logCacheHit(logger, cacheKey, null);
            }
        }


        private static void LogCacheEviction(ILogger logger, string cacheKey, string reason)
        {
            if (logger != null && logger.IsEnabled(LogLevel.Debug))
            {
                _logCacheEntryEviction(logger, cacheKey, reason, null);
            }
        }

        private static void TrackCacheEntry(ICacheEntry entry, ILogger logger, CancellationToken token)
            => entry.AddExpirationToken(new CancellationChangeToken(token))
                .RegisterPostEvictionCallback(CreateCacheItemRemovedCallback(logger));


        private static PostEvictionDelegate CreateCacheItemRemovedCallback(ILogger logger)
        {
            return (key, value, reason, state) => LogCacheEviction(logger, key.ToString(), reason.ToString());
        }

        public static void TriggerTokenCancellation(this ICacheObjects instance, ref CancellationTokenSource cacheResetTokenSource)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            using (CancellationTokenSource tokenSource = Interlocked.Exchange(ref cacheResetTokenSource, new CancellationTokenSource()))
            {
                tokenSource.Cancel();
            }

            LogCacheResetTriggered(instance.Logger);
        }




        public static void LogCacheResetTriggered(ILogger logger)
        {
            if (logger != null && logger.IsEnabled(LogLevel.Information))
            {
                _logCacheResetTriggered(logger, null);
            }
        }

        public static IFeatureManagementBuilder AddSingletonFeatureManagementPlus(this IServiceCollection services, IConfiguration configuration, Action<FeatureManagementPlusOptions> configureOptions = null)
        {

            services.AddOptions<FeatureManagementPlusOptions>()
                .BindConfiguration(FeatureManagementPlusOptions.SectionName)
                .Configure(configureOptions ??= options =>
                {
                    options.AddDebug = false;
                    options.SqlFeatureDefinitionProvider.ConnectionStringName = "DefaultConnection"; // Default connection string name
                    options.SqlFeatureDefinitionProvider.TableName = "Features";
                });

            services.AddLogging(lb =>
            {
                var addDebug = configuration.GetValue<bool>(FeatureManagementPlusOptions.AddDebugKey);
                if (addDebug)
                {
                    lb.AddDebug();
                }
                lb.SetMinimumLevel(LogLevel.Trace);
            });

            services.AddMemoryCache();
            services.TryAddSingleton<ConfigurationFeatureDefinitionProvider>();
            services.AddOptions<FeatureManagementOptions>()
                .Configure(options =>
            {
                options.IgnoreMissingFeatureFilters = true; // Ignore missing feature filters
                options.IgnoreMissingFeatures = true; // Ignore missing features
            });
            return services
                .AddSingleton<SqlFeaturesDefinitionsService>()
                .AddSingleton<IFeaturesDefinitionsService>(sp => sp.GetRequiredService<SqlFeaturesDefinitionsService>().WithLogging<IFeaturesDefinitionsService>(sp))
                .AddSingleton<DelegatingFeatureDefinitionProvider<IFeaturesDefinitionsService>>()
                .RemoveAll<IFeatureDefinitionProvider>()
                .AddSingleton<IFeatureDefinitionProvider>(sp =>
                new CompositeFeatureDefinitionProvider(new IFeatureDefinitionProvider[]
                {
                    sp.GetRequiredService<ConfigurationFeatureDefinitionProvider>(),
                    sp
                    .GetRequiredService<DelegatingFeatureDefinitionProvider<IFeaturesDefinitionsService>>()
                    .WithMemoryCache(configuration.GetValue<bool>(FeatureManagementPlusOptions.EnableMemoryCacheKey), sp)
                    .WithLogging(sp)
                }))
                .AddFeatureManagement();


        }
    }
}
