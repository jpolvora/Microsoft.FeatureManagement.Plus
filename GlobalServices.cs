using System;
using System.Collections.Generic;
using FeatureManagement.ResultPattern;
using Microsoft.FeatureManagement;

namespace FeatureManagement
{
    public class GlobalServices
    {
        private GlobalServices() { }

        public static T Get<T>()
        {
            return Cache<T>.Instance();
        }

        /// <summary>
        /// Gets the service of type T from the service provider.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static T GetService<T>() where T : class
        {
            IServiceProvider serviceProvider = Cache<IServiceProvider>.Instance();
            var result = serviceProvider == null
                ? throw new InvalidOperationException("Service provider is not set.")
                : serviceProvider.GetService(typeof(T)) as T;

            return result;
        }

        public static T GetRequiredService<T>() where T : class
        {
            var result = GetService<T>();
            return result ?? throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
        }

        public static IEnumerable<T> GetServices<T>() where T : class
        {
            var result = GetService<IEnumerable<T>>();
            return result;
        }

        public static FeatureManager GetFeatureManager()
        {
            return Result.Try(() => GetService<FeatureManager>());
        }

        public static Lazy<T> Set<T>(Func<T> factory) => Cache<T>.Lazy = new Lazy<T>(factory);

        public static IServiceProvider SetServiceProvider(IServiceProvider serviceProvider)
        {
            Cache<IServiceProvider>.Lazy = new Lazy<IServiceProvider>(() => serviceProvider);
            return serviceProvider;
        }

        private static class Cache<T>
        {
            public static Lazy<T> Lazy;
            public static T Instance() => Lazy != null ? Lazy.Value : default;
        }
    }

}