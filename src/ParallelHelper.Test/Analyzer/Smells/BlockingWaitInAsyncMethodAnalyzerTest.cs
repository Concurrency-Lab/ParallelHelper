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
    public void ReportsTaskWaitInAsyncMethodWhenOtherTaskIsAwaited() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    var a = Task.Run(() => {});
    var b = Task.Run(() => {});
    await a;
    b.Wait();
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(8, 5));
    }

    [TestMethod]
    public void ReportsTaskWaitInAsyncMethodWhenOtherTasksAreAwaitedWithWhenAll() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    var a = Task.Run(() => {});
    var b = Task.Run(() => {});
    var c = Task.Run(() => {});
    await Task.WhenAll(a, c);
    b.Wait();
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(9, 5));
    }

    [TestMethod]
    public void ReportsTaskMethodWaitInAsyncMethodWhenMethodWasAwaitedBefore() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    await DoWorkInternalAsync();
    DoWorkInternalAsync().Wait();
  }

  private Task DoWorkInternalAsync() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 5));
    }

    [TestMethod]
    public void ReportsTaskMethodResultInAsyncMethodWhenMethodWasAwaitedBefore() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task<int> DoWorkAsync() {
    await DoWorkInternalAsync();
    return DoWorkInternalAsync().Result;
  }

  private Task<int> DoWorkInternalAsync() {
    return Task.FromResult(1);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 12));
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

    [TestMethod]
    public void DoesNotReportTaskWaitInAsyncMethodNestedInLambda() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    Action<Task> action = t => t.Wait();
    await DoWorkInternalAsync();
  }

  private Task DoWorkInternalAsync() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTaskResultInAsyncMethodNestedInLambda() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    Func<Task<int>, int> action = t => t.Result;
    await DoWorkInternalAsync();
  }

  private Task DoWorkInternalAsync() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTaskResultInAsyncMethodWhenTaskIsAwaitedBefore() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public async Task<int> DoWorkAsync() {
    var task = DoWorkInternalAsync();
    await task;
    return task.Result;
  }

  private Task<int> DoWorkInternalAsync() {
    return Task.FromResult(1);
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTaskWaitInAsyncMethodWhenTaskIsAwaitedBefore() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    var task = DoWorkInternalAsync();
    await task;
    task.Wait();
  }

  private Task DoWorkInternalAsync() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTaskWaitInAsyncMethodWhenTaskIsAwaitedBeforeWithWhenAny() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    var task = DoWorkInternalAsync();
    await Task.WhenAny(task, DoWorkInternalAsync());
    task.Wait();
  }

  private Task DoWorkInternalAsync() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTaskResultInAsyncMethodWhenTaskIsAwaitedBeforeWithWhenAll() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public async Task<int> DoWorkAsync() {
    var task = DoWorkInternalAsync();
    await Task.WhenAll(DoWorkInternalAsync(), task);
    return task.Result;
  }

  private Task<int> DoWorkInternalAsync() {
    return Task.FromResult(1);
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportsTaskWaitInAsyncMethodWhenAllTasksAreAwaitedWithWhenAll() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    var a = Task.Run(() => {});
    var b = Task.Run(() => {});
    var c = Task.Run(() => {});
    await Task.WhenAll(a, b, c);
    a.Wait();
    b.Wait();
    c.Wait();
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportsTaskWaitInAsyncMethodWhenTaskIsAwaitedWithConfigureAwait() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    var task = Task.Run(() => { });
    await task.ConfigureAwait(false);
    task.Wait();
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportsTaskWaitInAsyncMethodWhenAllTasksAreAwaitedWithWhenAllAndConfigureAwait() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    var a = Task.Run(() => {});
    var b = Task.Run(() => {});
    var c = Task.Run(() => {});
    await Task.WhenAll(a, b, c).ConfigureAwait(false);
    a.Wait();
    b.Wait();
    c.Wait();
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
