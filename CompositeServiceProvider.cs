using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace FeatureManagement
{
    public class CompositeServiceProvider : IServiceProvider
    {
        private readonly IEnumerable<IServiceProvider> _serviceProviders;
        private readonly TraceSource _trace;

        public CompositeServiceProvider(IEnumerable<IServiceProvider> serviceProviders, TraceSource trace = null)
        {
            if (serviceProviders == null)
            {
                throw new ArgumentNullException(nameof(serviceProviders));
            }

            _serviceProviders = serviceProviders;
            _trace = trace;
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            foreach (var provider in _serviceProviders)
            {
                var service = provider.GetService(serviceType);
                if (service != null)
                {
                    _trace?.TraceEvent(TraceEventType.Information, 0,
                        $"Service type {serviceType.FullName} resolved by provider {provider.GetType().FullName}.");
                    return service;
                }
                else
                {
                    _trace?.TraceEvent(TraceEventType.Verbose, 0,
                        $"Service type {serviceType.FullName} not found in provider {provider.GetType().FullName}.");
                }
            }

            // Optional fallback via ActivatorUtilities
            try
            {
                _trace?.TraceEvent(TraceEventType.Warning, 0,
                    $"Service type {serviceType.FullName} not found. Attempting fallback via ActivatorUtilities.");

                return ActivatorUtilities.CreateInstance(this, serviceType);
            }
            catch (Exception ex)
            {
                _trace?.TraceEvent(TraceEventType.Error, 0,
                    $"ActivatorUtilities fallback failed for {serviceType.FullName}: {ex.Message}");
            }

            return null;
        }
    }
}