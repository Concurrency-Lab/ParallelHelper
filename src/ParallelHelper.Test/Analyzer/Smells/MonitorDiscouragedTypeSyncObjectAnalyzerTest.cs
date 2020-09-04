using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class MonitorDiscouragedTypeSyncObjectAnalyzerTest : AnalyzerTestBase<MonitorDiscouragedTypeSyncObjectAnalyzer> {
    [TestMethod]
    public void ReportsStringLiteral() {
      const string source = @"
class Test {
  public void DoWork() {
    lock("""") {
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(3, 10));
    }

    [TestMethod]
    public void ReportsStringLiteralInsideParentheses() {
      const string source = @"
class Test {
  public void DoWork() {
    lock(("""")) {
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(3, 10));
    }

    [TestMethod]
    public void ReportsStringTypedField() {
      const string source = @"
class Test {
  private readonly string syncObject = """";

  public void DoWork() {
    lock(syncObject) {
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 10));
    }

    [TestMethod]
    public void ReportsFieldInitializedWithString() {
      const string source = @"
class Test {
  private readonly object syncObject = """";

  public void DoWork() {
    lock(syncObject) {
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 10));
    }

    [TestMethod]
    public void ReportsFieldInitializedWithInt() {
      const string source = @"
class Test {
  private readonly object syncObject = 15;

  public void DoWork() {
    lock(syncObject) {
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 10));
    }

    [TestMethod]
    public void DoesNotReportFieldInitializedWithObject() {
      const string source = @"
class Test {
  private readonly object syncObject = new object();

  public void DoWork() {
    lock(syncObject) {
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void ReportsFieldInitializedWithStringInConstructor() {
      const string source = @"
class Test {
  private readonly object syncObject;

  public Test() {
    syncObject = """";
  }

  public void DoWork() {
    lock(syncObject) {
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(9, 10));
    }

    [TestMethod]
    public void ReportsVariableWithStringAssignment() {
      const string source = @"
class Test {
  public void DoWork() {
    object syncObject;
    syncObject = """";
    lock(syncObject) {
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 10));
    }

    [TestMethod]
    public void ReportsVariableWithStringAssignmentAccessedWithParentheses() {
      const string source = @"
class Test {
  public void DoWork() {
    object syncObject;
    syncObject = """";
    lock((syncObject)) {
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 10));
    }

    [TestMethod]
    public void DoesNotReportVariableWithObjectOnlyAssignment() {
      const string source = @"
class Test {
  public void DoWork() {
    object syncObject;
    syncObject = new object();
    lock(syncObject) {
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public async Task DoesNotCrashWhenUsingVariableOfExternallyDeclaredVariableAsSyncObject() {
      const string referenced = @"
public class Foreign {
  public static readonly object syncObject = new object();
}";
      const string source = @"
class Test {
  public void DoWork() {
    lock(Foreign.syncObject) {
    }
  }
}";
      var compilation = CompilationFactory.CreateCompilation(referenced, source);
      var diagnostics = await compilation
        .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new MonitorDiscouragedTypeSyncObjectAnalyzer()))
        .GetAllDiagnosticsAsync();
      Assert.AreEqual(0, diagnostics.Length);
    }
  }
}
