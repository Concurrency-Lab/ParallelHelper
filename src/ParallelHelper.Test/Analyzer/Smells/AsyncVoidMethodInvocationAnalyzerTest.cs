using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class AsyncVoidMethodInvocationAnalyzerTest : AnalyzerTestBase<AsyncVoidMethodInvocationAnalyzer> {
    [TestMethod]
    public void ReportsInvocationOfAsyncVoidMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void DoIt() {
    DoItAsync();
  }

  public async void DoItAsync() {}
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 5));
    }

    [TestMethod]
    public void DoesNotReportInvocationOfAsyncTaskMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void DoIt() {
    DoItAsync();
  }

  public async Task DoItAsync() {}
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportInvocationOfTaskMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void DoIt() {
    DoItAsync();
  }

  public Task DoItAsync() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportInvocationOfVoidMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void DoIt() {
    DoIt2();
  }

  public void DoIt2() {}
}";
      VerifyDiagnostic(source);
    }
  }
}
