using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class AsyncMethodWithTaskImplementationAnalyzerTest : AnalyzerTestBase<AsyncMethodWithTaskImplementationAnalyzer> {
    [TestMethod]
    public void ReportsReturnedTaskRun() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync() {
    return Task.Run(() => {});
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(4, 3));
    }

    [TestMethod]
    public void ReportsReturnedGenericTaskRun() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task<int> DoWorkAsync() {
    return Task.Run(() => 1);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(4, 3));
    }

    [TestMethod]
    public void ReportsReturnedTaskFactoryStartNew() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task<int> DoWorkAsync() {
    return Task.Factory.StartNew(() => 1);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(4, 3));
    }

    [TestMethod]
    public void ReportsExpressionBodiedTaskRun() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync() => Task.Run(() => {});
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(4, 3));
    }
    [TestMethod]
    public void ReportsTaskRunForInterfaceImplementationIfEnabled() {
      const string source = @"
using System.Threading.Tasks;

interface ITest {
  Task DoItAsync();
}

class Test : ITest {
  public Task DoItAsync() {
    return Task.Run(() => {});
  }
}";
      CreateAnalyzerCompilationBuilder()
        .AddSourceTexts(source)
        .AddAnalyzerOption("dotnet_diagnostic.PH_S005.overrides", "report")
        .VerifyDiagnostic(new DiagnosticResultLocation(8, 3));
    }

    [TestMethod]
    public void ReportsTaskRunForMethodOverridesIfEnabled() {
      const string source = @"
using System.Threading.Tasks;

class TestBase {
  public virtual Task DoItAsync() { return Task.CompletedTask; }
}

class Test : TestBase {
  public override Task DoItAsync() {
    return Task.Run(() => {});
  }
}";
      CreateAnalyzerCompilationBuilder()
        .AddSourceTexts(source)
        .AddAnalyzerOption("dotnet_diagnostic.PH_S005.overrides", "report")
        .VerifyDiagnostic(new DiagnosticResultLocation(8, 3));
    }

    [TestMethod]
    public void DoesNotReportTaskRunWithoutAsyncSuffix() {
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
    public void DoesNotReportExpressionBodiedTaskRunWithoutAsyncSuffix() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoWork() => Task.Run(() => {});
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportDirectInvocationOfAsyncMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync() => DoWorkInternalAsync();

  private async Task DoWorkInternalAsync() {

  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAwaitInvocationOfAsyncMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    await DoWorkInternalAsync();
  }

  private async Task DoWorkInternalAsync() {

  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTaskRunForInterfaceImplementationByDefault() {
      const string source = @"
using System.Threading.Tasks;

interface ITest {
  Task DoItAsync();
}

class Test : ITest {
  public Task DoItAsync() {
    return Task.Run(() => {});
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTaskRunForInterfaceImplementationIfDisabled() {
      const string source = @"
using System.Threading.Tasks;

interface ITest {
  Task DoItAsync();
}

class Test : ITest {
  public Task DoItAsync() {
    return Task.Run(() => {});
  }
}";
      CreateAnalyzerCompilationBuilder()
        .AddSourceTexts(source)
        .AddAnalyzerOption("dotnet_diagnostic.PH_S005.overrides", "ignore")
        .VerifyDiagnostic();
    }

    [TestMethod]
    public void DoesNotReportTaskRunForMethodOverridesByDefault() {
      const string source = @"
using System.Threading.Tasks;

class TestBase {
  public virtual Task DoItAsync() { return Task.CompletedTask; }
}

class Test : TestBase {
  public override Task DoItAsync() {
    return Task.Run(() => {});
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTaskRunForMethodOverridesIfDisabled() {
      const string source = @"
using System.Threading.Tasks;

class TestBase {
  public virtual Task DoItAsync() { return Task.CompletedTask; }
}

class Test : TestBase {
  public override Task DoItAsync() {
    return Task.Run(() => {});
  }
}";
      CreateAnalyzerCompilationBuilder()
        .AddSourceTexts(source)
        .AddAnalyzerOption("dotnet_diagnostic.PH_S005.overrides", "ignore")
        .VerifyDiagnostic();
    }
  }
}
