using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class FireAndForgetThreadAnalyzerTest : AnalyzerTestBase<FireAndForgetThreadAnalyzer> {
    [TestMethod]
    public void ReportsThreadStartFireAndForget() {
      const string source = @"
using System.Threading;

class Test {
  public void DoWork() {
    new Thread(() => {}).Start();
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 5));
    }

    [TestMethod]
    public void DoesNotReportThreadStartedFromVariable() {
      const string source = @"
using System.Threading;

class Test {
  public void DoWork() {
    var thread = new Thread(() => {});
    thread.Start();
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportThreadStartedFromMethodInvocation() {
      const string source = @"
using System.Threading;

class Test {
  public void DoWork() {
    CreateThread().Start();
  }

  public Thread CreateThread() {
    return new Thread(() => {});
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportNonThreadFireAndForget() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public static void DoWork() {
    new Test().Start();
  }

  public void Start() {}
}";
      VerifyDiagnostic(source);
    }
  }
}
