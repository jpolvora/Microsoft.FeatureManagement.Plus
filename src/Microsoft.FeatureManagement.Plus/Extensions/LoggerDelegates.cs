using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.FeatureManagement.Plus.Extensions
{
    public static class LoggerDelegates
    {
        public static void LogCacheMiss(ILogger logger, string cacheKey)
        {
            if (logger != null && logger.IsEnabled(LogLevel.Debug))
            {
                LogCacheMissDelegate(logger, cacheKey, null);
            }
        }

        public static void LogCacheHit(ILogger logger, string cacheKey)
        {
            if (logger != null && logger.IsEnabled(LogLevel.Debug))
            {
                LogCacheHitDelegate(logger, cacheKey, null);
            }
        }


        public static void LogCacheEviction(ILogger logger, string cacheKey, string reason)
        {
            if (logger != null && logger.IsEnabled(LogLevel.Debug))
            {
                LogCacheEntryEvictionDelegate(logger, cacheKey, reason, null);
            }
        }

        public static void LogCacheResetTriggered(ILogger logger)
        {
            if (logger != null && logger.IsEnabled(LogLevel.Information))
            {
                LogCacheResetTriggeredDelegate(logger, null);
            }
        }

        private static readonly Action<ILogger, Exception> LogCacheResetTriggeredDelegate =
          LoggerMessage.Define(
              LogLevel.Information,
              new EventId(0, nameof(LogCacheResetTriggered)),
              "Cache reset triggered.");

        private static readonly Action<ILogger, string, Exception> LogCacheMissDelegate =
           LoggerMessage.Define<string>(
               LogLevel.Information,
               new EventId(0, nameof(LogCacheMiss)),
               "CacheMiss for key {Key}");

        private static readonly Action<ILogger, string, Exception> LogCacheHitDelegate =
         LoggerMessage.Define<string>(
             LogLevel.Information,
             new EventId(0, nameof(LogCacheHit)),
             "CacheHit for key {Key}");

        private static readonly Action<ILogger, string, string, Exception> LogCacheEntryEvictionDelegate =
            LoggerMessage.Define<string, string>(
          LogLevel.Information,
          new EventId(0, nameof(LogCacheEviction)),
          "Cache item {Key} removed due to {Reason}");


    }
}
