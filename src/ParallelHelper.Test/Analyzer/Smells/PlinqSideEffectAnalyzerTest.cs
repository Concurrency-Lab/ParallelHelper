using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class PlinqSideEffectAnalyzerTest : AnalyzerTestBase<PlinqSideEffectAnalyzer> {
    [TestMethod]
    public void ReportsLocalVariableIncrementedInPlinqExpression() {
      const string source = @"
using System.Collections.Generic;
using System.Linq;

class Test {
  public void DoWork(IEnumerable<int> input) {
    int x = 0;
    var result = (from i in input.AsParallel() select i + ++x).ToArray();
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 19));
    }

    [TestMethod]
    public void DoesNOtReportLocalVariableIncrementedInNonPlinqExpression() {
      const string source = @"
using System.Collections.Generic;
using System.Linq;

class Test {
  public void DoWork(IEnumerable<int> input) {
    int x = 0;
    var result = (from i in input select i + ++x).ToArray();
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void ReportsLocalVariableWrittenByRefInPlinqExpression() {
      const string source = @"
using System.Collections.Generic;
using System.Linq;

class Test {
  public void DoWork(IEnumerable<int> input) {
    int x = 0;
    var result = (from i in input.AsParallel() select i + Increment(ref x)).ToArray();
  }

  private int Increment(ref int x) {
    return ++x;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 19));
    }

    [TestMethod]
    public void DoesNotReportLocalVariableUsedByCallByValueInPlinqExpression() {
      const string source = @"
using System.Collections.Generic;
using System.Linq;

class Test {
  public void DoWork(IEnumerable<int> input) {
    int x = 0;
    var result = (from i in input.AsParallel() select i + Increment(x)).ToArray();
  }

  private int Increment(int x) {
    return ++x;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void ReportsLocalVariableAssignedInGroupedPlinqExpression() {
      const string source = @"
using System.Collections.Generic;
using System.Linq;

class Test {
  public void DoWork(IEnumerable<int> input) {
    int x = 0;
    var result = from i in input.AsParallel()
                  group i by i into n
                  select n.Key + (x += 1);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 18));
    }

    [TestMethod]
    public void ReportsLocalVariableDecrementedInPlinqExtensionMethod() {
      const string source = @"
using System.Collections.Generic;
using System.Linq;

class Test {
  public void DoWork(IEnumerable<int> input) {
    int x = 0;
    input.AsParallel().Select(i => i + x--);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 5));
    }

    [TestMethod]
    public void DoesNotReportLocalVariableDecrementedInNonPlinqExtensionMethod() {
      const string source = @"
using System.Collections.Generic;
using System.Linq;

class Test {
  public void DoWork(IEnumerable<int> input) {
    int x = 0;
    input.Select(i => i + --x);
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportLocalVariableDecrementedInNonParallelExtensionMethod() {
      const string source = @"
using System.Collections.Generic;
using System.Linq;

class Test {
  public void DoWork(IEnumerable<int> input) {
    int x = 0;
    input.Select(i => i + --x).AsParallel();
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportLocalVariableAssignedSequentialPlinqExtensionMethod() {
      const string source = @"
using System.Collections.Generic;
using System.Linq;

class Test {
  public void DoWork(IEnumerable<int> input) {
    int x = 0;
    input.AsParallel().Select(i => i * i).AsSequential().Select(i => i + (x *= 5));
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportLocalVariableAssignedEnumerablePlinqExtensionMethod() {
      const string source = @"
using System.Collections.Generic;
using System.Linq;

class Test {
  public void DoWork(IEnumerable<int> input) {
    int x = 0;
    input.AsParallel().Select(i => i * i).AsEnumerable().Select(i => i + (x *= 5));
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void ReportsLocalVariableWrittenByRefInPlinqMethod() {
      const string source = @"
using System.Collections.Generic;
using System.Linq;

class Test {
  public void DoWork(IEnumerable<int> input) {
    int x = 0;
    ParallelEnumerable.Select(ParallelEnumerable.AsParallel(input), i => IncrementAndGetIfEven(ref x));
  }

  private bool IncrementAndGetIfEven(ref int x) {
    return ++x % 2 == 0;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 5));
    }

    [TestMethod]
    public void DoesNotReportPlinqWithoutSideEffectButWithNestedLambdaArgumentAccess() {
      const string source = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Test {
  public void DoWork() {
      var test = new List<string>().AsParallel()
        .Select<string, Func<string>>(file => () => file);
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
