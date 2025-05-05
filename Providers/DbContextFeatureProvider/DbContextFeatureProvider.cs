using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using FeatureManagement.Providers.DbContextFeatureProvider.Impl;
using FeatureManagement.ResultPattern;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace FeatureManagement.Providers.DbContextFeatureProvider
{
    public abstract class DbContextFeatureProvider<TContext, TAcessor> : IFeatureDefinitionProvider
            where TContext : DbContext, IFeatureFlagsDbContext
            where TAcessor : IDbContextAccessor<TContext>
    {

        private readonly Func<TAcessor> dbContextAccessor;
        protected readonly ILogger Logger;

        protected DbContextFeatureProvider(Func<TAcessor> dbContextAcessor, ILogger logger)
        {
            dbContextAccessor = dbContextAcessor;
            Logger = logger;
        }

        public IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
            => Result.TryAsyncEnumerable(() => LoadFromDbAsync()
            .ToAsyncEnumerable()
            .SelectMany(f => f.ToAsyncEnumerable()))
            .AsyncValue();

        protected virtual async Task<IEnumerable<FeatureDefinition>> LoadFromDbAsync()
        {
            try
            {
                using (var context = dbContextAccessor())
                {
                    List<IFeatureEntity> features = await context.GetFeaturesQuery()
                                      .OrderBy(x => x.Id)
                                      .ToListAsync()
                                      .ConfigureAwait(false);


                    IEnumerable<FeatureDefinition> result = features.Select(f => f.MapToFeatureDefinition());

                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error loading feature definitions from database: {Error}", ex.Message);
                return Enumerable.Empty<FeatureDefinition>();
            }
        }

        public async Task<FeatureDefinition> GetFeatureDefinitionAsync(string featureName)
        {
            try
            {
                using (var context = dbContextAccessor())
                {

                    IFeatureEntity feature = await context.GetFeaturesQuery()
                                      .FirstOrDefaultAsync(f => f.Id == featureName)
                                      .ConfigureAwait(false);

                    if (feature == null)
                    {
                        Logger.LogWarning("Feature with name {FeatureName} not found in database.", featureName);
                        return null;
                    }

                    var result = feature.MapToFeatureDefinition();

                    Logger.LogDebug("");

                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error loading feature definition {feature} from database: {Error}", featureName, ex.Message);
                return null;
            }
        }
    }

}