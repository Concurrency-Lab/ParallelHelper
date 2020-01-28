using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class ThreadSleepInAsynchronousMethodAnalyzerTest : AnalyzerTestBase<ThreadSleepInAsynchronousMethodAnalyzer> {
    [TestMethod]
    public void ReportsThreadSleepInAsyncMethod() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  public async Task DoWork() {
    Thread.Sleep(100);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 5));
    }

    [TestMethod]
    public void ReportsThreadSleepInAsyncDelegate() {
      const string source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    Func<Task> action = async delegate {
      Thread.Sleep(100);
    };
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(8, 7));
    }

    [TestMethod]
    public void ReportsThreadSleepInAsyncParenthesizedLambda() {
      const string source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    Func<Task> action = async () => {
      Thread.Sleep(100);
    };
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(8, 7));
    }

    [TestMethod]
    public void ReportsThreadSleepInAsyncLambda() {
      const string source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    Func<Task> action = async () => Thread.Sleep(100);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 37));
    }

    [TestMethod]
    public void ReportsThreadSleepInAsyncLocalFunctionInMethod() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    async Task Sleep() => Thread.Sleep(100);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 27));
    }

    [TestMethod]
    public void DoesNotReportThreadSleepInNonAsyncMethod() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    Thread.Sleep(100);
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportThreadSleepInLambdaInAsyncMethod() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  public async Task DoWork() {
    Action sleep = () => Thread.Sleep(100);
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
