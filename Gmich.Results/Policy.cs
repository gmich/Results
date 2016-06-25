using System;
using System.Threading;

namespace Gmich.Results
{
    public static class Policy
    {
        public static Result Retry(
            Func<Result> func,
            TimeSpan interval,
            int count = 3,
            string errorMessage = "")
        {
            Result result = Result.FailWith(State.Error, errorMessage);

            for (int retry = 0; retry < count; retry++)
            {
                if (retry > 0)
                    Thread.Sleep(interval);
                try
                {
                    result = func();
                }
                catch (Exception ex)
                {
                    return Result.FailWith(State.Error, errorMessage + ex.Message);
                }
                if (result.Success) return result;
            }
            return result;
        }

        public static Result<T> Retry<T>(
            Func<Result<T>> func,
            TimeSpan interval,
            int count = 3,
            string errorMessage = "")
        {
            Result<T> result = Results.Result.FailWith<T>(State.Error, errorMessage);

            for (int retry = 0; retry < count; retry++)
            {
                if (retry > 0)
                {
                    Thread.Sleep(interval);
                }
                try
                {
                    result = func();
                }
                catch (Exception ex)
                {
                    return Result.FailWith<T>(State.Error, errorMessage + ex.Message);
                }
                if (result.Success) return result;
            }
            return result;
        }
    }
}
