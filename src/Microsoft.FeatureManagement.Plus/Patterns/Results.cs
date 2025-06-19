using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.FeatureManagement.Plus.Patterns
{
    public class Result
    {
        protected internal Result()
        {
            Error = Error.None;
        }

        protected internal Result(Error error)
        {
            Error = error;
        }

        public bool IsSuccess => Error == Error.None;

        public bool IsFailure => !IsSuccess;

        public Error Error { get; }

        public static Result Success() => new Result();
        public static Result<TValue> Success<TValue>(TValue value) => new Result<TValue>(value);
        public static Result Failure(Error error) => new Result(error);
        public static Result<TValue> Failure<TValue>(Error error) => new Result<TValue>(error);

        public static Result Try(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                return Failure(ex);
            }

            return Success();
        }

        public static Result<TValue> Try<TValue>(Func<TValue> function, Action<Error> errorHandler)
        {
            try
            {
                TValue result = function();
                return Success(result);
            }
            catch (Exception ex)
            {
                var result = Failure<TValue>(ex);
                errorHandler?.Invoke(result);
                return result;
            }
        }

        public static Result<TValue> Try<TValue>(Func<TValue> function) => Try(function, null);

        public static async Task<Result<TValue>> TryAwait<TValue>(Func<Task<TValue>> function)
        {
            try
            {
                var result = await function().ConfigureAwait(false);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Failure<TValue>(new Error(nameof(ex), ex.Message));
            }
        }

        public static Result<Task<TValue>> TryTask<TValue>(Func<Task<TValue>> function)
        {
            try
            {
                Task<TValue> result = function();
                return Success(result);
            }
            catch (Exception ex)
            {
                return Failure<Task<TValue>>(ex);
            }
        }

        public static Result<IAsyncEnumerable<TValue>> TryAsyncEnumerable<TValue>(Func<IAsyncEnumerable<TValue>> function)
        {
            try
            {
                IAsyncEnumerable<TValue> result = function();
                return Success(result);
            }
            catch (Exception ex)
            {
                return Failure<IAsyncEnumerable<TValue>>(ex);
            }
        }
    }

    public class Result<TValue> : Result
    {
        private readonly TValue _value;

        protected internal Result(TValue value)
        {
            _value = value;
        }

        protected internal Result(Error error) : base(error)
        {
            _value = default;
        }

        public TValue Value
        {
            get
            {
                if (IsSuccess)
                {
                    return _value;
                }

                throw new InvalidOperationException("Cannot access Value when the result is a failure.");
            }
        }

        public static implicit operator Result<TValue>(TValue value) => Success(value);
        public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);
        public static implicit operator TValue(Result<TValue> result) => result.Value;
        public static implicit operator Error(Result<TValue> result) => result.Error;
    }

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
                always(result.Value);
            }
            else if (result.IsFailure)
            {
                failure(result.Error);
            }

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

            Result<TResult> result = await Result.TryAwait(function).ConfigureAwait(false);
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

    public class Error
    {
        public Error(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public bool IsNone => this == None;

        public string Code { get; }
        public string Message { get; set; }

        public static readonly Error None = new Error(string.Empty, string.Empty);

        public static readonly Error NullValue = new Error("Error.NullValue", "The specified result value is null.");

        public static readonly Error ConditionNotMet = new Error("Error.ConditionNotMet", "The specified condition was not met.");

        public static implicit operator Error(Exception ex) => new Error(ex.GetType().Name, ex.Message);

        public static implicit operator Error(Result value) => value.Error;

        private const string DefaultCodeForStringError = "Error.FromString";
        public static implicit operator Error(string value) => new Error(DefaultCodeForStringError, value);
    }
}