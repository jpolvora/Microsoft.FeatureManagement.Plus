using System;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.Plus
{
    [FilterAlias("volatile")]
    public class CustomFilter : IContextualFeatureFilter<CustomFilterContext>
    {
        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext featureFilterContext, CustomFilterContext appContext)
        {
            // Custom logic to evaluate the feature filter
            // For example, you can check some condition in appContext or featureFilterContext
            // Here we just return true for demonstration purposes
            if (appContext == null)
            {
                throw new ArgumentNullException(nameof(appContext), "CustomFilterContext cannot be null");
            }
            // Example condition: always return true
            return Task.FromResult(appContext.Result);
        }
    }


    public class CustomFilterContext
    {
        public bool Result { get; set; }

        public void ToggleReturnValue()
        {
            Result = !Result;

        }
    }
}