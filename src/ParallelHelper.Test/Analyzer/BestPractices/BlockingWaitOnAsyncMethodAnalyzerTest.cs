using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.BestPractices;

namespace ParallelHelper.Test.Analyzer.BestPractices {
  [TestClass]
  public class BlockingWaitOnAsyncMethodAnalyzerTest : AnalyzerTestBase<BlockingWaitOnAsyncMethodAnalyzer> {
    [TestMethod]
    public void ReportsWaitOnAsyncMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    DoWork2().Wait();
  }

  public async Task DoWork2() {
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 5));
    }

    [TestMethod]
    public void ReportsWaitOnAsyncMethodReturnValue() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    DoWork2().Wait();
  }

  public async Task<int> DoWork2() {
    return await Task.FromResult(1);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 5));
    }

    [TestMethod]
    public void ReportsWaitOnMethodWithAsyncSuffix() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    DoWorkAsync().Wait();
  }

  public Task DoWorkAsync() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 5));
    }

    [TestMethod]
    public void ReportsResultOnMethodWithAsyncSuffixReturningValueTask() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    var result = DoWorkAsync().Result;
  }

  public ValueTask<int> DoWorkAsync() {
    return new ValueTask(1);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 18));
    }

    [TestMethod]
    public void ReportsResultOnAsyncMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    int r = DoWorkAsync().Result;
  }

  public async Task<int> DoWorkAsync() {
    return await Task.FromResult(1);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 13));
    }

    [TestMethod]
    public void DoesNotReportWaitOnTaskRunGuardedAsyncMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    Task.Run(() => DoWorkAsync()).Wait();
  }

  public async Task DoWorkAsync() {
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportResultOnTaskRunGuardedAsyncMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    int r = Task.Run(() => DoWorkAsync()).Result;
  }

  public async Task<int> DoWorkAsync() {
    return await Task.FromResult(1);
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
