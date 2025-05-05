using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FeatureManagement.ResultPattern
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

        public static Result<TValue> Try<TValue>(Func<TValue> function, Action<Error> errorHandler = default)
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

        public static Result<TValue> Try<TValue>(Func<TValue> function) => Try(function, default);


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

        protected internal Result(TValue value) : base()
        {
            _value = value;
        }

        protected internal Result(Error error) : base(error)
        {
            _value = default;
        }

        public TValue Value => IsSuccess ? _value : default;

        public static implicit operator Result<TValue>(TValue value) => Success(value);
        public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);
        public static implicit operator TValue(Result<TValue> result) => result.Value;
        public static implicit operator Error(Result<TValue> result) => result.Error;
    }
}