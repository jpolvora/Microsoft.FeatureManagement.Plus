using Microsoft.Extensions.Logging;

namespace Microsoft.FeatureManagement.Plus.Patterns
{
    public interface ICacheObjects
    {
        /// <summary>
        /// Expires all cache items.
        /// </summary>
        /// <returns></returns>
        void InvalidateCache();

        ILogger<ICacheObjects> Logger { get; }
    }
}