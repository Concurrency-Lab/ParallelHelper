using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.BestPractices;

namespace ParallelHelper.Test.Analyzer.BestPractices {
  [TestClass]
  public class MonitorReadonlySyncObjectAnalyzerTest : AnalyzerTestBase<MonitorReadonlySyncObjectAnalyzer> {
    [TestMethod]
    public void ReportsNonReadonlyField() {
      const string source = @"
class Test {
  private object syncObject = new object();

  public void DoWork() {
    lock(syncObject) {
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 10));
    }

    [TestMethod]
    public void DoesNotReportReadonlyField() {
      const string source = @"
class Test {
  private readonly object syncObject = new object();

  public void DoWork() {
    lock(syncObject) {
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReadonlyFieldInParentheses() {
      const string source = @"
class Test {
  private readonly object syncObject = new object();

  public void DoWork() {
    lock((syncObject)) {
    }
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
