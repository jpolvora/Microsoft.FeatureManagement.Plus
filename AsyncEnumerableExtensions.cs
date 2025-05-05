using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FeatureManagement
{
    public static class AsyncEnumerableExtensions
    {
        public static Task<IAsyncEnumerable<T>> ToTask<T>(this IAsyncEnumerable<T> source) => Task.FromResult(source);

        public static IAsyncEnumerable<T> FromTask<T>(this Task<IAsyncEnumerable<T>> asyncEnumerableTask)
            => asyncEnumerableTask.ToAsyncEnumerable().SelectMany(x => x);

    }
}