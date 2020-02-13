using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ParallelHelper.Test.Extensions {
  [TestClass]
  public class LinqExtensionsTest {
    [TestMethod, ExpectedException(typeof(OperationCanceledException))]
    public void WithCancellationThrowsOperationCanceledExceptionWhenCancellationIsRequested() {
      var canceledToken = new CancellationTokenSource();
      canceledToken.Cancel();
      Enumerable.Range(0, 10)
        .WithCancellation(canceledToken.Token)
        .ToArray();
      Assert.Fail("cancellation token not respected");
    }

    [TestMethod]
    public void WithCancellationDoesNotThrowOperationCanceledExceptionWhenNoCancellationIsRequested() {
      var canceledToken = new CancellationTokenSource();
      var entryCount = Enumerable.Range(0, 10)
        .WithCancellation(canceledToken.Token)
        .Count();
      Assert.AreEqual(10, entryCount);
    }

    [TestMethod]
    public void IsNotNullFiltersAllNullValues() {
      var input = new List<string?> {
        "a", "b", null, "c", null, "d", null
      };
      var output = input.IsNotNull().ToList();
      var expected = new List<string> {
        "a", "b", "c", "d"
      };
      CollectionAssert.AreEqual(expected, output);
    }
  }
}
