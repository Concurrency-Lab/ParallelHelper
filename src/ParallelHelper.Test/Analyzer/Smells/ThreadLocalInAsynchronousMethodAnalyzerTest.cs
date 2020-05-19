using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class ThreadLocalInAsynchronousMethodAnalyzerTest : AnalyzerTestBase<ThreadLocalInAsynchronousMethodAnalyzer> {
    [TestMethod]
    public void ReportsThreadLocalReadInAsyncMethod() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  private readonly ThreadLocal<int> count = new ThreadLocal<int>();

  public async Task<int> GetCountAsync() {
    await Task.Delay(100);
    return count.Value;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(9, 12));
    }

    [TestMethod]
    public void ReportsThreadLocalWriteInAsyncDelegate() {
      const string source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class Test {
  private readonly ThreadLocal<int> count = new ThreadLocal<int>();

  public void DoWork() {
    Func<Task> reset = async delegate {
      await Task.Delay(100);
      count.Value = 0;
    };
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(11, 7));
    }

    [TestMethod]
    public void ReportsThreadLocalReadWriteInAsyncParenthesizedLambda() {
      const string source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class Test {
  private readonly ThreadLocal<int> count = new ThreadLocal<int>();

  public void DoWork() {
    Func<int, Task<int>> getAndSet = async (newValue) => {
      var oldValue = count.Value;
      await Task.Delay(100);
      count.Value = newValue;
      return oldValue;
    };
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(10, 22), new DiagnosticResultLocation(12, 7));
    }

    [TestMethod]
    public void ReportsThreadLocalReadInAsyncLambda() {
      const string source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class Test {
  private readonly ThreadLocal<int> count = new ThreadLocal<int>();

  public void DoWork() {
    Func<Task<int>> action = async () => count.Value;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(9, 42));
    }

    [TestMethod]
    public void ReportsThreadLocalWriteInAsyncLocalFunctionInMethod() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  private readonly ThreadLocal<int> count = new ThreadLocal<int>();

  public void DoWork() {
    async Task Reset() => count.Value = 0;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(8, 27));
    }

    [TestMethod]
    public void DoesNotReportThreadLocalReadInNonAsyncMethod() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  private readonly ThreadLocal<int> count = new ThreadLocal<int>();

  public int GetValue() {
    return count.Value;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportLocalWriteInLambdaInAsyncMethod() {
      const string source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class Test {
  private readonly ThreadLocal<int> count = new ThreadLocal<int>();

  public async Task DoWork() {
    Action reset = () => count.Value = 0;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportThreadLocalReadInLocalFunctionInAsyncMethod() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  private readonly ThreadLocal<int> count = new ThreadLocal<int>();

  public async Task DoWork() {
    int Count() => count.Value;
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
