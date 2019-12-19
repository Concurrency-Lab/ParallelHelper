using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class AwaitSynchronousTaskCompletionAnalyzerTest : AnalyzerTestBase<AwaitSynchronousTaskCompletionAnalyzer> {
    [TestMethod]
    public void ReportsAwaitTaskFromResult() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    await Task.FromResult(0);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 5));
    }

    [TestMethod]
    public void ReportsAwaitTaskCompletedTask() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    await Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 5));
    }

    [TestMethod]
    public void DoesNotReportAwaitTaskRun() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    await Task.Run(() => 0);
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReturnTaskFromResult() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task<int> DoWorkAsync() {
    return Task.FromResult(0);
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReturnTaskCompletedTask() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
