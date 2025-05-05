using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Effort;
using FeatureManagement.Extensions;
using FeatureManagement.Filters;
using FeatureManagement.Providers;
using FeatureManagement.Providers.DbContextFeatureProvider;
using FeatureManagement.Providers.DbContextFeatureProvider.Impl;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.FeatureManagement;

namespace FeatureManagement
{
    public static class FeatureManagementExtensions
    {
        public static ServiceProvider InitializeFeatures()
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonStream(FeatureManagementExtensions.GetTestFeature());
            IConfiguration configuration = builder.Build();

            IServiceCollection services = new ServiceCollection();
            return InitializeFeatures(services, configuration);
        }

        public static ServiceProvider InitializeFeatures(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddSingleton<IConfiguration>(configuration)
                .AddLogging(options => options.AddDebug().SetMinimumLevel(LogLevel.Debug))
                .AddMemoryCache(options => options.TrackLinkedCacheEntries = true)
                .AddTransient<ILogger>(sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(ILogger)))
                .AddFeatureManagement()
                .AddFeatureFilter<TenantFilter>()
                .Services
                .AddSingleton<ConfigurationFeatureDefinitionProvider>()
                .AddTransient<DbConnection>(sp => DbConnectionFactory.CreatePersistent("Features"))
                .AddTransient<FeatureFlagsDbContext>()
                .AddTransient<FeatureFlagsDbContextAcessor>()
                .AddTransient<IDbContextAccessor<FeatureFlagsDbContext, FeatureEntity, FeatureTenantEntity>>(sp => sp.GetRequiredService<FeatureFlagsDbContextAcessor>())
                .AddSingleton<Func<FeatureFlagsDbContextAcessor>>(sp => sp.GetRequiredService<FeatureFlagsDbContextAcessor>)
                .AddSingleton<FeatureFlagsDbContextFeatureProvider>()
                .AddSingleton<IFeatureDefinitionProvider>(sp =>
                {
                    var dbProvider = sp.GetRequiredService<FeatureFlagsDbContextFeatureProvider>()
                    .WithMemoryCache()
                    .WithLogging();

                    var cfgProvider = sp.GetRequiredService<ConfigurationFeatureDefinitionProvider>();

                    return new CompositeFeatureDefinitionProvider(new[] { dbProvider, cfgProvider });
                })
                .AddSingleton<CompositeFeatureDefinitionProvider>(sp => (CompositeFeatureDefinitionProvider)sp.GetRequiredService<IFeatureDefinitionProvider>());

            var serviceProviderOptions = new ServiceProviderOptions()
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            };

            ServiceProvider serviceProvider = services.BuildServiceProvider(serviceProviderOptions);

            return serviceProvider;

        }

        private static readonly char[] separator = new[] { ',' };

        public static IFeatureDefinitionProvider WithMemoryCache(this IFeatureDefinitionProvider target)
            => new FeatureDefinitionProviderCacheDecorator(target, GlobalServices.GetRequiredService<IMemoryCache>(), GlobalServices.GetRequiredService<ILogger<FeatureDefinitionProviderCacheDecorator>>());

        public static IFeatureDefinitionProvider WithLogging(this IFeatureDefinitionProvider provider)
            => new FeatureDefinitionProviderLoggerDecorator(provider, GlobalServices.GetRequiredService<ILogger<FeatureDefinitionProviderLoggerDecorator>>());

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
                var featureDefinition = new FeatureDefinition()
                {
                    Name = feature.Id,
                    Status = feature.Enabled ? FeatureStatus.Conditional : FeatureStatus.Disabled,
                    RequirementType = feature.RequirementType == 1 ? RequirementType.All
                                                                   : RequirementType.Any
                };

                //MetadataManager<FeatureDefinition>.Set(featureDefinition, "ProviderName", providerName);

                var filters = new List<FeatureFilterConfiguration>();

                if (!string.IsNullOrEmpty(feature.Filters))
                {
                    foreach (var filter in feature.Filters.Split(separator, StringSplitOptions.RemoveEmptyEntries).Distinct())
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
            catch (Exception)
            {
                return null;
            }
        }

        //[Obsolete("This method is obsolete. Use IsEnabledAsync instead.")]
        public static bool IsEnabled(this IFeatureManager manager, string feature)
        {
            // This is a workaround to avoid deadlocks when calling async methods from sync context
            // in a non-async method. It is not the best practice, but it works for this case.
            // wrap call into a try catch
            // and log the exception
            try
            {
                Func<Task<bool>> isEnabledFunc = () => manager.IsEnabledAsync(feature);
                bool isEnabled = isEnabledFunc.RunSync();
                return isEnabled;
            }
            catch (Exception ex)
            {
                //logger.LogError(ex, "Error while checking feature {Feature}", feature);
                throw new FeatureManagementException("Error while checking feature {Feature}", ex);
            }

        }

        //[Obsolete("This method is obsolete. Use IsEnabledAsync instead.")]
        public static bool IsEnabled<TContext>(this IFeatureManager manager, string feature, TContext context)
        {
            try
            {
                Func<Task<bool>> isEnabledFunc = () => manager.IsEnabledAsync(feature, context);
                bool isEnabled = isEnabledFunc.RunSync();
                return isEnabled;
            }
            catch (Exception ex)
            {

                //logger.LogError(ex, "Error while checking feature {Feature} with TContext parameter", feature);
                throw new FeatureManagementException("Error while checking feature {Feature} with TContext parameter", ex);
            }
        }

        public static async Task ResetCache(this FeatureManager featureManager)
        {
            var providers = GlobalServices.GetRequiredService<CompositeFeatureDefinitionProvider>();
            foreach (IFeatureDefinitionProvider provider in providers)
            {
                await RecursivelyClearCache(provider);
            }


            foreach (IFeatureFilterMetadata filter in featureManager.FeatureFilters.ToArray())
            {
                if (typeof(ICacheManager).IsAssignableFrom(filter.GetType()))
                {
                    ((ICacheManager)filter).ExpireAllCacheItems();
                }
            }
        }

        private static async Task RecursivelyClearCache(this IFeatureDefinitionProvider provider)
        {
            if (typeof(ICacheManager).IsAssignableFrom(provider.GetType()))
            {
                ((ICacheManager)provider).ExpireAllCacheItems();
            }

            if (typeof(IGenericDecorator<IFeatureDefinitionProvider>).IsAssignableFrom(provider.GetType()))
            {
                var providerDecorated = provider as IGenericDecorator<IFeatureDefinitionProvider>;
                if (providerDecorated != null)
                {
                    await RecursivelyClearCache(providerDecorated.Target);
                }
            }
        }

        public static async Task<T> ExecuteWithCache<T>(this IMemoryCache cache, string cacheKey, Func<ICacheEntry, Task<T>> factory, ILogger logger, CancellationToken token)
        {
            bool cacheMiss = false;
            T result = await cache.GetOrCreateAsync(cacheKey, entry =>
            {
                cacheMiss = true;
                TrackCacheEntry(entry, logger, token);
                return factory(entry);
            });

            logger.LogDebug("Cache {CacheStatus} for {CacheKey}", cacheMiss ? "miss" : "hit", cacheKey);

            return result;
        }

        private static void TrackCacheEntry(ICacheEntry entry, ILogger logger, CancellationToken token)
        {
            entry.AddExpirationToken(new CancellationChangeToken(token))
                .RegisterPostEvictionCallback(CreateCacheItemRemovedCallback(logger));
        }

        private static PostEvictionDelegate CreateCacheItemRemovedCallback(ILogger logger)
        {
            return (object key, object value, EvictionReason reason, object state) =>
            {
                logger.LogDebug("Cache item {Key} removed due to {Reason}", key, reason);
            };
        }

        public static void TriggerTokenCancellation(this ILogger<ICacheManager> logger, ref CancellationTokenSource cacheResetTokenSource)
        {
            using (var tokenSource = Interlocked.Exchange(ref cacheResetTokenSource, new CancellationTokenSource()))
            {
                //Cancelling the old token will trigger cache items expiration and observers will be notified
                CancellationTokenRegistration reg = default;
                reg = tokenSource.Token.Register(() =>
                {
                    logger.LogDebug("Cache reset token cancelled");
                    reg.Dispose();
                });

                tokenSource.Cancel();
            }

            logger.LogDebug("Cache reset triggered");
        }

    }
}