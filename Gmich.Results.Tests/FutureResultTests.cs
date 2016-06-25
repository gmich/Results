using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gmich.Results.Tests
{ 
    [TestClass]
    public class FutureResultTests
    {
        private FutureResult TrueResult =>
            FutureResult.For(() =>
                Result.Ok());

        private FutureResult FalseResult =>
            FutureResult.For(() =>
                Result.FailWith(State.Error, "Error"));

        [TestMethod]
        public void FutureResultTests_True_And_False_EqualsFalse()
        {
            Assert.IsFalse(TrueResult.And(FalseResult).Result);
        }

        [TestMethod]
        public void FutureResultTests_True_Or_False_EqualsTrue()
        {
            Assert.IsTrue(TrueResult.Or(FalseResult).Result);
        }

        [TestMethod]
        public void FutureResultTests_False_And_True_EqualsFalse()
        {
            Assert.IsFalse(FalseResult.And(TrueResult).Result);
        }

        [TestMethod]
        public void FutureResultTests_Not_True_EqualsFalse()
        {
            Assert.IsFalse(TrueResult.Not.Result);
        }

        [TestMethod]
        public void FutureResultTests_Not_False_EqualsTrue()
        {
            Assert.IsTrue(FalseResult.Not.Result);
        }
    }
}
