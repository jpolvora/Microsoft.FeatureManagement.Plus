using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FeatureManagement.ResultPattern
{
    public static class ResultExtensions
    {
        public static async Task<T> TaskValue<T>(this Task<Result<T>> resultTask) => await resultTask.ConfigureAwait(false);

        public static IAsyncEnumerable<T> AsyncValue<T>(this Result<IAsyncEnumerable<T>> result) => result.Value;
        public static Result<TValue> Match<TValue>(this Result<TValue> result, Func<TValue, Result<TValue>> success, Func<Error, Result<TValue>> failure)
        {
            return result.IsSuccess ? success(result.Value) : failure(result.Error);
        }

        public static Result<TValue> Match<TValue>(this Result<TValue> result, Action<TValue> success, Action<Error> failure, Action<TValue> always)
        {
            if (result.IsSuccess)
            {
                success(result.Value);
            }
            else if (result.IsFailure)
            {
                failure(result.Error);
            }

            always(result.Value);

            return result;
        }

        public static TValue Map<TValue>(this Result<TValue> result, Func<Result<TValue>, TValue> action)
        {
            return action(result);
        }

        public static async Task<TResult> ExecuteWithLogger<TResult>(this Func<Task<TResult>> function, ILogger logger, bool throwError)
        {
            logger.LogDebug("Executing method {MethodName}", function.Method.Name);
            Stopwatch sw = Stopwatch.StartNew();

            Result<TResult> result = await Result.TryAwait(function);
            sw.Stop();

            result.Match(
                success => logger.LogDebug("Executed method {MethodName} in {ElapsedMilliseconds}ms => {result}",
                            function.Method.Name,
                            sw.ElapsedMilliseconds,
                            success),

                error => logger.LogError("Error Executed method {MethodName} in {ElapsedMilliseconds}ms => {error}",
                            function.Method.Name,
                            sw.ElapsedMilliseconds,
                            error),

                always => { }
            );

            if (result.IsFailure)
            {
                if (throwError)
                {
                    logger.LogError("Throwing exception for method {MethodName} with error {Error}", function.Method.Name, result.Error);
                    throw new Exception(result.Error.ToString());
                }
            }

            return result.Value;

        }
    }
}