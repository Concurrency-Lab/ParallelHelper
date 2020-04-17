using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class LeakedInboundCollectionAnalyzerTest : AnalyzerTestBase<LeakedInboundCollectionAnalyzer> {
    [TestMethod]
    public void ReportsAssignedUnsafeFieldCollectionInsideLockStatementOfPublicMethod() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private ISet<string> entries = new HashSet<string>();

  public void SetContent(ISet<string> entries) {
    lock(syncObject) {
      this.entries = entries;
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(9, 7));
    }
    [TestMethod]
    public void ReportsAssignedUnsafeFieldCollectionInsideLockStatementOfPublicMethodInNestedClassOnlyOnce() {
      const string source = @"
using System.Collections.Generic;

class Test {
  class Nested {
    private readonly object syncObject = new object();
    private ISet<string> entries = new HashSet<string>();

    public void SetContent(ISet<string> entries) {
      lock(syncObject) {
        this.entries = entries;
      }
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(10, 9));
    }

    [TestMethod]
    public void ReportsAssignedUnsafeFieldCollectionInsideLockStatementOfPublicProperty() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private IList<string> entries = new List<string>();

  public IList<string> Content {
    set {
      lock(syncObject) {
        entries = value;
      }
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(10, 9));
    }

    [TestMethod]
    public void DoesNotReportAssignedUnsafeFieldCollectionInsideLockStatementOfPrivateMethod() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private ISet<string> entries = new HashSet<string>();

  private void GetContent(ISet<string> entries) {
    lock(syncObject) {
      this.entries = entries;
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAssignedUnsafeFieldCollectionInsideLockStatementOfPrivateProperty() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private IList<string> entries = new List<string>();

  private IList<string> Content {
    set {
      lock(syncObject) {
        entries = value;
      }
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAssignedUnsafeFieldCollectionInsideLockStatementOfPublicPropertyWithPrivateAccessor() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private IList<string> entries = new List<string>();

  public IList<string> Content {
    private set {
      lock(syncObject) {
        entries = value;
      }
    }
    get {}
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAssignedUnsafeFieldCollectionOutsideLockStatementOfPublicMethod() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private ISet<string> entries = new HashSet<string>();

  public void SetContent(ISet<string> entries) {
    this.entries = entries;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAssignedImmutableFieldCollectionInsideLockStatementOfPublicMethod() {
      const string source = @"
using System.Collections.Generic;
using System.Collections.Immutable;

class Test {
  private readonly object syncObject = new object();
  private ISet<string> entries;

  public void SetContent(ImmutableHashSet<string> entries) {
    lock(syncObject) {
      this.entries = entries;
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAssignedUnsafeFieldCollectionCopyInsideLockStatementOfPublicMethod() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private ISet<string> entries = new HashSet<string>();

  public void SetContent(ISet<string> entries) {
    lock(syncObject) {
      this.entries = new HashSet<string>(entries);
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAssignedUnsafeFieldCollectionAssignedByParenthesizedLambdaInsideLockStatementOfPublicMethod() {
      const string source = @"
using System;
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private ISet<string> entries = new HashSet<string>();

  public void DoIt() {
    lock(syncObject) {
      Action<ISet<string>> lambda = (entries) => {
        this.entries = entries;
      };
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAssignedUnsafeFieldCollectionAssignedBySimpleLambdaInsideLockStatementOfPublicMethod() {
      const string source = @"
using System;
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private ISet<string> entries = new HashSet<string>();

  public void DoIt() {
    lock(syncObject) {
      Action<ISet<string>> lambda = entries => {
        this.entries = entries;
      };
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAssignedUnsafeFieldCollectionAssignedByAnonymousMethodInsideLockStatementOfPublicMethod() {
      const string source = @"
using System;
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private ISet<string> entries = new HashSet<string>();

  public void DoIt() {
    lock(syncObject) {
      Action<ISet<string>> lambda = delegate(ISet<string> entries) {
        this.entries = entries;
      };
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAssignedUnsafeFieldCollectionAssignedByLocalFunctionInsideLockStatementOfPublicMethod() {
      const string source = @"
using System;
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private ISet<string> entries = new HashSet<string>();

  public void DoIt() {
    lock(syncObject) {
      void SetEntries(ISet<string> entries) {
        this.entries = entries;
      }
    }
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
