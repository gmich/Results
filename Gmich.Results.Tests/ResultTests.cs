using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gmich.Results.Tests
{
    [TestClass]
    public class ResultTests
    {
        private Result SuccessfulResult => Result.Ok();
        private Result FailedResult => Result.FailWith(State.Error, "Failed Result");

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
            SuccessfulResult
            .OnSuccess(res =>
                Assert.Fail("This should fail"))
            .OnFailure(res =>
                Assert.IsTrue(res.Failure));
        }
    }
}
