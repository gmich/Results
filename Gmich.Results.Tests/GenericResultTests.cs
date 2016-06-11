using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gmich.Results.Tests
{
    [TestClass]
    public class GenericResultTests
    {
        private Result<string> SuccessfulResult => Result.Ok("Success");
        private Result<string> FailedResult => Result.FailWith<string>(State.Error, "Failed Result");

        [TestMethod]
        public void SuccessfulResult_Test()
        {
            SuccessfulResult
            .OnSuccess(res =>
                Assert.IsTrue(res.Success))
            .OnFailure(res =>
                Assert.Fail("This should be successful"));
        }

        [TestMethod]
        public void FailedResult_Test()
        {
            FailedResult
            .OnSuccess(res =>
                Assert.Fail("This should fail"))
            .OnFailure(res =>
                Assert.IsTrue(res.Failure));
        }

        [TestMethod]
        public void ChangeValues()
        {
            int newValue = 10;
            SuccessfulResult
            .OnSuccess(res =>
                Result.Ok(newValue))
            .OnSuccess(res =>
                Assert.AreEqual(newValue,res))
            .OnFailure(res =>
                Assert.Fail("This should be successful"));
        }

        [TestMethod]
        public void ChangeValuesWithAs()
        {
            var result=
            SuccessfulResult
            .OnSuccess(res =>
                Result.Ok(10))
            .As<string>();

            Assert.AreEqual(typeof(Result<string>), result.GetType());
        }
    }
}
