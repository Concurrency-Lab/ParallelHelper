using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.BestPractices;

namespace ParallelHelper.Test.Analyzer.BestPractices {
  [TestClass]
  public class UnusedCancellationTokenFromEnclosingScopeAnalyzerTest : AnalyzerTestBase<UnusedCancellationTokenFromEnclosingScopeAnalyzer> {
    [TestMethod]
    public void ReportsMissingPassthroughOfCancellationTokenForMethodWithDefaultArguments() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync1(CancellationToken cancellationToken = default) {
    await DoWorkAsync2();
  }

  public Task DoWorkAsync2(CancellationToken cancellationToken = default) {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 11));
    }

    [TestMethod]
    public void ReportsMissingPassthroughOfCancellationTokenForMethodIfDefaultIsPassed() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync1(CancellationToken cancellationToken) {
    await DoWorkAsync2(default);
  }

  public Task DoWorkAsync2(CancellationToken cancellationToken) {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 11));
    }

    [TestMethod]
    public void ReportsMissingPassOfFieldCancellationTokenForMethodWithDefaultArguments() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  private readonly CancellationTokenSource cancellationToken = new CancellationTokenSource();

  public async Task DoWorkAsync1() {
    await DoWorkAsync2();
  }

  public Task DoWorkAsync2(CancellationToken cancellationToken = default) {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(8, 11));
    }

    [TestMethod]
    public void ReportsMissingPassOfPropertyCancellationTokenForMethodWithDefaultArguments() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  private CancellationTokenSource CancellationToken { get; } = new CancellationTokenSource();

  public async Task DoWorkAsync1() {
    await DoWorkAsync2();
  }

  public Task DoWorkAsync2(CancellationToken cancellationToken = default) {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(8, 11));
    }

    [TestMethod]
    public void ReportsMissingPassOfPropertyCancellationTokenOfBaseTypeForMethodWithDefaultArguments() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Base {
  protected CancellationTokenSource CancellationToken { get; } = new CancellationTokenSource();
}

class Test : Base {

  public async Task DoWorkAsync1() {
    await DoWorkAsync2();
  }

  public Task DoWorkAsync2(CancellationToken cancellationToken = default) {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(11, 11));
    }

    [TestMethod]
    public void ReportsMissingPassthroughOfCancellationTokenInsideLambdaForMethodWithDefaultArguments() {
      const string source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class Test {
  public Action<CancellationToken> DoIt = token => DoWorkAsync2();

  public static Task DoWorkAsync2(CancellationToken cancellationToken = default) {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 52));
    }

    [TestMethod]
    public void ReportsMissingPassthroughOfCancellationTokenForMethodsWithOverload() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync1(CancellationToken cancellationToken = default) {
    await DoWorkAsync2();
  }

  public Task DoWorkAsync2() {
    return Task.CompletedTask;
  }

  public Task DoWorkAsync2(CancellationToken cancellationToken) {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 11));
    }

    [TestMethod]
    public void DoesNotReportIfCancellationTokenIsPassed() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync1(CancellationToken cancellationToken = default) {
    await DoWorkAsync2(cancellationToken);
  }

  public Task DoWorkAsync2(CancellationToken cancellationToken = default) {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportIfTargetDoesNotAcceptCancellationToken() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync1(CancellationToken cancellationToken = default) {
    await DoWorkAsync2();
  }

  public Task DoWorkAsync2() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportIfMethodDoesNotAcceptCancellationToken() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync1(int argument) {
    await DoWorkAsync2();
  }

  public Task DoWorkAsync2(CancellationToken cancellationToken = default) {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportIfNoOverloadAcceptsMissingCancellationToken() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync1(CancellationToken cancellationToken = default) {
    await DoWorkAsync2();
  }

  public Task DoWorkAsync2() {
    return Task.CompletedTask;
  }

  public Task DoWorkAsync2(int value) {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportIfMethodAcceptsCancellationTokenThatIsNoOverload() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync1(CancellationToken cancellationToken = default) {
    await DoWorkAsync2();
  }

  public Task DoWorkAsync2() {
    return Task.CompletedTask;
  }

  public Task DoWorkAsync3(CancellationToken cancellationToken) {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportPassOfFieldCancellationTokenIfNoSuchFieldForMethodWithDefaultArguments() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  private readonly string cancellationToken = null;

  public async Task DoWorkAsync1() {
    await DoWorkAsync2();
  }

  public Task DoWorkAsync2(CancellationToken cancellationToken = default) {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportMissingPassthroughOfCancellationTokenInsideLambdaForMethodWithDefaultArgumentsIfNoTokenIsReceived() {
      const string source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class Test {
  public Action<string> DoIt = token => new Test().DoWorkAsync2();

  public Task DoWorkAsync2(CancellationToken cancellationToken = default) {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportIfInvokedMethodIsInstanceAndOverloadIsStatic() {
      const string source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync(CancellationToken cancellationToken) {
    return DoWorkAsync2();
  }

  public Task DoWorkAsync2() {
    return Task.CompletedTask;
  }

  public static Task DoWorkAsync2(CancellationToken cancellationToken) {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportInstanceCancellationTokenIfCurrentMethodIsStatic() {
      const string source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class Test {
  private readonly CancellationToken cancellationToken;

  public static Task DoWorkAsync() {
    return new Test().DoWorkAsync2();
  }

  public Task DoWorkAsync2() {
    return Task.CompletedTask;
  }

  public Task DoWorkAsync2(CancellationToken cancellationToken) {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotCrashForAnonymousMethodWithoutParameters() {
      const string source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class Test {
  private readonly CancellationToken cancellationToken;

  public Action<Task> CreaeWorkTaskFactory() {
    Action<Task> action = async delegate { await DoWorkAsync(cancellationToken); };
    return action;
  }

  public Task DoWorkAsync(CancellationToken cancellationToken = default) {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportMissingPassOfPrivatePropertyCancellationTokenOfBaseTypeForMethodWithDefaultArguments() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Base {
  private CancellationTokenSource CancellationToken { get; } = new CancellationTokenSource();
}

class Test : Base {

  public async Task DoWorkAsync1() {
    await DoWorkAsync2();
  }

  public Task DoWorkAsync2(CancellationToken cancellationToken = default) {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(11, 11));
    }


    [TestMethod]
    public void DoesNotReportIfMissingForMethodThatIsExcludedByDefault() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync1(CancellationToken cancellationToken = default) {
    await Task.Run(() => {});
  }
}";
      VerifyDiagnostic(source);
    }


    [TestMethod]
    public void DoesNotReportIfMissingForMethodThatIsManuallyExcluded() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync1(CancellationToken cancellationToken = default) {
    await Task.Delay(1000);
  }
}";
      CreateAnalyzerCompilationBuilder()
        .AddSourceTexts(source)
        .AddAnalyzerOption("dotnet_diagnostic.PH_P007.exclusions", "System.Threading.Tasks.Task:Delay")
        .VerifyDiagnostic();
    }
  }
}
