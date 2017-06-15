using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gmich.Results
{

    public class Result
    {
        #region Public Properties

        public string ErrorMessage { get; }
        public State State { get; private set; }
        public bool Failure => State != State.Ok;
        public bool Success => State == State.Ok;

        #endregion

        #region Ctor

        public static Result Create(State state, string error) => new Result(state, error);

        protected internal Result(State state, string error)
        {
            State = state;
            ErrorMessage = error;
        }

        #endregion

        #region Generic OnSuccess

        public static Result<T3> Chain<T1, T2, T3>(
            Result<T1> res1,
            Func<T1, Result<T2>> res2,
            Func<T1, T2, Result<T3>> final)
        {
            if (res1.Success)
            {
                var secondRes = res2(res1.Value);
                if (secondRes.Success)
                {
                    return final(res1.Value, secondRes.Value);
                }
                return secondRes.As<T3>();
            }
            return res1.As<T3>();
        }

        public static Result<T4> Chain<T1, T2, T3, T4>(
            Result<T1> res1,
            Func<T1, Result<T2>> res2,
            Func<T1, T2, Result<T3>> res3,
            Func<T1, T2, T3, Result<T4>> final)
        {
            if (res1.Success)
            {
                var secondRes = res2(res1.Value);
                if (secondRes.Success)
                {
                    var thirdRes = res3(res1.Value, secondRes.Value);
                    if (thirdRes.Success)
                    {
                        return final(res1.Value, secondRes.Value, thirdRes.Value);
                    }
                    else
                    {
                        return thirdRes.As<T4>();
                    }
                }
                else
                {
                    return secondRes.As<T4>();
                }
            }
            return res1.As<T4>();
        }

        public static Result<T5> Chain<T1, T2, T3, T4, T5>(
            Result<T1> res1,
            Func<T1, Result<T2>> res2,
            Func<T1, T2, Result<T3>> res3,
            Func<T1, T2, T3, Result<T4>> res4,
            Func<T1, T2, T3, T4, Result<T5>> final)
        {
            if (res1.Success)
            {
                var secondRes = res2(res1.Value);
                if (secondRes.Success)
                {
                    var thirdRes = res3(res1.Value, secondRes.Value);
                    if (thirdRes.Success)
                    {
                        var forthRes = res4(res1.Value, secondRes.Value, thirdRes.Value);
                        if (forthRes.Success)
                        {
                            return final(res1.Value, secondRes.Value, thirdRes.Value, forthRes.Value);
                        }
                        else
                        {
                            return forthRes.As<T5>();
                        }
                    }
                    else
                    {
                        return thirdRes.As<T5>();
                    }
                }
                else
                {
                    return secondRes.As<T5>();
                }
            }
            return res1.As<T5>();
        }

        #endregion

        #region Policies

        #region Retry Interval 

        public static Result Until(Func<bool> exit, Func<Result> func, int forceExit = 5)
        {
            var res = func();
            int retry = 1;
            while (!exit())
            {
                if (retry > forceExit) return res;
                if (res.Success)
                {
                    res = func();
                }
                else
                {
                    return res;
                }
                retry++;
            }
            return res;
        }
        public static TimeSpan ExponentialBackoff(int exponent)
        => TimeSpan.FromSeconds((System.Math.Pow(2, exponent) - 1) / 2);

        #endregion
        public static Result Retry(
            Action action,
            TimeSpan interval,
            int count = 3,
            string msg = "")
        {
            var builder = new StringBuilder();
            for (int retry = 0; retry < count; retry++)
            {
                if (retry > 0)
                    Thread.Sleep(interval);
                try
                {
                    action();
                    return Ok();
                }
                catch (Exception ex)
                {
                    builder.Append(ex.ToString());
                }
            }
            return Result.FailWith(State.Error, $"Maximum retries reached. {msg}. {builder.ToString()}");
        }

        public static Task<Result> RetryAsync(
            Action<Action> action,
            Func<int, TimeSpan> interval,
            int count = 3,
            string msg = "")
        {
            int retry = 0;
            Action cancelRetry = () => retry = count;
            var builder = new StringBuilder();

            for (retry = 0; retry < count; retry++)
            {
                if (retry > 0)
                    Task.Delay(interval(retry + 1)).Wait();
                try
                {
                    action(cancelRetry);
                    return Task.FromResult(Ok());
                }
                catch (Exception ex)
                {
                    builder.Append(ex.ToString());
                }
            }
            return Task.FromResult(Result.FailWith(State.Error, $"Maximum retries reached. {msg}. {builder.ToString()}"));
        }
        public static Task<Result<T>> RetryAsync<T>(
            Func<Action, Result<T>> func,
            Func<int, TimeSpan> interval,
            int count = 3,
            string msg = "",
            Action<string> warn = null)
        {
            var builder = new StringBuilder();
            int retry = 0;
            Action cancelRetry = () => retry = count;
            for (retry = 0; retry < count; retry++)
            {
                if (retry > 0)
                {
                    Task.Delay(interval(retry + 1)).Wait();
                }
                try
                {
                    var res = func(cancelRetry);
                    if (res.Failure)
                    {
                        var warning = $"Retry {retry + 1}. {res.ToString()}";
                        builder.Append(warning);
                        warn?.Invoke(warning);
                        continue;
                    }
                    else return Task.FromResult(res);
                }
                catch (Exception ex)
                {
                    builder.AppendLine(ex.ToString());
                }
            }
            return Task.FromResult(Result.FailWith<T>(State.Error, $"Maximum retries reached. {builder.ToString()}. {msg}."));
        }

        public static Task<Result<T>> RetryAsync<T>(
            Func<Result<T>> func,
            TimeSpan interval,
            int count = 3,
            string msg = "",
            Action<string> warn = null)
        {
            var builder = new StringBuilder();
            for (int retry = 0; retry < count; retry++)
            {
                if (retry > 0)
                {
                    Task.Delay(interval);
                }
                try
                {
                    var res = func();
                    if (res.Failure)
                    {
                        var warning = $"Retry {retry + 1}. {res.ToString()}";
                        builder.Append(warning);
                        warn?.Invoke(warning);
                        continue;
                    }
                    else return Task.FromResult(res);
                }
                catch (Exception ex)
                {
                    builder.AppendLine(ex.ToString());
                }
            }
            return Task.FromResult(Result.FailWith<T>(State.Error, $"Maximum retries reached. {builder.ToString()}. {msg}."));
        }


        public static Result Retry(
            Func<Result> func,
            TimeSpan interval,
            int count = 3,
            string msg = "")
        {
            var builder = new StringBuilder();
            for (int retry = 0; retry < count; retry++)
            {
                if (retry > 0)
                    Thread.Sleep(interval);
                try
                {
                    var res = func();
                    if (res.Failure)
                    {
                        builder.Append($"Retry {retry + 1}. {res.ToString()}");
                        continue;
                    }
                    else return res;
                }
                catch (Exception ex)
                {
                    builder.AppendLine(ex.ToString());
                }
            }
            return Result.FailWith(State.Error, $"Maximum retries reached. {builder.ToString()}. {msg}.");
        }

        public static Result<T> Retry<T>(
            Func<Result<T>> func,
            TimeSpan interval,
            int count = 3,
            string msg = "",
            Action<string> warn = null)
        {
            var builder = new StringBuilder();
            for (int retry = 0; retry < count; retry++)
            {
                if (retry > 0)
                {
                    Thread.Sleep(interval);
                }
                try
                {
                    var res = func();
                    if (res.Failure)
                    {
                        var warning = $"Retry {retry + 1}. {res.ToString()}";
                        builder.Append(warning);
                        warn?.Invoke(warning);
                        continue;
                    }
                    else return res;
                }
                catch (Exception ex)
                {
                    builder.AppendLine(ex.ToString());
                }
            }
            return Result.FailWith<T>(State.Error, $"Maximum retries reached. {builder.ToString()}. {msg}.");
        }

        public static Result<T> Retry<T>(
            Func<T> func,
            Predicate<T> successPredicate,
            TimeSpan interval,
            int count = 3,
            string msg = "")
        {
            T res = default(T);
            var errors = new StringBuilder();
            for (int retry = 0; retry < count; retry++)
            {
                if (retry > 0)
                {
                    Thread.Sleep(interval);
                }
                try
                {
                    res = func();
                    if (successPredicate(res))
                    {
                        return Result.Ok(res);
                    }
                }
                catch (Exception ex)
                {
                    errors.Append(ex.ToString());
                }
            }
            return Result.FailWith<T>(res, State.Error, $"Maximum retries reached. {msg}. {errors.ToString()}");
        }

        #endregion

        #region Failure

        public static Result<TValue> FailWith<TValue>(State state, string message)
        {
            return new Result<TValue>(default(TValue), state, message);
        }

        public static Result FailWith(State state, string message)
        {
            return new Result(state, message);
        }

        public static Result<TValue> FailWith<TValue>(TValue fallbackValue, State status, string message)
        {
            return new Result<TValue>(fallbackValue, status, message);
        }

        #endregion

        #region Equality and Operators

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            var otherRes = obj as Result;
            if (otherRes == null)
            {
                try
                {
                    return Equals((bool)obj);
                }
                catch { }
                return false;
            }
            return (otherRes.State == this.State);
        }

        public bool Equals(bool success)
        {
            return ((Success && success) || (Failure && !success));
        }

        public static implicit operator bool(Result res)
        {
            return !object.ReferenceEquals(res, null) && res.Success;
        }


        public static bool operator ^(Result result, Result otherRes)
        {
            return result.Equals(otherRes);
        }

        public static bool operator ==(Result result, Result otherRes)
        {
            if (Object.ReferenceEquals(result, otherRes))
            {
                return true;
            }
            if (Object.ReferenceEquals(result, null))
            {
                return false;
            }
            return result.Equals(otherRes);
        }

        public static bool operator !=(Result result, Result otherRes)
        {
            if (Object.ReferenceEquals(result, otherRes))
            {
                return false;
            }
            if (Object.ReferenceEquals(result, null))
            {
                return false;
            }
            return !result.Equals(otherRes);
        }

        public static Result operator &(Result result, Result otherRes)
        {
            if (Object.ReferenceEquals(result, otherRes))
            {
                return Result.Ok();
            }
            if (Object.ReferenceEquals(result, null) || Object.ReferenceEquals(otherRes, null))
            {
                return FailWith(State.Error, "Result in bitwise & was null");
            }

            if (result.Failure)
            {
                return result;
            }
            else
            {
                return otherRes;
            }
        }

        public static Result operator |(Result result, Result otherRes)
        {
            if (Object.ReferenceEquals(result, null) || Object.ReferenceEquals(otherRes, null))
            {
                return FailWith(State.Error, "Result in bitwise | was null");
            }

            if (result.Failure)
            {
                return otherRes;
            }
            return result;
        }

        public static Result operator !(Result result)
        {
            if (Object.ReferenceEquals(result, null))
            {
                return FailWith(State.Error, "Result in bitwise | was null");
            }

            if (result.State == State.Ok)
            {
                result.State = State.Error;
            }
            else
            {
                result.State = State.Ok;
            }
            return result;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator true(Result res)
        {
            return res.Success;
        }

        public static bool operator false(Result res)
        {
            return res.Failure;
        }

        #endregion

        #region Success

        public static Result Ok()
        {
            return new Result(State.Ok, String.Empty);
        }

        public static Result Ok(Action action)
        {
            action();
            return new Result(State.Ok, String.Empty);
        }

        public static Result<TValue> Ok<TValue>(TValue value)
        {
            return new Result<TValue>(value, State.Ok, String.Empty);
        }

        public static Result<TValue> Ok<TValue>(Func<TValue> valueGetter)
        {
            return new Result<TValue>(valueGetter(), State.Ok, String.Empty);
        }


        #endregion

        #region Assert

        public static Result<TValue> RequireNotNull<TValue>(Func<TValue> valueGetter)
        {
            var value = valueGetter();
            Argument.Require.NotNull(() => value);
            return RequireNotNull(value);
        }

        public static Result<TValue> RequireNotNull<TValue>(TValue value)
        {
            Argument.Require.NotNull(() => value);
            return Ok(value);
        }


        public static Result Test(bool value)
        {
            Argument.Require.That(() => value == true);
            return Result.Ok();
        }

        public static Result Ensure(Func<bool> predicate, string msg = "")
        {
            return predicate() ? Result.Ok() : Result.FailWith(State.Error, "Failed to ensure. " + msg);
        }

        public static Result Ensure(Func<bool> predicate, Func<string> msg)
        {
            return predicate() ? Result.Ok() : Result.FailWith(State.Error, "Failed to ensure. " + msg());
        }

        public static Result<TValue> Ensure<TValue>(TValue value, Func<TValue, bool> predicate, Func<string> msg)
        {
            return predicate(value) ? Result.Ok(value) : Result.FailWith(value, State.Error, "Failed to ensure. " + msg());
        }

        public static Result<TValue> Try<TValue>(Func<TValue> func)
        {
            try
            {
                return Result.Ok(func());
            }
            catch (Exception ex)
            {
                return Result.FailWith<TValue>(State.Error, ex.ToString());
            }
        }

        public static Result<TValue> Try<TValue>(Func<Result<TValue>> func)
        {
            try
            {
                return func().ThrowExceptionOnFailure();
            }
            catch (Exception ex)
            {
                return Result.FailWith<TValue>(State.Error, ex.ToString());
            }
        }

        public static Result<TValue> Try<TValue>(Func<Result<TValue>> func, Func<string> additionalMessage)
        {
            try
            {
                return func().ThrowExceptionOnFailure();
            }
            catch (Exception ex)
            {
                return Result.FailWith<TValue>(State.Error, ex.ToString() + additionalMessage());
            }
        }

        public static Result Try(Func<Result> func, string additionalMessage)
        {
            try
            {
                return func().ThrowExceptionOnFailure();
            }
            catch (Exception ex)
            {
                return FailWith(State.Error, additionalMessage + ex.ToString());
            }
        }

        public static Result Try(Func<Result> func, Func<string> additionalMessage)
        {
            try
            {
                return func().ThrowExceptionOnFailure();
            }
            catch (Exception ex)
            {
                return FailWith(State.Error, additionalMessage() + ex.ToString());
            }
        }

        public static Result<TValue> Try<TValue>(Func<TValue> func, Func<string> additionalMessage)
        {
            try
            {
                return Result.Ok(func());
            }
            catch (Exception ex)
            {
                return Result.FailWith<TValue>(State.Error, additionalMessage() + ex.ToString());
            }
        }

        public static Result<TValue> FailIfNull<TValue>(TValue value, string msg = "value was null")
        {
            if (value == null)
            {
                return FailWith<TValue>(State.NotFound, msg);
            }
            return Ok(value);
        }

        public static Result<TValue> FailIfNull<TValue>(TValue value, Func<string> msg)
        {
            if (value == null)
            {
                return FailWith<TValue>(State.NotFound, msg());
            }
            return Ok(value);
        }
        #endregion

        #region Gates

        public static Result OnFirstSuccess(params Func<Result>[] results)
        {
            foreach (var result in results)
            {
                var res = result();
                if (res.Success)
                {
                    return res;
                }
            }
            return Result.FailWith(State.Error, "All results failed");
        }

        public static Result<TValue> OnFirstSuccess<TValue>(params Func<Result<TValue>>[] results)
        {
            foreach (var result in results)
            {
                var res = result();
                if (res.Success)
                {
                    return res;
                }
            }
            return Result.FailWith<TValue>(State.Error, "All results failed");
        }

        public static Result OnAll(params Func<Result>[] results)
        {
            foreach (var result in results)
            {
                var res = result();
                if (res.Failure)
                {
                    return res;
                }
            }
            return Result.Ok();
        }

        public static Result OnAll<TValue>(TValue value, params Func<TValue, Result>[] results)
        {
            foreach (var result in results)
            {
                var res = result(value);
                if (res.Failure)
                {
                    return res;
                }
            }
            return Result.Ok();
        }

        #endregion

        #region Misc

        public static Task<Result<TValue>> AsTask<TValue>(Func<Result<TValue>> getAsync)
        {
            return Task.Run(getAsync);
        }

        public static Result Enumerate(IEnumerable<Func<Result>> results)
        {
            var errors = new StringBuilder();
            var hasFailure = false;
            foreach (var result in results)
            {
                var res = result();
                if (res.Failure)
                {
                    hasFailure = true;
                    errors.AppendLine(res.ErrorMessage);
                }
            }
            return hasFailure ?
                Result.FailWith(State.Error, errors.ToString()) :
                Result.Ok();
        }

        public static Result Enumerate(params Func<Result>[] results)
        {
            var errors = new StringBuilder();
            var hasFailure = false;
            foreach (var result in results)
            {
                var res = result();
                if (res.Failure)
                {
                    hasFailure = true;
                    errors.AppendLine(res.ErrorMessage);
                }
            }
            return hasFailure ?
                Result.FailWith(State.Error, errors.ToString()) :
                Result.Ok();
        }

        public static Result Combine(params Result[] results)
        {
            foreach (Result result in results)
            {
                if (result.Failure)
                {
                    return result;
                }
            }
            return Ok();
        }

        public static Result Combine(IEnumerable<Result> results)
        {
            foreach (Result result in results)
            {
                if (result.Failure)
                {
                    return result;
                }
            }
            return Ok();
        }

        public override string ToString()
        {
            return $"Success: { Success }. State: {State}. Message: {ErrorMessage ?? ""}.";
        }

        #endregion
    }
}