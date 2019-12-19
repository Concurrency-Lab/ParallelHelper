using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class TaskOnlyReturningValueAnalyzerTest : AnalyzerTestBase<TaskOnlyReturningValueAnalyzer> {
    [TestMethod]
    public void ReportsTaskWithSimpleLambdaReturningStringLiteral() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void RunTest() {
    Task.Run(() => ""test"");
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 5));
    }

    [TestMethod]
    public void ReportsTaskWithSimpleLambdaReturningLocalVariable() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void RunTest() {
    var result = 1;
    Task.Run(() => result);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 5));
    }

    [TestMethod]
    public void ReportsTaskWithSimpleLambdaReturningParameter() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void RunTest(int result) {
    Task.Run(() => result);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 5));
    }

    [TestMethod]
    public void ReportsTaskWithParenthesizedLambdaReturningIntLiteral() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void RunTest() {
    Task.Run(() => { return 10; });
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 5));
    }

    [TestMethod]
    public void ReportsTaskWithParenthesizedLambdaReturningField() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  private object _currentValue;

  public void RunTest() {
    Task.Run(() => {
      return _currentValue;
    });
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 5));
    }

    [TestMethod]
    public void DoesNotReportTaskWithSimpleLambdaReturningMethodInvocation() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void RunTest() {
    Task.Run(() => Compute());
  }

  private int Compute() {
    return 10;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTaskWithDelegate() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void RunTest() {
    Task.Run(Compute);
  }

  private int Compute() {
    return 10;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTaskWithParenthesizedLambdaWithMultipleStatementsReturningLocalVariable() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public void RunTest() {
    Task.Run(() => {
      int i = 10;
      return i;
    });
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
