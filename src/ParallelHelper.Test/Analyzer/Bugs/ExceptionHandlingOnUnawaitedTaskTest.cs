using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Bugs;

namespace ParallelHelper.Test.Analyzer.Bugs {
  [TestClass]
  public class ExceptionHandlingOnUnawaitedTaskTest : AnalyzerTestBase<ExceptionHandlingOnUnawaitedTask> {
    [TestMethod]
    public void ReportsReturnedAsyncMethodInvocationWithEnclosingTryStatement() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync() {
    try {
      return DoWorkInternalAsync();
    } catch(Exception e) { }
    return Task.CompletedTask;
  }

  private Task DoWorkInternalAsync() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 14));
    }

    [TestMethod]
    public void ReportsReturnedTaskRunInvocationWithEnclosingTryStatement() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync() {
    try {
      return Task.Run(() => {});
    } catch(Exception e) { }
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 14));
    }

    [TestMethod]
    public void DoesNotReportReturnedCompletedTaskEnclosedByTryStatement() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync() {
    try {
      return Task.CompletedTask;
    } catch(Exception e) { }
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReturnedCompletedTaskReturnedInCatchClause() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync() {
    try {
    } catch(Exception e) {
      return Task.FromException(e);
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReturnedCompletedTaskFromVariable() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync() {
    try {
      var temp = Task.CompletedTask;
      return temp;
    } catch(Exception e) { }
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReturnedValueFromAwaitedTask() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public async Task<int> GetAsync() {
    try {
      return await GetInternalAsync();
    } catch(Exception e) { }
    return -1;
  }

  private Task<int> GetInternalAsync() {
    return Task.FromResult(0);
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTaskReturnedInSeparateActivationFrame() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    try {
      await Task.Run(() => {
        return GetInternalAsync();
      });
    } catch(Exception e) {}
  }

  private Task<int> GetInternalAsync() {
    return Task.FromResult(0);
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
