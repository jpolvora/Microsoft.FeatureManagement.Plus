using Microsoft.Extensions.Logging;

namespace Microsoft.FeatureManagement.Plus.Patterns
{
    public interface ICacheable
    {
        /// <summary>
        /// Expires all cache items.
        /// </summary>
        /// <returns></returns>
        void InvalidateCache();

        ILogger<ICacheable> Logger { get; }
    }
}