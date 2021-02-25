using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class UnusedSynchronousTaskResultAnalyzerTest : AnalyzerTestBase<UnusedSynchronousTaskResultAnalyzer> {
    [TestMethod]
    public void ReportsReturnFromResultForTaskMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync() {
    return Task.FromResult(0);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 12));
    }

    [TestMethod]
    public void ReportsAwaitOnFromResultInMethodWithoutReturnValue() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    await Task.FromResult(0);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 11));
    }

    [TestMethod]
    public void ReportsAllAwaitsOnFromResultWhoseValueIsNotUsed() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task<int> DoWorkAsync() {
    await Task.FromResult(0);
    await Task.FromResult(1);
    return await Task.FromResult(2);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 11), new DiagnosticResultLocation(6, 11));
    }

    [TestMethod]
    public void ReportsAllReturnFromResultOnTaskMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync(bool completed) {
    if(completed) {
      return Task.FromResult(1);
    }
    return Task.FromResult(0);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 14), new DiagnosticResultLocation(8, 12));
    }

    [TestMethod]
    public void ReportsExpressionBodyTaskFromResultOnMethodWithoutReturnValue() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoAsync() => Task.FromResult(0);
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(4, 28));
    }

    [TestMethod]
    public void ReportsFromResultInsideSimpleLambdaInMethodWithoutReturnValue() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    Func<Task> f = () => Task.FromResult(0);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 26));
    }

    [TestMethod]
    public void ReportsAwaitOnFromResultInsideLambdaInMethodWithoutReturnValue() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    Func<Task> f = async () => {
      await Task.FromResult(0);
    };
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 13));
    }

    [TestMethod]
    public void ReportsAwaitOnFromResultInsideLocalFunctionWithoutReturnValue() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    async Task DoIt() {
      await Task.FromResult(0);
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 13));
    }

    [TestMethod]
    public void ReportsReturnFromResultInsideLocalFunctionWithoutReturnValue() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    Task DoIt() {
      return Task.FromResult(0);
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 14));
    }

    [TestMethod]
    public void ReportsFromResultInsideExpressionBodiedLocalFunctionWithoutReturnValue() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    Task DoIt() => Task.FromResult(0);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 20));
    }

    [TestMethod]
    public void DoesNotReportReturnFromResultOnTaskMethodWithReturnValue() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task<int> DoWorkAsync(bool completed) {
    if(completed) {
      return Task.FromResult(1);
    }
    return Task.FromResult(0);
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAwaitInsideExpression() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    var result = await Task.FromResult(0) * 1;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportExpressionBodyTaskFromResultOnMethodWithReturnValue() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task<int> GetAsync() => Task.FromResult(0);
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportFromResultInExpressionBodiedLambdaInsideAsyncMethodWithoutReturnValue() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    Func<Task<int>> f = () => Task.FromResult(0);
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportFromResultInLambdaInsideAsyncMethodWithoutReturnValue() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    Func<Task<int>> f = () => {
      return Task.FromResult(0);
    };
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
