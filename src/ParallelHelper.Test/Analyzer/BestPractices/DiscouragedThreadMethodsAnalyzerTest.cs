using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.BestPractices;

namespace ParallelHelper.Test.Analyzer.BestPractices {
  [TestClass]
  public class DiscouragedThreadMethodsAnalyzerTest : AnalyzerTestBase<DiscouragedThreadMethodsAnalyzer> {
    [TestMethod]
    public void ReportsThreadAbort() {
      const string source = @"
using System.Threading;

class Test {
  public void DoWork() {
    var thread = new Thread(() => { });
    thread.Abort();
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 5));
    }

    [TestMethod]
    public void DoesNotReportThreadStart() {
      const string source = @"
using System.Threading;

class Test {
  public void DoWork() {
    var thread = new Thread(() => { });
    thread.Start();
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
