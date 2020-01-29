using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class MonitorLockInAsyncMethodAnalyzerTest : AnalyzerTestBase<MonitorLockInAsyncMethodAnalyzer> {
    [TestMethod]
    public void ReportsLockStatementInsideAsyncMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  private readonly object syncObject = new object();

  public async Task DoWorkAsync() {
    lock(syncObject) {
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 5));
    }

    [TestMethod]
    public void ReportsLockStatementInsideAsyncLambdaExpression() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  private readonly object syncObject = new object();

  public Task DoWorkAsync() {
    Func<Task> job = async () => {
      lock(syncObject) {
      }
    };
    return job();
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(9, 7));
    }

    [TestMethod]
    public void ReportsLockStatementInsideAsyncAnonymousFunction() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  private readonly object syncObject = new object();

  public Task DoWorkAsync() {
    Func<Task> job = async delegate () {
      lock(syncObject) {
      }
    };
    return job();
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(9, 7));
    }

    [TestMethod]
    public void ReportsLockStatementInsideAsyncInnerMethodDefinedInNonAsyncMethod() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  private readonly object syncObject = new object();

  public void DoWork() {
    async Task DoWorkInternal() {
      lock(syncObject) {}
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(9, 7));
    }

    [TestMethod]
    public void DoesNotReportLockStatementInsideNonAsyncMethod() {
      const string source = @"
class Test {
  private readonly object syncObject = new object();

  public void DoWork() {
    lock(syncObject) {
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportLockStatementInsideNonAsyncLambdaExpression() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  private readonly object syncObject = new object();

  public void DoWork() {
    Action job = () => {
      lock(syncObject) {
      }
    };
    job();
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportLockStatementInsideNonAsyncAnonymousFunction() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  private readonly object syncObject = new object();

  public void DoWork() {
    Action job = delegate () {
      lock(syncObject) {
      }
    };
    job();
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportLockStatementInsideNonAsyncLambdaExpressionDefinedInAsyncMethod() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  private readonly object syncObject = new object();

  public async Task DoWork() {
    Action job = () => {
      lock(syncObject) {
      }
    };
    job();
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportLockStatementInsideNonAsyncAnonymousFunctionDefinedInAsyncMethod() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  private readonly object syncObject = new object();

  public async Task DoWork() {
    Action job = delegate () {
      lock(syncObject) {
      }
    };
    job();
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportLockStatementInsideNonAsyncInnerMethodDefinedInAsyncMethod() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  private readonly object syncObject = new object();

  public async Task DoWork() {
    void DoWorkInternal() {
    }
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
