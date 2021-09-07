using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Bugs;

namespace ParallelHelper.Test.Analyzer.Bugs {
  [TestClass]
  public class AwaitOnConditionalAccessAnalyzerTest : AnalyzerTestBase<AwaitOnConditionalAccessAnalyzer> {
    [TestMethod]
    public void ReportsAwaitOnDirectConditionalAccess() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWork() {
    var test = new Sub();
    await test?.Task;
  }

  private class Sub {
    public Task Task { get; }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 5));
    }

    [TestMethod]
    public void ReportsAwaitOnDeeplyNestedConditionalAccess() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWork() {
    var test = new Sub();
    await test.Nested.Nested?.Nested.Nested.Nested.Task;
  }

  private class Sub {
    public Sub Nested { get; }
    public Task Task { get; }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 5));
    }

    [TestMethod]
    public void ReportsMultipleConditionalAccessesOnlyOnce() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWork() {
    var test = new Sub();
    await test?.Nested?.Nested?.Task;
  }

  private class Sub {
    public Sub Nested { get; }
    public Task Task { get; }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 5));
    }

    [TestMethod]
    public void ReportsAwaitOnWithParenthesesNestedConditionalAccess() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWork() {
    var test = new Sub();
    await (test?.Nested).Task;
  }

  private class Sub {
    public Sub Nested { get; }
    public Task Task { get; }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 5));
    }

    [TestMethod]
    public void ReportsAwaitOnWithCastAndNestedConditionalAccess() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWork() {
    var test = new Sub();
    await ((Sub)test?.Nested).Task;
  }

  private class Sub {
    public Sub Nested { get; }
    public Task Task { get; }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 5));
    }

    [TestMethod]
    public void ReportsAwaitOnAsExpressionNestedInParentheses() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWork() {
    var test = new Sub();
    await (test as Sub).Nested.Task;
  }

  private class Sub {
    public Sub Nested { get; }
    public Task Task { get; }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 5));
    }

    [TestMethod]
    public void DoesReportsDirectConditionalAccessWithoutAwait() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Task DoWork() {
    var test = new Sub();
    var t = test?.Task;
  }

  private class Sub {
    public Task Task { get; }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAwaitWithoutConditionalAccess() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWork() {
    var test = new Sub();
    await test.Task;
  }

  private class Sub {
    public Task Task { get; }
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
