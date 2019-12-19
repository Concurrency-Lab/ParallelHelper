using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Bugs;

namespace ParallelHelper.Test.Analyzer.Bugs {
  [TestClass]
  public class NonAtomicReadWriteOnVolatileAnalyzerTest : AnalyzerTestBase<NonAtomicReadWriteOnVolatileAnalyzer> {
    [TestMethod]
    public void ReportsPostfixUnaryIncrementOnVolatileFieldInPublicMember() {
      const string source = @"
class Test {
  private volatile int count;

  public void Increment() {
    count++;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 5));
    }

    [TestMethod]
    public void ReportsPrefixUnaryDecrementOnVolatileFieldInPublicMember() {
      const string source = @"
class Test {
  private volatile int count;

  public void Decrement() {
    --count;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 5));
    }

    [TestMethod]
    public void ReportsAddAssignmentOnVolatileFieldInPublicMember() {
      const string source = @"
class Test {
  private volatile int count;

  public void Add(int amount) {
    count += amount;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 5));
    }

    [TestMethod]
    public void DoesNotReportAddAssignmentOnNonVolatileFieldInPublicMember() {
      const string source = @"
class Test {
  private int count;

  public void Add(int amount) {
    count += amount;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAddAssignmentOnVolatileFieldInPrivateMember() {
      const string source = @"
class Test {
  private volatile int count;

  private void Add(int amount) {
    count += amount;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAddAssignmentOnVolatileFieldInPublicConstructor() {
      const string source = @"
class Test {
  private volatile int count;

  public Test(int offset) {
    count += offset;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAddAssignmentOnVolatileFieldInPublicMemberEnclosedByLock() {
      const string source = @"
class Test {
  private readonly object syncObject = new object();
  private volatile int count;

  public void Add(int amount) {
    lock(syncObject) {
      count += amount;
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportUnaryExpressionÎnPublicMember() {
      const string source = @"
class Test {
  private readonly object syncObject = new object();
  private volatile bool disabled;

  public void DoIt() {
    if(!disabled) {
    }
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
