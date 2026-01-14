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


        private static readonly Action<ILogger, string, Exception> LogFeatureDefinitionLookupDelegate =
            LoggerMessage.Define<string>(
                LogLevel.Trace,
                new EventId(100, nameof(LogFeatureDefinitionLookup)),
                "Going to database looking up feature definition for feature {FeatureName}");

        private static readonly Action<ILogger, string, Exception> LogFeatureDefinitionErrorDelegate =
            LoggerMessage.Define<string>(
                LogLevel.Error,
                new EventId(101, nameof(LogFeatureDefinitionError)),
                "Error retrieving feature definition {FeatureName} from database");

        private static readonly Action<ILogger, Exception> LogFetchingAllFeaturesDelegate =
            LoggerMessage.Define(
                LogLevel.Trace,
                new EventId(102, nameof(LogFetchingAllFeatures)),
                "Fetching all feature definitions from database");

        public static void LogFeatureDefinitionLookup(ILogger logger, string featureName)
        {
             if (logger.IsEnabled(LogLevel.Trace))
             {
                 LogFeatureDefinitionLookupDelegate(logger, featureName, null);
             }
        }

        public static void LogFeatureDefinitionError(ILogger logger, Exception ex, string featureName)
        {
             if (logger.IsEnabled(LogLevel.Error))
             {
                 LogFeatureDefinitionErrorDelegate(logger, featureName, ex);
             }
        }

        public static void LogFetchingAllFeatures(ILogger logger)
        {
             if (logger.IsEnabled(LogLevel.Trace))
             {
                 LogFetchingAllFeaturesDelegate(logger, null);
             }
        }
    }
}
