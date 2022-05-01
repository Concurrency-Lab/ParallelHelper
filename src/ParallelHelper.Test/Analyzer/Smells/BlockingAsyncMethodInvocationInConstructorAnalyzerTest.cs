using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class BlockingAsyncMethodInvocationInConstructorAnalyzerTest : AnalyzerTestBase<BlockingAsyncMethodInvocationInConstructorAnalyzer> {
    [TestMethod]
    public void ReportsTaskRunWaitInvocationInConstructor() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Test() {
    Task.Run(() => {}).Wait();
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 5));
    }

    [TestMethod]
    public void ReportsWebClientDownloadResultAccessInConstructor() {
      const string source = @"
using System;
using System.Net;

class Test {
  private readonly string content;

  public Test(Uri address) {
    using(var client = new WebClient()) {
      content = client.DownloadStringTaskAsync(address).Result;
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(9, 17));
    }

    [TestMethod]
    public void DoesNotReportTaskRunWaitInvocationInDifferentActivationFrameInConstructor() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public Test() {
    Action action = () => Task.Run(() => {}).Wait();
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportWebClientDownloadResultAccessInMethod() {
      const string source = @"
using System;
using System.Net;

class Test {
  private string content;

  public void Refresh(Uri address) {
    using(var client = new WebClient()) {
      content = client.DownloadStringTaskAsync(address).Result;
    }
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
