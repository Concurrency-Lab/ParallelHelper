using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class ParallelForSideEffectAnalyzerTest : AnalyzerTestBase<ParallelForSideEffectAnalyzer> {
    [TestMethod]
    public void ReportsLocalVariableIncrementedInParallelForStatement() {
      const string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    int x = 0;
    Parallel.For(0, 10, i => {
      x++;
    });
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 5));
    }

    [TestMethod]
    public void ReportsLocalVariableAssignedInParallelForEachStatement() {
      const string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    int x = 0;
    Parallel.ForEach(new int[10], i => {
      x += i;
    });
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 5));
    }

    [TestMethod]
    public void DoesNotReportBodyVariableDecrementedInStatementsBody() {
      const string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    Parallel.For(0, 10, i => {
      int x = 0;
      x += i;
    });
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportLocalArrayAssignedInParallelForEachStatement() {
      const string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    var x = new int[10];
    Parallel.For(0, x.Length, i => {
      x[i] = i * i;
    });
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
