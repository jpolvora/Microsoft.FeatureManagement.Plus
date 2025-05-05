using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FeatureManagement.ResultPattern;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace FeatureManagement.Providers
{

    public class FeatureDefinitionProviderLoggerDecorator : IGenericDecorator<IFeatureDefinitionProvider>, IFeatureDefinitionProvider
    {
        private readonly ILogger logger;

        public IFeatureDefinitionProvider Target { get; }

        public FeatureDefinitionProviderLoggerDecorator(IFeatureDefinitionProvider target, ILogger logger)
        {
            this.Target = target;
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<FeatureDefinition> GetFeatureDefinitionAsync(string featureName)
        {
            Func<Task<FeatureDefinition>> taskFunctor = () => Target.GetFeatureDefinitionAsync(featureName);
            Task<FeatureDefinition> result = taskFunctor.ExecuteWithLogger(logger, throwError: false);
            return result;
        }


        public IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
        {
            Func<Task<IAsyncEnumerable<FeatureDefinition>>> taskFunctor = () => Target.GetAllFeatureDefinitionsAsync().ToTask();
            IAsyncEnumerable<FeatureDefinition> result = taskFunctor.ExecuteWithLogger(logger, throwError: false).FromTask();
            return result;
        }

    }
}
