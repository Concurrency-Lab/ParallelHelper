using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Bugs;

namespace ParallelHelper.Test.Analyzer.Bugs {
  [TestClass]
  public class NullCheckAgainstTaskInsteadOfValueAnalyzerTest : AnalyzerTestBase<NullCheckAgainstTaskInsteadOfValueAnalyzer> {
    [TestMethod]
    public void ReportsNullCheckAgainstTaskOfAsyncMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  private bool IsNull() {
    return GetValueAsync() == null;
  }

  private async Task<object> GetValueAsync() {
    await Task.Delay(100);
    return new object();
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 12));
    }

    [TestMethod]
    public void ReportsNegatedNullCheckAgainstTaskOfAsyncMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  private bool IsNotNull() {
    return GetValueAsync() != null;
  }

  private async Task<object> GetValueAsync() {
    await Task.Delay(100);
    return new object();
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 12));
    }

    [TestMethod]
    public void ReportsNullCheckAgainstTaskOfMethodWithAsyncSuffix() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  private bool IsNull() {
    return null == GetValueAsync();
  }

  private Task<object> GetValueAsync() {
    return Task.FromResult(new object());
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 12));
    }

    [TestMethod]
    public void DoesNotReportNullCheckAgainstTaskOfMethodWithoutAsyncSuffix() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  private bool IsNull() {
    return GetValue() == null;
  }

  private Task<object> GetValue() {
    return Task.FromResult(new object());
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportNullCheckAgainstAwaitedTask() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  private bool IsNull() {
    return await GetValueAsync() == null;
  }

  private Task<object> GetValueAsync() {
    return Task.FromResult(new object());
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportNullCheckAgainstFalselyNamedMethod() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  private bool IsNull() {
    return GetValueAsync() == null;
  }

  private object GetValueAsync() {
    return new object();
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
