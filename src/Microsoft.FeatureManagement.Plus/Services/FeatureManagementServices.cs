using System;
using System.Collections.Generic;
using Microsoft.FeatureManagement.Plus.Patterns;

namespace Microsoft.FeatureManagement.Plus.Services
{

    public static class FeatureManagementServices
    {
        private static readonly object _lock = new object();

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
        private const string ServiceProviderNotSetMessage = "Service provider is not set.";

        public static T GetService<T>() where T : class
        {
            var serviceProvider = GetServiceProviderOrThrow();
            var service = serviceProvider.GetService(typeof(T)) as T;
            return service;
        }

        private static IServiceProvider GetServiceProviderOrThrow()
        {
            var serviceProvider = Cache<IServiceProvider>.Instance();
            if (serviceProvider == null)
            {
                throw new InvalidOperationException(ServiceProviderNotSetMessage);
            }

            return serviceProvider;
        }

        public static T GetRequiredService<T>() where T : class
        {
            T result = GetService<T>();
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
            lock (_lock)
            {
                Cache<IServiceProvider>.Lazy = new Lazy<IServiceProvider>(() => serviceProvider);
                return serviceProvider;
            }
        }

        private static class Cache<T>
        {
            public static Lazy<T> Lazy = new Lazy<T>();
            public static T Instance() => Lazy.Value;
        }
    }
}