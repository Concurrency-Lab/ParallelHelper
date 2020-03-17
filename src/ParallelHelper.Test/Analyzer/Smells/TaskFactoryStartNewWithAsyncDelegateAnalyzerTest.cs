using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class TaskFactoryStartNewWithAsyncDelegateAnalyzerTest : AnalyzerTestBase<TaskFactoryStartNewWithAsyncDelegateAnalyzer> {
    [TestMethod]
    public void ReportsStartNewWithAsyncMethodReference() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync() {
    return Task.Factory.StartNew(DoItAsync);
  }

  public async Task DoItAsync() { }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 12));
    }

    [TestMethod]
    public void ReportsStartNewWithAsyncLambdaExpression() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync() {
    return Task.Factory.StartNew(async () => {});
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 12));
    }

    [TestMethod]
    public void ReportsStartNewWithAnonymousMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync() {
    return Task.Factory.StartNew(async delegate {});
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 12));
    }

    [TestMethod]
    public void ReportsStartNewWithTaskReturningMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync() {
    return Task.Factory.StartNew(DoItAsync);
  }

  public Task DoItAsync() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 12));
    }

    [TestMethod]
    public void ReportsStartNewWithTaskReturningLambda() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync() {
    return Task.Factory.StartNew(() => Task.FromResult(1));
  }

  public Task DoItAsync() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 12));
    }

    [TestMethod]
    public void ReportsStartNewWithTaskReturningLambdaFromWrapping() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync() {
    return Task.Factory.StartNew(() => DoItAsync());
  }

  public Task DoItAsync() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 12));
    }

    [TestMethod]
    public void DoesNotReportStartNewWithNonAsyncMethodReference() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync() {
    return Task.Factory.StartNew(DoIt);
  }

  public void DoIt() { }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportStartNewWithNonAsyncLambdaExpression() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync() {
    return Task.Factory.StartNew(() => {});
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportStartNewWithNonAnonymousMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync() {
    return Task.Factory.StartNew(delegate {});
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTaskRunWithAsyncMethodReference() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync() {
    return Task.Run(DoItAsync);
  }

  public async Task DoItAsync() { }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTaskRunWithAsyncLambdaExpression() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync() {
    return Task.Run(async () => {});
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTaskRunWithAnonymousMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync() {
    return Task.Run(async delegate {});
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
