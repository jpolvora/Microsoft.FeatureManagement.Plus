using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.Plus.Extensions
{
    public static class TaskExtensions
    {
        public static T RunSync<T>(this Func<CancellationToken, Task<T>> taskFactory, CancellationToken token)
        {
            if (taskFactory == null) throw new ArgumentNullException(nameof(taskFactory));
            SynchronizationContext originalContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            try
            {
                return taskFactory(token).GetAwaiter().GetResult();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(originalContext);
            }
        }

        public static T RunSync<T>(this Func<Task<T>> taskFactory)
        {
            if (taskFactory == null) throw new ArgumentNullException(nameof(taskFactory));
            SynchronizationContext originalContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            try
            {
                return taskFactory().GetAwaiter().GetResult();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(originalContext);
            }
        }


        public static void RunSync(this Func<CancellationToken, Task> taskFactory, CancellationToken token)
        {
            if (taskFactory == null) throw new ArgumentNullException(nameof(taskFactory));
            SynchronizationContext originalContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            try
            {
                taskFactory(token).GetAwaiter().GetResult();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(originalContext);
            }
        }

        public static void RunSync(this Func<Task> taskFactory)
        {
            if (taskFactory == null) throw new ArgumentNullException(nameof(taskFactory));
            SynchronizationContext originalContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            try
            {
                taskFactory().GetAwaiter().GetResult();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(originalContext);
            }
        }
    }
}