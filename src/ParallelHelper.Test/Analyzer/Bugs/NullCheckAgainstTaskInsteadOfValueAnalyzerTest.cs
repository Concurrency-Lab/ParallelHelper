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
    public void ReportsNullCheckAgainstTaskOfMethodWithAsyncSuffixReferencedByVariable() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  private bool IsNull() {
    var task = GetValueAsync();
    return task == null;
  }

  private Task<object> GetValueAsync() {
    return Task.FromResult(new object());
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 12));
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
  private async Task<bool> IsNullAsync() {
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

    [TestMethod]
    public void DoesNotReportCheckAgainstOtherTasks() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  private bool IsSame() {
    return GetValueAsync() == Task.FromResult(new object());
  }

  private Task<object> GetValueAsync() {
    return Task.FromResult(new object());
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportCheckAgainstOtherLiterals() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  private bool IsSame() {
    return GetValueAsync() == ""123"";
  }

  private Task<object> GetValueAsync() {
    return Task.FromResult(new object());
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportNullCheckAgainstTaskOfMethodWithAsyncSuffixReferencedByVariableThatIsPotentiallyNull() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public int X { get; set; }

  private bool IsNull() {
    var task = GetValueAsync();
    if(X > 0) {
      task = null;
    }
    return task == null;
  }

  private Task<object> GetValueAsync() {
    return Task.FromResult(new object());
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportNullCheckAgainstTaskOfMethodWithAsyncSuffixReferencedByVariableThatIsPotentiallyNullByAnotherActivationFrame() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public int X { get; set; }

  private bool IsNull() {
    var task = GetValueAsync();
    Task.Run(() => task = null);
    return task == null;
  }

  private Task<object> GetValueAsync() {
    return Task.FromResult(new object());
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportNullCheckAgainstTaskOfMethodWithAsyncSuffixReferencedByVariableThatIsPotentiallyNullByAnotherActivationFrameThroughRef() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  public int X { get; set; }

  private async Task<bool> IsNullAsync() {
    var task = GetValueAsync();
    await Task.Run(() => SetNull(ref task));
    return task == null;
  }

  private void SetNull(ref Task<object> task) {
    task = null;
  }

  private Task<object> GetValueAsync() {
    return Task.FromResult(new object());
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
