using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Bugs;

namespace ParallelHelper.Test.Analyzer.Bugs {
  [TestClass]
  public class MultipleAwaitsOnValueTaskAnalyzerTest : AnalyzerTestBase<MultipleAwaitsOnValueTaskAnalyzer> {
    [TestMethod]
    public void ReportsMultipleAwaitsOnValueTaskDefinedOnInitialization() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task<int> GetDoubleAsync() {
    var task = GetValueAsync();
    return await task + await task;
  }

  private ValueTask<int> GetValueAsync() {
    return new ValueTask<int>(1);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 12), new DiagnosticResultLocation(6, 25));
    }

    [TestMethod]
    public void ReportsMultipleAwaitsOnValueTaskDefinedThroughAssignment() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoItDoubleAsync() {
    ValueTask task;
    task = DoItAsync();
    await task;
    await task;
  }

  private ValueTask DoItAsync() {
    return new ValueTask();
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 5), new DiagnosticResultLocation(8, 5));
    }

    [TestMethod]
    public void ReportsAllAwaitsOnValueTaskDefinedThroughAssignment() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoItDoubleAsync() {
    ValueTask task;
    task = DoItAsync();
    await task;
    await task;
    await task;
    await task;
  }

  private ValueTask DoItAsync() {
    return new ValueTask();
  }
}";
      VerifyDiagnostic(
        source,
        new DiagnosticResultLocation(7, 5),
        new DiagnosticResultLocation(8, 5),
        new DiagnosticResultLocation(9, 5),
        new DiagnosticResultLocation(10, 5)
      );
    }

    [TestMethod]
    public void ReportsMultipleAwaitsOnValueTaskDefinedThroughAssignmentEvenIfPassedAsArgument() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoItDoubleAsync() {
    ValueTask task;
    task = DoItAsync();
    await task;
    DoNothing(task);
    await task;
  }

  private ValueTask DoItAsync() {
    return new ValueTask();
  }

  private void DoNothing(ValueTask task) { }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 5), new DiagnosticResultLocation(9, 5));
    }

    [TestMethod]
    public void DoesNotReportMultipleAwaitsOnValueTaskDefinedThroughMultipleAssignment() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoItDoubleAsync() {
    ValueTask task;
    task = DoItAsync();
    await task;
    task = DoItAsync();
    await task;
  }

  private ValueTask DoItAsync() {
    return new ValueTask();
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportMultipleAwaitsOnValueTaskDefinedThroughInitializationAndAssignment() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoItDoubleAsync() {
    var task = DoItAsync();
    task = DoItAsync();
    await task;
    await task;
  }

  private ValueTask DoItAsync() {
    return new ValueTask();
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportMultipleAwaitsOnValueTaskDefinedThroughAssignmentAndRefArgument() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoItDoubleAsync() {
    ValueTask task;
    task = DoItAsync();
    await task;
    Change(ref task);
    await task;
  }

  private ValueTask DoItAsync() {
    return new ValueTask();
  }

  private void Change(ref ValueTask task) {
    task = new ValueTask();
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportMultipleAwaitsOnValueTaskDefinedThroughAssignmentAndOutArgument() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoItDoubleAsync() {
    ValueTask task;
    task = DoItAsync();
    await task;
    Change(out task);
    await task;
  }

  private ValueTask DoItAsync() {
    return new ValueTask();
  }

  private void Change(out ValueTask task) {
    task = new ValueTask();
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportMultipleAwaitsOnTaskDefinedOnInitialization() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task<int> GetDoubleAsync() {
    var task = GetValueAsync();
    return await task + await task;
  }

  private Task<int> GetValueAsync() {
    return Task.FromResult(1);
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportSingleAwaitOnValueTaskDefinedOnInitialization() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task<int> GetDoubleAsync() {
    var task = GetValueAsync();
    return await task;
  }

  private ValueTask<int> GetValueAsync() {
    return new ValueTask<int>(1);
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportDoubleAwaitOfValueTaskReturnedByMethodInvocation() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task<int> GetDoubleAsync() {
    return await GetValueAsync() + await GetValueAsync();
  }

  private ValueTask<int> GetValueAsync() {
    return new ValueTask<int>(1);
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
