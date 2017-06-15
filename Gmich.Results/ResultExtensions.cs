using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gmich.Results
{
    public class ResultException : Exception
    {
        public ResultException(string message) : base(message)
        {
        }
    }

    public static class ResultExtensions
    {

        #region Exceptions

        public static Result<TValue> ThrowExceptionOnFailure<TValue>(this Result<TValue> res)
        {
            if (res.Failure)
            {
                throw new ResultException(res.ToString());
            }
            return res;
        }

        public static Result ThrowExceptionOnFailure(this Result res)
        {
            if (res.Failure)
            {
                throw new ResultException(res.ToString());
            }
            return res;
        }

        #endregion

        #region Linq

        public static IEnumerable<TValue> FilterSuccessful<TValue>(this IEnumerable<Result<TValue>> stream)
        {
            return stream.Where(c => c.Success).Select(c => c.Value);
        }
        public static IEnumerable<Result> FilterSuccessful(this IEnumerable<Result> stream)
        {
            return stream.Where(c => c.Success);
        }

        public static Result Enumerate<TValue>(this IEnumerable<Result<TValue>> stream, Action<TValue> action)
        {
            foreach (var res in stream)
            {
                if (res.Success)
                {
                    action(res.Value);
                }
                else
                {
                    return res;
                }
            }
            return Result.Ok();
        }

        public static bool AnySuccessful(this IEnumerable<Result> stream)
        {
            return stream.Any(c => c.Success);
        }

        public static bool AllSuccessful(this IEnumerable<Result> stream)
        {
            return !stream.Any(c => c.Failure);
        }

        public static Result AnyFailures(this IEnumerable<Result> stream)
        {
            var failedResult = stream.FirstOrDefault(c => c.Failure);
            return failedResult ?? Result.Ok();
        }

        public static IEnumerable<Result> LogFailures(this IEnumerable<Result> stream, Action<string> log)
        {
            foreach (var res in stream)
            {
                res.LogOnFailure(log);
            }
            return stream;
        }

        public static IEnumerable<Result<TValue>> LogFailures<TValue>(this IEnumerable<Result<TValue>> stream, Action<string> log)
        {
            foreach (var res in stream)
            {
                res.LogOnFailure(log);
            }
            return stream;
        }

        #endregion


        #region Async

        public static async Task<Result> OnSuccessAsync(this Result result, Func<Task<Result>> func)
        {
            if (result.Failure)
            {
                return result;
            }
            var res = await func();
            return res;
        }

        public static async Task<Result> OnSuccessAsync<TValue>(this Result<IEnumerable<Task<TValue>>> result, Action<IEnumerable<TValue>> func)
        {
            if (result.Failure)
            {
                return result;
            }
            var res = await Task.WhenAll(result.Value);
            func(res);
            return result;
        }

        public static async Task<Result> OnSuccessAsync(this Task<Result> resultTask, Func<Result, Result> func)
        {
            var result = await resultTask;
            if (result.Failure)
            {
                return result;
            }
            return func(result);
        }

        public static async Task<Result<TValue>> OnSuccessAsync<TValue>(this Result result, Func<Task<Result<TValue>>> func)
        {
            if (result.Failure)
            {
                return result.As<TValue>();
            }
            var res = await func();
            return res;
        }

        public static async Task<Result<TValue>> OnSuccessAsync<TValue>(this Task<Result<TValue>> resultTask, Action<Result<TValue>> action)
        {
            var result = await resultTask;
            if (result.Failure)
            {
                return result;
            }
            action(result);
            return result;
        }

        public static async Task<Result<TValue>> OnFailureAsync<TValue>(this Task<Result<TValue>> resultTask, Action<Result<TValue>> action)
        {
            var result = await resultTask;
            if (result.Success)
            {
                return result;
            }
            action(result);
            return result;
        }

        public static async Task<Result<TValue>> LogOnFailureAsync<TValue>(this Task<Result<TValue>> result, Action<string> logger, string msg = "")
        {
            var res = await result;
            if (res.Failure)
            {
                logger($"{msg}. {res.ToString()}");
            }
            return res;
        }

        public static async Task<Result<TValue>> Do<TValue>(this Task<Result<TValue>> result, Action<Result<TValue>> action)
        {
            var res = await result;
            action(res);
            return res;
        }


        #endregion

        #region Success

        public static Result OnSuccess(this Result result, Func<Result> func)
        {
            if (result.Failure)
            {
                return result;
            }
            return func();
        }

        public static Result<TValue> TryOnSuccess<TValue>(this Result result, Func<TValue> func, Func<string> msg)
        {
            if (result.Failure)
            {
                return Result.FailWith<TValue>(result.State, result.ErrorMessage);
            }

            return Result.Try(func, msg);
        }

        public static Result TryOnSuccess<TValue>(this Result result, Func<Result> func, Func<string> msg)
        {
            if (result.Failure)
            {
                return Result.FailWith(result.State, result.ErrorMessage);
            }

            return Result.Try(func, msg);
        }


        public static Result<TNext> TryOnSuccess<TValue, TNext>(this Result<TValue> result, Func<TValue, TNext> func, Func<string> msg)
        {
            if (result.Failure)
            {
                return Result.FailWith<TNext>(result.State, result.ErrorMessage);
            }

            return Result.Try(() => func(result.Value), msg);
        }

        public static Result<TValue> OnSuccess<TValue>(this Result result, Func<Result<TValue>> func)
        {
            if (result.Failure)
            {
                return Result.FailWith<TValue>(result.State, result.ErrorMessage);
            }
            return func();
        }

        public static Result OnSuccess(this Result result, Action<Result> action)
        {
            if (result.Failure)
            {
                return result;
            }
            action(result);

            return result;
        }

        public static Result OnSuccess(this Result result, Action action)
        {
            if (result.Failure)
            {
                return result;
            }
            action();

            return Result.Ok();
        }

        public static Result<TValue> ToResult<TValue>(this TValue value) => Result.Ok(value);

        public static Result<TValue> SuccessDependsOn<TValue>(this Result<TValue> result, params Result[] others)
        {
            if (result.Failure) return result;

            foreach (var other in others)
            {
                if (other.Failure)
                {
                    return Result.FailWith(result.Value, other.State, other.ErrorMessage);
                }
            }
            return result;
        }
        public static Result<TValue> OnSuccess<TValue>(this Result<TValue> result, Action<TValue> action)
        {
            if (result.Success)
            {
                action(result.Value);
            }

            return result;
        }

        public static Result<TValue> ResultOnSuccess<TValue>(this Result<TValue> result, Action<Result<TValue>> action)
        {
            if (result.Success)
            {
                action(result);
            }

            return result;
        }

        public static Result<TNext> OnSuccess<TValue, TNext>(this Result<TValue> result, Func<TValue, Result<TNext>> func)
        {
            if (result.Failure)
            {
                return Result.FailWith<TNext>(result.State, result.ErrorMessage);
            }
            return func(result.Value);
        }

        public static Result OnSuccess<TValue>(this Result<TValue> result, Func<TValue, Result> func)
        {
            if (result.Failure)
            {
                return result;
            }
            return func(result.Value);
        }

        #endregion

        #region Assert

        public static Result<TValue> ThrowIfNull<TValue>(this Result<TValue> result)
        {
            if (result.Success)
            {
                Argument.Require.NotNull(() => result.Value);
            }
            return result;
        }

        public static Result<TValue> Ensure<TValue>(this Result<TValue> result, Predicate<TValue> predicate, string failure = "")
        {
            if (result.Failure)
            {
                return result;
            }
            if (predicate(result.Value))
            {
                return result;
            }
            return Result.FailWith<TValue>(result.Value, State.Error, "Ensure failure. " + failure);
        }

        public static Result<TValue> Ensure<TValue>(this Result<TValue> result, Predicate<TValue> predicate, Func<string> failure)
        {
            if (result.Failure)
            {
                return result;
            }
            if (predicate(result.Value))
            {
                return result;
            }
            return Result.FailWith<TValue>(result.Value, State.Error, "Ensure failure. " + failure());
        }

        public static Result<TValue> Ensure<TValue>(this Result<TValue> result, Predicate<TValue> predicate, Func<TValue, string> failure)
        {
            if (result.Failure)
            {
                return result;
            }
            if (predicate(result.Value))
            {
                return result;
            }
            return Result.FailWith<TValue>(result.Value, State.Error, "Ensure failure. " + failure(result.Value));
        }

        public static Result Ensure(this Result result, Predicate<Result> predicate, string failure = "")
        {
            if (result.Failure)
            {
                return result;
            }
            if (predicate(result))
            {
                return result;
            }
            return Result.FailWith(State.Error, "Ensure failure. " + failure);
        }

        public static Result Ensure(this Result result, Predicate<Result> predicate, Func<string> failure)
        {
            if (result.Failure)
            {
                return result;
            }
            if (predicate(result))
            {
                return result;
            }
            return Result.FailWith(State.Error, "Ensure failure. " + failure());
        }

        public static Result Do(this Result result, Action<Result> thisResult)
        {
            thisResult(result);
            return result;
        }

        public static Result<TValue> Do<TValue>(this Result<TValue> result, Action<Result<TValue>> thisResult)
        {
            thisResult(result);
            return result;
        }

        public static Result FailWithState(this Result result, State newState)
        {
            if (result.Failure)
            {
                return new Result(newState, result.ErrorMessage);
            }
            return result;
        }

        public static Result<TValue> FailWithState<TValue>(this Result<TValue> result, State newState)
        {
            if (result.Failure)
            {
                return new Result<TValue>(result.Value, newState, result.ErrorMessage);
            }
            return result;
        }

        public static Result<TValue> FailIfNull<TValue>(this Result<TValue> result, Func<string> message = null)
        {
            if (result.Success)
            {
                if (result.Value == null)
                {
                    return new Result<TValue>(result.Value, State.Error, $"{typeof(TValue)} should not be null. {message?.Invoke() ?? ""}");
                }
            }
            return result;
        }


        public static Result<TValue> FailWithStateIf<TValue>(this Result<TValue> result, State newState, Predicate<TValue> predicate)
        {
            if (result.Failure)
            {
                if (predicate(result.Value))
                {
                    return new Result<TValue>(result.Value, newState, result.ErrorMessage);
                }
            }
            return result;
        }

        #endregion


        #region Failure

        public static Result OnFailure(this Result result, Action<Result> action)
        {
            if (result.Failure)
            {
                action(result);
            }
            return result;
        }



        public static Result OnFailure(this Result result, Func<Result> next)
        {
            if (result.Failure)
            {
                return next();
            }
            return result;
        }

        public static Result OnFailure(this Result result, Func<Result, Result> next)
        {
            if (result.Failure)
            {
                return next(result);
            }
            return result;
        }

        public static Result ForceSuccess(this Result result)
        {
            if (result.Failure)
            {
                return Result.Ok();
            }
            return result;
        }

        public static Result<TValue> OnFailure<TValue>(this Result<TValue> result, Action<Result<TValue>> action)
        {
            if (result.Failure)
            {
                action(result);
            }
            return result;
        }


        public static Result<TValue> OnFailure<TValue>(this Result<TValue> result, Func<Result<TValue>, Result<TValue>> func)
        {
            if (result.Failure)
            {
                return func(result);
            }
            return result;
        }


        public static Result<TValue> OnFailure<TValue>(this Result<TValue> result, Func<Result<TValue>> func)
        {
            if (result.Failure)
            {
                return func();
            }
            return result;
        }

        #endregion

        #region Log

        public static Result<TValue> Log<TValue>(this Result<TValue> result, Action<string> logger, string msg = "")
        {
            logger($"{msg}. {result.ToString()}");
            return result;
        }

        public static Result<TValue> LogOnFailure<TValue>(this Result<TValue> result, Action<string> logger, string msg = "")
        {
            if (result.Failure)
            {
                logger($"{msg}. {result.ToString()}");
            }
            return result;
        }

        public static Result<TValue> LogOnFailure<TValue>(this Result<TValue> result, Action<string> logger, Func<string> msg)
        {
            if (result.Failure)
            {
                logger($"{msg()}. {result.ToString()}");
            }
            return result;
        }

        public static Result LogOnFailure(this Result result, Action<string> logger, Func<string> msg = null)
        {
            if (result.Failure)
            {
                logger($"{msg?.Invoke() ?? ""} {result.ToString()}");
            }
            return result;
        }

        public static Result LogIf(this Result result, Action<string> logger, Predicate<Result> predicate, Func<string> msg = null)
        {
            if (predicate(result))
            {
                logger($"{msg?.Invoke() ?? ""} {result.ToString()}");
            }
            return result;
        }

        public static Result<TValue> LogIf<TValue>(this Result<TValue> result, Action<string> logger, Predicate<Result<TValue>> predicate, Func<string> msg = null)
        {
            if (predicate(result))
            {
                logger($"{msg?.Invoke() ?? ""} {result.ToString()}");
            }
            return result;
        }


        public static Result Log(this Result result, Action<string> logger, string msg = "")
        {
            logger($"{msg}. {result.ToString()}");
            return result;
        }

        #endregion

        #region Map

        public static Result<TValue> As<TValue>(this Result result)
        {
            return new Result<TValue>(default(TValue), result.State, result.ErrorMessage);
        }

        public static Result AsResult<TValue>(this Result<TValue> result)
        {
            return new Result(result.State, result.ErrorMessage);
        }
        public static Result<TNext> As<TNext>(this Result result, TNext next)
        {
            return new Result<TNext>(next, result.State, result.ErrorMessage);
        }

        public static Result<TNext> As<TValue, TNext>(this Result<TValue> result, Func<TValue, TNext> next)
        {
            return new Result<TNext>(result.Success ? next(result.Value) : default(TNext), result.State, result.ErrorMessage);
        }

        public static Result<TNext> As<TNext>(this Result result, Func<TNext> next)
        {
            return new Result<TNext>(result.Success ? next() : default(TNext), result.State, result.ErrorMessage);
        }

        public static Result<TNewValue> ChangeValue<TValue, TNewValue>(this Result<TValue> result, TNewValue newValue)
        {
            return new Result<TNewValue>(newValue, result.State, result.ErrorMessage);
        }

        public static Result<TNewValue> ChangeValue<TValue, TNewValue>(this Result<TValue> result, Func<TValue, TNewValue> newValue)
        {
            return new Result<TNewValue>(newValue(result.Value), result.State, result.ErrorMessage);
        }

        public static Result<TConverted> Cast<TConverted>(this Result result)
        {
            return new Result<TConverted>(default(TConverted), result.State, result.ErrorMessage);
        }

        #endregion

        #region Add Errors

        public static Result<TValue> AddErrorMessage<TValue>(this Result<TValue> result, Result other)
        {
            return Result.FailWith(result.Value, result.State, $"{result.ErrorMessage}. {other.ErrorMessage}");
        }

        public static Result<TValue> WithErrorMessage<TValue>(this Result<TValue> result, Func<string> error)
        {
            if (result.Failure)
            {
                return Result.FailWith(result.Value, result.State, $"{result.ErrorMessage}. {error()}");
            }
            return result;
        }

        public static Result WithErrorMessage(this Result result, Func<string> error)
        {
            if (result.Failure)
            {
                return Result.FailWith(result.State, $"{result.ErrorMessage}. {error()}");
            }
            return result;
        }

        public static Result AddErrorMessage(this Result result, Result other)
        {
            if (result.Failure)
            {
                return Result.FailWith(result.State, $"{result.ErrorMessage}. {other.ErrorMessage}");
            }
            return result;
        }

        public static Result<TValue> ChangeErrorMessage<TValue>(this Result<TValue> result, Func<string> error)
        {
            if (result.Failure)
            {
                return Result.FailWith(result.Value, result.State, error());
            }
            return result;
        }

        public static Result ChangeErrorMessage(this Result result, Func<string> error)
        {
            if (result.Failure)
            {
                return Result.FailWith(result.State, error());
            }
            return result;
        }
        #endregion

        #region On both

        public static Result<TNext> OnBoth<TValue, TNext>(this Result<TValue> result, Func<Result<TValue>, Result<TNext>> func)
        {
            return func(result);
        }

        public static Result<TNext> OnBoth<TValue, TNext>(this Result<TValue> result, Func<TValue, Result<TNext>> func)
        {
            return func(result.Value);
        }

        public static Result<TValue> OnBoth<TValue>(this Result<TValue> result, Action<TValue> action)
        {
            action(result.Value);
            return result;
        }

        public static Result OnBoth<TValue>(this Result<TValue> result, Func<TValue, Result> func)
        {
            return func(result.Value);
        }

        public static Result<TValue> OnBoth<TValue>(this Result<TValue> result, Func<TValue, Result<TValue>> func)
        {
            return func(result.Value);
        }

        public static Result<TValue> OnBoth<TValue>(this Result result, Func<Result, Result<TValue>> func)
        {
            return func(result);
        }

        public static Result OnBoth(this Result result, Func<Result> action)
        {
            return action();
        }

        public static Result OnBoth(this Result result, Func<Result, Result> func)
        {
            return func(result);
        }

        public static Result OnBoth(this Result result, Action action)
        {
            action();
            return result;
        }

        public static Result OnBoth(this Result result, Action<Result> action)
        {
            action(result);
            return result;
        }


        #endregion

    }
}
