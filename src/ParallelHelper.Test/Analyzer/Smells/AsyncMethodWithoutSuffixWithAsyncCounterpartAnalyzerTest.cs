using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class AsyncMethodWithoutSuffixWithAsyncCounterpartAnalyzerTest : AnalyzerTestBase<AsyncMethodWithoutSuffixWithAsyncCounterpartAnalyzer> {
    [TestMethod]
    public void ReportsAsyncVoidMethodWithoutAsyncSuffixThatHasCounterpartWithAsyncSuffix() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async void DoWork() {
  }

  public Task DoWorkAsync() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(4, 3));
    }

    [TestMethod]
    public void ReportsAsyncTaskMethodWithoutAsyncSuffixThatHasCounterpartWithAsyncSuffix() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWork() {
  }

  public Task DoWorkAsync() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(4, 3));
    }

    [TestMethod]
    public void ReportsTaskReturningMethodWithoutAsyncSuffixThatHasCounterpartWithAsyncSuffix() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoWork() {
    return Task.CompletedTask;
  }

  public Task DoWorkAsync() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(4, 3));
    }

    [TestMethod]
    public void ReportsValueTaskReturningMethodWithoutAsyncSuffixThatHasCounterpartWithAsyncSuffix() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task<int> DoWork() {
    return Task.FromResult(0);
  }

  public Task DoWorkAsync() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(4, 3));
    }

    [TestMethod]
    public void DoesNotReportSynchronousMethodWithoutAsyncSuffixThatHasCounterpartWithAsyncSuffix() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void DoWork() {
  }

  public Task DoWorkAsync() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }
    [TestMethod]
    public void DoesNotReportAsyncVoidMethodWithoutAsyncSuffixWithoutAsyncCounterpart() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async void DoWork() {
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAsyncTaskMethodWithoutAsyncSuffixWithoutAsyncCounterpart() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWork() {
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTaskReturningMethodWithoutAsyncSuffixWithoutAsyncCounterpart() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoWork() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportValueTaskReturningMethodWithoutAsyncSuffixWithoutAsyncCounterpart() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task<int> DoWork() {
    return Task.FromResult(0);
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAsyncTaskReturningMethodWithoutThatHasAsyncCounterPartWithDifferentSuffix() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWork() {
  }

  public Task DoWorkInternal() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
