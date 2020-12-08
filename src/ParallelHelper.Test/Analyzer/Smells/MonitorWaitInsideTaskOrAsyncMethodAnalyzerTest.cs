using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class MonitorWaitInsideTaskOrAsyncMethodAnalyzerTest : AnalyzerTestBase<MonitorWaitInsideTaskOrAsyncMethodAnalyzer> {
    [TestMethod]
    public void ReportsMonitorWaitInsideAsyncMethod() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  private readonly object syncObject = new object();

  public async Task DoItAsync() {
    lock(syncObject) {
      Monitor.Wait(syncObject);
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(9, 7));
    }

    [TestMethod]
    public void ReportsMonitorWaitInsideAsyncLambda() {
      const string source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class Test {
  private readonly object syncObject = new object();

  public async Task DoItAsync() {
    Func<Task> action = async () => {
      lock(syncObject) {
        Monitor.Wait(syncObject);
      }
    };
    await action();
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(11, 9));
    }

    [TestMethod]
    public void ReportsMonitorWaitInsideMethodReferencedByTaskRunDelegate() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  private readonly object syncObject = new object();

  public Task StartDoIt() {
    return Task.Run(DoIt);
  }

  public void DoIt() {
    lock(syncObject) {
      Monitor.Wait(syncObject);
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(13, 7));
    }

    [TestMethod]
    public void ReportsMonitorWaitInsideLambdaEncapsulatedByTaskRun() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  private readonly object syncObject = new object();

  public Task StartDoIt() {
    return Task.Run(() => {
      lock(syncObject) {
        Monitor.Wait(syncObject);
      }
    });
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(10, 9));
    }

    [TestMethod]
    public void DoesNotReportMonitorWaitInsideNonAsyncMethod() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  private readonly object syncObject = new object();

  public void DoIt() {
    lock(syncObject) {
      Monitor.Wait(syncObject);
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportMonitorWaitInsideNonAsyncLambda() {
      const string source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class Test {
  private readonly object syncObject = new object();

  public void DoIt() {
    Action action = () => {
      lock(syncObject) {
        Monitor.Wait(syncObject);
      }
    };
    action();
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportMonitorWaitOfSeperateActivationFrameInsideAsyncMethod() {
      const string source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class Test {
  private readonly object syncObject = new object();

  public async Task DoItAsync() {
    Action action = () => {
      lock(syncObject) {
        Monitor.Wait(syncObject);
      }
    };
    action();
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportMonitorWaitWhenTaskRunDelegateIsUnresolvable() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  private readonly object syncObject = new object();

  private Action action;

  public async Task DoItAsync() {
    Task.Run(action);
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
