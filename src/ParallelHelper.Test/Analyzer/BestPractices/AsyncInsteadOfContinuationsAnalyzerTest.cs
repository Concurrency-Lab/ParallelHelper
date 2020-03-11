using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.BestPractices;

namespace ParallelHelper.Test.Analyzer.BestPractices {
  [TestClass]
  public class AsyncInsteadOfContinuationsAnalyzerTest : AnalyzerTestBase<AsyncInsteadOfContinuationsAnalyzer> {
    [TestMethod]
    public void ReportsTaskContinueWithInAsyncMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWorkInBackground() {
    await Task.Run(() => {})
      .ContinueWith(t => {});
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 11));
    }

    [TestMethod]
    public void ReportsTaskContinueWithInMethodReturningTask() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoWorkInBackground() {
    return Task.Run(() => {})
      .ContinueWith(t => {});
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 12));
    }

    [TestMethod]
    public void ReportsTaskContinueWithInAsyncDelegate() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public void DoWorkInBackground() {
    Func<Task> action = async delegate {
      await Task.Run(() => {}).ContinueWith(t => {});
    };
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 13));
    }

    [TestMethod]
    public void ReportsTaskContinueWithInDelegateReturningTask() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public void DoWorkInBackground() {
    Func<Task> action = delegate {
      return Task.Run(() => 1).ContinueWith(t => {});
    };
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 14));
    }

    [TestMethod]
    public void ReportsTaskContinueWithInAsyncLambda() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public void DoWorkInBackground() {
    Func<Task> action = async () => {
      await Task.Run(() => {}).ContinueWith(t => {});
    };
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 13));
    }

    [TestMethod]
    public void ReportsTaskContinueWithInLambdaReturningTask() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public void DoWorkInBackground() {
    Func<Task> action = () => {
      return Task.Run(() => {}).ContinueWith(t => {});
    };
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 14));
    }

    [TestMethod]
    public void ReportsTaskContinueWithInSimpleLambda() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public void DoWorkInBackground() {
    Func<Task> action = () => Task.Run(() => {}).ContinueWith(t => {});
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 31));
    }

    [TestMethod]
    public void DoesNotReportTaskContinueWithInMethodReturningVoid() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void DoWorkInBackground() {
    Task.Run(() => {})
      .ContinueWith(t => {});
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTaskConfigureAwaitInAsyncMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWorkInBackground() {
    await Task.Run(() => {}).ConfigureAwait(false);
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportNonTaskContinueWithInAsyncMethod() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkInBackground() {
    await new SomeType().ContinueWith(t => {});
  }

  private class SomeType {
    public Task ContinueWith(Action<Task> continuation) {
      return Task.CompletedTask;
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTaskContinueWithInNonAsyncLambda() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public void DoWorkInBackground() {
    Action action = () => {
      Task.Run(() => {}).ContinueWith(t => {});
    };
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
