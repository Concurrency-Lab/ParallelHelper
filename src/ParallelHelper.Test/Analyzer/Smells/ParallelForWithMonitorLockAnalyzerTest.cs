using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class ParallelForWithMonitorLockAnalyzerTest : AnalyzerTestBase<ParallelForWithMonitorLockAnalyzer> {
    [TestMethod]
    public void ReportsMonitorLockInsideParallelFor() {
      const string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    var syncObject = new object();
    int x = 0;
    Parallel.For(0, 10, i => {
      lock(syncObject) {
        x++;
      }
    });
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(8, 5));
    }

    [TestMethod]
    public void ReportsMonitorLockInsideParallelForEach() {
      const string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    var syncObject = new object();
    int x = 0;
    Parallel.ForEach(new int[10], i => {
      lock(syncObject) {
        x += i;
      }
    });
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(8, 5));
    }

    [TestMethod]
    public void DoesNotReportParallelForWithoutLockStatement() {
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
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportParallelForEachWithoutLockStatement() {
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
      VerifyDiagnostic(source);
    }
  }
}
