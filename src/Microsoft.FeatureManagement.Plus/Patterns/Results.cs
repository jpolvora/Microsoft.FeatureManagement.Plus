using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.FeatureManagement.Plus.Patterns
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public class Result
    {
        protected internal Result()
        {
            Fault = Fault.None;
        }

        protected internal Result(Fault fault)
        {
            Fault = fault;
        }

        public bool IsSuccess => Fault == Fault.None;

        public bool IsFailure => !IsSuccess;

        public Fault Fault { get; }

        public static Result Success() => new Result();
        public static Result<TValue> Success<TValue>(TValue value) => new Result<TValue>(value);
        public static Result Failure(Fault fault) => new Result(fault);
        public static Result<TValue> Failure<TValue>(Fault fault) => new Result<TValue>(fault);

        [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
        public static Result Try(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

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

        public static Result<TValue> Try<TValue>(Func<TValue> function, Action<Fault> errorHandler)
        {
            if (function == null) throw new ArgumentNullException(nameof(function));
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
            if (function == null) throw new ArgumentNullException(nameof(function));
            try
            {
                TValue result = await function().ConfigureAwait(false);
                return Success(result);
            }
            catch (Exception ex)
            {
                return Failure<TValue>(new Fault(nameof(ex), ex.Message));
            }
        }

        public static Result<Task<TValue>> TryTask<TValue>(Func<Task<TValue>> function)
        {
            if (function == null) throw new ArgumentNullException(nameof(function));

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
            if (function == null) throw new ArgumentNullException(nameof(function));
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

    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
    public class Result<TValue> : Result
    {
        private readonly TValue _value;

        protected internal Result(TValue value)
        {
            _value = value;
        }

        protected internal Result(Fault fault) : base(fault)
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
        public static implicit operator Result<TValue>(Fault fault) => Failure<TValue>(fault);
        public static implicit operator TValue(Result<TValue> result) => result != null ? result.Value : default;

        public static implicit operator Fault(Result<TValue> result) => result != null ? result.Fault : Fault.None;

        public static Result<TValue> ToResult(TValue value) => Success(value);
        public static Result<TValue> FromError(Fault fault) => Failure<TValue>(fault);
        public static TValue FromResult(Result<TValue> result) => result != null ? result.Value : default;
    }

    [SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates")]
    [SuppressMessage("Usage", "CA2201:Do not raise reserved exception types")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class ResultExtensions
    {
        public static async Task<T> TaskValue<T>(this Task<Result<T>> resultTask)
        {
            if (resultTask == null)
            {
                throw new ArgumentNullException(nameof(resultTask), "The result task cannot be null.");
            }

            return await resultTask.ConfigureAwait(false);
        }

        public static IAsyncEnumerable<T> AsyncValue<T>(this Result<IAsyncEnumerable<T>> result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            return result.Value;
        }

        public static Result<TValue> Match<TValue>(this Result<TValue> result, Func<TValue, Result<TValue>> success, Func<Fault, Result<TValue>> failure)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (success == null) throw new ArgumentNullException(nameof(success));
            if (failure == null) throw new ArgumentNullException(nameof(failure));

            return result.IsSuccess ? success(result.Value) : failure(result.Fault);
        }

        public static Result<TValue> Match<TValue>(this Result<TValue> result, Action<TValue> success, Action<Fault> failure, Action<TValue> always)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (success == null) throw new ArgumentNullException(nameof(success));
            if (failure == null) throw new ArgumentNullException(nameof(failure));
            if (always == null) throw new ArgumentNullException(nameof(always));

            if (result.IsSuccess)
            {
                success(result.Value);
                always(result.Value);
            }
            else if (result.IsFailure)
            {
                failure(result.Fault);
            }

            return result;
        }

        public static TValue Map<TValue>(this Result<TValue> result, Func<Result<TValue>, TValue> action)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (action == null) throw new ArgumentNullException(nameof(action));

            return action(result);
        }

        public static async Task<TResult> ExecuteWithLogger<TResult>(this Func<Task<TResult>> function, ILogger logger, bool throwError)
        {
            if (function == null) throw new ArgumentNullException(nameof(function));
            logger.LogDebug("Executing method {MethodName}", function.Method.Name);
            Stopwatch sw = Stopwatch.StartNew();

            Result<TResult> result = await Result.TryAwait(function).ConfigureAwait(false);
            sw.Stop();

            result.Match(
                success => logger.LogDebug("Executed method {MethodName} in {ElapsedMilliseconds}ms => {Result}",
                    function.Method.Name,
                    sw.ElapsedMilliseconds,
                    success),
                error => logger.LogError("Error Executed method {MethodName} in {ElapsedMilliseconds}ms => {Error}",
                    function.Method.Name,
                    sw.ElapsedMilliseconds,
                    error),
                always => { }
            );

            if (result.IsFailure)
            {
                if (throwError)
                {
                    logger.LogError("Throwing exception for method {MethodName} with error {Error}", function.Method.Name, result.Fault);
                    throw new Exception(result.Fault.ToString());
                }
            }

            return result.Value;
        }
    }

    public class Fault
    {
        private const string DefaultCodeForStringError = "Error.FromString";

        public Fault(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public bool IsNone => this == None;

        public string Code { get; }
        public string Message { get; set; }

        public static readonly Fault None = new Fault(string.Empty, string.Empty);

        public static readonly Fault NullValue = new Fault("Error.NullValue", "The specified result value is null.");

        public static readonly Fault ConditionNotMet = new Fault("Error.ConditionNotMet", "The specified condition was not met.");

        public static implicit operator Fault(Exception ex) => new Fault(ex?.GetType().Name, ex.Message);
        public static Fault FromException(Exception ex) => ex;

        public static implicit operator Fault(Result value) => value?.Fault ?? None;
        public static Fault FromResult(Result value) => value?.Fault ?? None;

        public static implicit operator Fault(string value) => new Fault(DefaultCodeForStringError, value);
        public static Fault ToFault(string value) => new Fault(DefaultCodeForStringError, value);
    }
}