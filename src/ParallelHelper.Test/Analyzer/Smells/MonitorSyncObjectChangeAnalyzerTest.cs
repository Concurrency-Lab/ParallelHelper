using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class MonitorSyncObjectChangeAnalyzerTest : AnalyzerTestBase<MonitorSyncObjectChangeAnalyzer> {
    [TestMethod]
    public void ReportsChangeInsideMonitor() {
      const string source = @"
class Test {
  private object syncObject = new object();

  public void DoWork() {
    lock(syncObject) {
      syncObject = new object();
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 7));
    }

    [TestMethod]
    public void DoesNotReportChangeOutsideMonitor() {
      const string source = @"
class Test {
  private object syncObject = new object();

  public void DoWork() {
    lock(syncObject) {
    }
    syncObject = new object();
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportChangeOfDifferentFieldInsideMonitor() {
      const string source = @"
class Test {
  private object syncObject = new object();
  private int count;

  public void DoWork() {
    lock(syncObject) {
      ++count;
    }
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
