using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.BestPractices;

namespace ParallelHelper.Test.Analyzer.BestPractices {
  [TestClass]
  public class MonitorWaitWithoutConditionalLoopAnalyzerTest : AnalyzerTestBase<MonitorWaitWithoutConditionalLoopAnalyzer> {
    [TestMethod]
    public void ReportsWaitWithoutAnyCondition() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();

  public void DoWork() {
    lock(syncObject) {
      Monitor.Wait(syncObject);
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(8, 7));
    }

    [TestMethod]
    public void ReportsWaitOnlyEnclosedByIfStatement() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count = 0;

  public void DoWork() {
    lock(syncObject) {
      if(count <= 0) {
        Monitor.Wait(syncObject);
      }
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(10, 9));
    }

    [TestMethod]
    public void ReportsWaitEnclosedByWhileLoopOutsideOfLockStatement() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count = 0;

  public void DoWork() {
    while(count <= 0) {
      lock(syncObject) {
        Monitor.Wait(syncObject);
      }
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(10, 9));
    }

    [TestMethod]
    public void DoesNotReportWaitEnclosedByWhileLoop() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count = 0;

  public void DoWork() {
    lock(syncObject) {
      while(count <= 0) {
        Monitor.Wait(syncObject);
      }
    }
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
