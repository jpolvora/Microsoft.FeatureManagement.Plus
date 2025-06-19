using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.Plus.Extensions;
using Microsoft.FeatureManagement.Plus.Options;

namespace Microsoft.FeatureManagement.Plus.Patterns
{
    public sealed class FeatureDefinitionProviderCacheDecorator : IGenericDecorator<IFeatureDefinitionProvider>, IFeatureDefinitionProvider, ICacheable, IDisposable
    {
        private readonly IMemoryCache _cache;
        private CancellationTokenSource _cacheResetTokenSource = new CancellationTokenSource();
        private readonly bool _shouldTrackCacheItemEviction;
        public IFeatureDefinitionProvider Target { get; }
        public ILogger<ICacheable> Logger { get; }

        public FeatureDefinitionProviderCacheDecorator(IFeatureDefinitionProvider target, IMemoryCache cache, ILogger<FeatureDefinitionProviderCacheDecorator> logger, IOptions<FeatureManagementPlusOptions> options)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            _shouldTrackCacheItemEviction = options.Value.TrackCacheItemEviction;
        }

        public Task<FeatureDefinition> GetFeatureDefinitionAsync(string featureName)
        {
            return _cache.ExecuteWithCache(GetCacheKey(featureName), async entry =>
            {
                // No need to assign to a local variable
                return await Target.GetFeatureDefinitionAsync(featureName).ConfigureAwait(false);
            }, Logger, _shouldTrackCacheItemEviction, _cacheResetTokenSource.Token);
        }

        public async IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
        {
            var features = await _cache.ExecuteWithCache(nameof(FeatureDefinitionProviderCacheDecorator), async entry =>
            {
                // Materialize to array to avoid multiple enumerations
                var list = new List<FeatureDefinition>();
                await foreach (var feature in Target.GetAllFeatureDefinitionsAsync().ConfigureAwait(false))
                {
                    list.Add(feature);
                }
                return list;
            }, Logger, _shouldTrackCacheItemEviction, _cacheResetTokenSource.Token).ConfigureAwait(false);

            foreach (var feature in features)
            {
                yield return feature;
            }
        }



        private static string GetCacheKey(string featureName)
        {
            return $"Features:{featureName}";
        }

        public void InvalidateCache()
        {
            this.TriggerTokenCancellation(ref _cacheResetTokenSource);
        }

        public void Dispose()
        {
            _cacheResetTokenSource.Dispose();
        }
    }
}