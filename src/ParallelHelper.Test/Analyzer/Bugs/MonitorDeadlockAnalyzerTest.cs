using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Bugs;

namespace ParallelHelper.Test.Analyzer.Bugs {
  [TestClass]
  public class MonitorDeadlockAnalyzerTest : AnalyzerTestBase<MonitorDeadlockAnalyzer> {
    [TestMethod]
    public void DoesNotReportSingleLock() {
      const string source = @"
class Test {
  private readonly object syncObject = new object();

  public void DoWork(Test other) {
    lock(syncObject) {
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void ReportsNestedLocksOfDifferentInstances() {
      const string source = @"
class Test {
  private readonly object syncObject = new object();

  public void DoWork(Test other) {
    lock(syncObject) {
      lock(other.syncObject) {
      }
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 5));
    }

    [TestMethod]
    public void ReportsNestedLocksOfDifferentInstancesWhereTheSyncObjectIsUnnecessarilyWrappedInParentheses() {
      const string source = @"
class Test {
  private readonly object syncObject = new object();

  public void DoWork(Test other) {
    lock((syncObject)) {
      lock((((other.syncObject)))) {
      }
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 5));
    }

    [TestMethod]
    public void DoesNotReportNestedLocksOfDifferentSyncObjects() {
      const string source = @"
class Test {
  private readonly object syncObjectA = new object();
  private readonly object syncObjectB = new object();

  public void DoWork() {
    lock(syncObjectA) {
      lock(syncObjectB) {
      }
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportNestedLocksOfTheSameInstance() {
      const string source = @"
class Test {
  private readonly object syncObject = new object();

  public void DoWork(Test other) {
    lock(syncObject) {
      lock(syncObject) {
      }
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportNestedLocksOfLinearLockingOrder() {
      const string source = @"
class Test {
  private readonly object syncObject = new object();
  private readonly int id;

  public Test(int id) {
    this.id = id;
  }

  public void DoWork(Test other) {
    object first, second;
    if(other.id < id) {
      first = other.syncObject;
      second = syncObject;
    } else {
      first = syncObject;
      second = other.syncObject;
    }
  
    lock(first) {
      lock(second) {
      }
    }
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
