using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.BestPractices;

namespace ParallelHelper.Test.Analyzer.BestPractices {
  [TestClass]
  public class MonitorLockInsteadOfEnterAnalyzerTest : AnalyzerTestBase<MonitorLockInsteadOfEnterAnalyzer> {
    [TestMethod]
    public void ReportsEnterWithSyncObjectOnly() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();

  public void DoWork() {
    Monitor.Enter(syncObject);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 5));
    }

    [TestMethod]
    public void DoesNotReportEnterWithSyncObjectAndLockTakenFlag() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();

  public void DoWork() {
    var lockTaken = false;
    Monitor.Enter(syncObject, ref lockTaken);
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportPulse() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();

  public void DoWork() {
    Monitor.Pulse(syncObject);
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
