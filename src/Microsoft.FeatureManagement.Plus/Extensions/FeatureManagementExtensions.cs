using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.FeatureManagement.Plus.Entities;
using Microsoft.FeatureManagement.Plus.FeatureDefinitionProviders;
using Microsoft.FeatureManagement.Plus.Options;
using Microsoft.FeatureManagement.Plus.Patterns;

namespace Microsoft.FeatureManagement.Plus.Extensions
{
    public static class FeatureManagementExtensions
    {

        private static readonly char[] Separator = new[] { ',' };

        public static IFeatureDefinitionProvider WithMemoryCache(this IFeatureDefinitionProvider target, IServiceProvider sp, bool enabled)
            => enabled
            ? new FeatureDefinitionProviderCacheDecorator(
                target,
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<ILogger<FeatureDefinitionProviderCacheDecorator>>(),
                sp.GetRequiredService<IOptions<FeatureManagementPlusOptions>>())
            : target;

        public static IFeatureDefinitionProvider WithLogging(this IFeatureDefinitionProvider target, IServiceProvider sp, bool enabled)
            => enabled ?
            new FeatureDefinitionProviderLoggerDecorator(
                target,
                sp.GetRequiredService<ILogger<FeatureDefinitionProviderLoggerDecorator>>())
            : target;



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
                if (typeof(ICacheable).IsAssignableFrom(filter.GetType()))
                {
                    ((ICacheable)filter).InvalidateCache();
                }
            }
        }

        private static Task RecursivelyClearCache(this IFeatureDefinitionProvider provider)
        {
            while (true)
            {
                if (provider is ICacheable manager)
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

        public static async Task<T> ExecuteWithCache<T>(this IMemoryCache cache, string cacheKey, Func<ICacheEntry, Task<T>> factory, ILogger logger, bool trackCacheItems, CancellationToken token)
        {
            bool cacheMiss = false;
            T result = await cache.GetOrCreateAsync(cacheKey, entry =>
            {
                cacheMiss = true;
                if (trackCacheItems)
                {
                    entry.AddExpirationToken(new CancellationChangeToken(token))
                    .RegisterPostEvictionCallback(CreateCacheItemRemovedCallback(logger));
                }

                return factory(entry);

            }).ConfigureAwait(false);

            if (cacheMiss)
            {
                LoggerDelegates.LogCacheMiss(logger, cacheKey);
            }
            else
            {
                LoggerDelegates.LogCacheHit(logger, cacheKey);
            }

            return result;
        }

        private static PostEvictionDelegate CreateCacheItemRemovedCallback(ILogger logger)
        {
            return (key, value, reason, state) => LoggerDelegates.LogCacheEviction(logger, key.ToString(), reason.ToString());
        }

        public static void TriggerTokenCancellation(this ICacheable instance, ref CancellationTokenSource cacheResetTokenSource)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            using (CancellationTokenSource tokenSource = Interlocked.Exchange(ref cacheResetTokenSource, new CancellationTokenSource()))
            {
                tokenSource.Cancel();
            }

            LoggerDelegates.LogCacheResetTriggered(instance.Logger);
        }
    }
}
