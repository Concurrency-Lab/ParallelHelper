using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class BlockingWaitInAsyncMethodAnalyzerTest : AnalyzerTestBase<BlockingWaitInAsyncMethodAnalyzer> {
    [TestMethod]
    public void ReportsTaskWaitInAsyncVoidMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async void DoWorkAsync() {
    Task.Run(() => {}).Wait();
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 5));
    }

    [TestMethod]
    public void ReportsTaskWaitInTaskReturningMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync() {
    Task.Run(() => {}).Wait();
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 5));
    }

    [TestMethod]
    public void ReportsTaskResultInTaskReturningMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task<int> DoWorkAsync() {
    var value = Task.Run(() => 1).Result;
    return Task.FromResult(value);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 17));
    }

    [TestMethod]
    public void ReportsTaskResultOfAsyncInvocation() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task<int> DoWorkAsync() {
    var value = DoWorkInternalAsync().Result;
    return Task.FromResult(value);
  }

  private Task<int> DoWorkInternalAsync() {
    return Task.FromResult(0);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 17));
    }

    [TestMethod]
    public void DoesNotReportTaskResultInNonAsyncMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public int DoWork() {
    var value = Task.Run(() => 1).Result;
    return value;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTaskWaitInNonAsyncMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    Task.Run(() => { }).Wait();
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
