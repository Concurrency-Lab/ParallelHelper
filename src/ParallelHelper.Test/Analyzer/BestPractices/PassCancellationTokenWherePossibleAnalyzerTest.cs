using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.BestPractices;

namespace ParallelHelper.Test.Analyzer.BestPractices {
  [TestClass]
  public class PassCancellationTokenWherePossibleAnalyzerTest : AnalyzerTestBase<PassCancellationTokenWherePossibleAnalyzer> {
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
  }
}
