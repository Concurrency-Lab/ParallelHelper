using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Bugs;

namespace ParallelHelper.Test.Analyzer.Bugs {
  [TestClass]
  public class MonitorWaitInsideNestedLocksAnalyzerTest : AnalyzerTestBase<MonitorWaitInsideNestedLocksAnalyzer> {
    [TestMethod]
    public void ReportsWaitInsideTwoNestedLocksWithDifferentSyncObjects() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject1 = new object();
  private readonly object syncObject2 = new object();

  public void DoWork() {
    lock(syncObject1) {
      lock(syncObject2) {
        Monitor.Wait(syncObject1);
      }
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(10, 9));
    }

    [TestMethod]
    public void DoesNotReportWaitInsideSingleLockStatement() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject1 = new object();

  public void DoWork() {
    lock(syncObject1) {
      Monitor.Wait(syncObject1);
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportWaitInsideNestedLockStatementWithSameSyncObject() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject1 = new object();

  public void DoWork() {
    lock(syncObject1) {
      lock(syncObject1) {
        Monitor.Wait(syncObject1);
      }
    }
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
