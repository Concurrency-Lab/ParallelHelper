using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class InvalidUseOfNonBlockingCollectionAnalyzerTest : AnalyzerTestBase<InvalidUseOfNonBlockingCollectionAnalyzer> {
    [TestMethod]
    public void ReportsWhileLoopWithNonBlockingQueueConsumptionAndBlockingSleepWithContinue() {
      const string source = @"
using System.Collections.Concurrent;
using System.Threading;

class Test {
  private readonly ConcurrentQueue<string> queue = new ConcurrentQueue<string>();

  public void ProcessItems() {
    while(true) {
      if(!queue.TryDequeue(out var item)) {
        Thread.Sleep(100);
        continue;
      }
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(9, 7));
    }

    [TestMethod]
    public void DoesNotReportBlockingQueueConsumptionOnlyWithContinue() {
      const string source = @"
using System.Collections.Concurrent;
using System.Threading;

class Test {
  private readonly ConcurrentQueue<string> queue = new ConcurrentQueue<string>();

  public void ProcessItems() {
    while(true) {
      if(!queue.TryDequeue(out var item)) {
        continue;
      }
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportBlockingQueueConsumptionForUnspooling() {
      const string source = @"
using System.Collections.Concurrent;
using System.Threading;

class Test {
  private readonly ConcurrentQueue<string> queue = new ConcurrentQueue<string>();

  public void ProcessItems() {
    while(true) {
      if(!queue.TryDequeue(out var item) || item == null) {
        continue;
      }
    }
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
