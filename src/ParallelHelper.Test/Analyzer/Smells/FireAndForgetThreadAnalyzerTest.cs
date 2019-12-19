using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class FireAndForgetThreadAnalyzerTest : AnalyzerTestBase<FireAndForgetThreadAnalyzer> {
    [TestMethod]
    public void ReportsTaskRunFireAndForget() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    Task.Run(() => {});
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 5));
    }

    [TestMethod]
    public void ReportsTaskFactoryStartNewFireAndForget() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    Task.Factory.StartNew(() => {});
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 5));
    }

    [TestMethod]
    public void ReportsGenericTaskRunFireAndForget() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    Task.Run(() => 123);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 5));
    }

    [TestMethod]
    public void ReportsThreadStartFireAndForget() {
      const string source = @"
using System.Threading;

class Test {
  public void DoWork() {
    new Thread(() => {}).Start();
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 5));
    }

    [TestMethod]
    public void DoesNotReportThreadStartedFromVariable() {
      const string source = @"
using System.Threading;

class Test {
  public void DoWork() {
    var thread = new Thread(() => {});
    thread.Start();
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAwaitedTask() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWork() {
    await Task.Run(() => {});
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReturnedTask() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoWork() {
    return Task.Run(() => {});
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTaskAssignedToVariable() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    var task = Task.Run(() => {});
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTaskPassedAsArgument() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    Receiver(Task.Run(() => {}));
  }

  public void Receiver(Task task) {}
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportNonTaskFireAndForget() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public static void DoWork() {
    new Test().Run();
  }

  public void Run() {}
}";
      VerifyDiagnostic(source);
    }
  }
}
