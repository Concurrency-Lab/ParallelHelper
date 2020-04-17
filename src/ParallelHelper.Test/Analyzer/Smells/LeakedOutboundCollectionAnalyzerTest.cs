using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class LeakedOutboundCollectionAnalyzerTest : AnalyzerTestBase<LeakedOutboundCollectionAnalyzer> {
    [TestMethod]
    public void ReportsReturnedUnsafeFieldCollectionInsideLockStatementOfPublicMethod() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly HashSet<string> entries = new HashSet<string>();

  public ISet<string> GetContent() {
    lock(syncObject) {
      return entries;
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(9, 14));
    }

    [TestMethod]
    public void ReportsRefParameterAssignedUnsafeFieldCollectionInsideLockStatementOfPublicMethod() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly HashSet<string> entries = new HashSet<string>();

  public void GetContent(ref ISet<string> entries) {
    lock(syncObject) {
      entries = this.entries;
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(9, 17));
    }

    [TestMethod]
    public void ReportsOutParameterAssignedUnsafeFieldCollectionInsideLockStatementOfPublicMethod() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly HashSet<string> entries = new HashSet<string>();

  public void GetContent(out ISet<string> entries) {
    lock(syncObject) {
      entries = this.entries;
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(9, 17));
    }

    [TestMethod]
    public void ReportsReturnedUnsafeFieldCollectionInsideLockStatementOfPublicMethodInNestedClassOnlyOnce() {
      const string source = @"
using System.Collections.Generic;

class Test {
  class Nested {
    private readonly object syncObject = new object();
    private readonly HashSet<string> entries = new HashSet<string>();

    public ISet<string> GetContent() {
      lock(syncObject) {
        return entries;
      }
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(10, 16));
    }

    [TestMethod]
    public void ReportsReturnedUnsafeFieldCollectionInsideLockStatementOfPublicProperty() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly List<string> entries = new List<string>();

  public IList<string> Content {
    get {
      lock(syncObject) {
        return entries;
      }
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(10, 16));
    }

    [TestMethod]
    public void DoesNotReportEmptyReturnInsideLockStatementOfPublicMethod() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly HashSet<string> entries = new HashSet<string>();

  public void DoIt() {
    lock(syncObject) {
      return;
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReturnedUnsafeFieldCollectionInsideLockStatementOfPrivateMethod() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly HashSet<string> entries = new HashSet<string>();

  private ISet<string> GetContent() {
    lock(syncObject) {
      return entries;
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReturnedUnsafeFieldCollectionInsideLockStatementOfPrivateProperty() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly List<string> entries = new List<string>();

  private IList<string> Content {
    get {
      lock(syncObject) {
        return entries;
      }
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReturnedUnsafeFieldCollectionInsideLockStatementOfPublicPropertyWithPrivateAccessor() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly List<string> entries = new List<string>();

  public IList<string> Content {
    private get {
      lock(syncObject) {
        return entries;
      }
    }
    set {}
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReturnedUnsafeFieldCollectionOutsideLockStatementOfPublicMethod() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly HashSet<string> entries = new HashSet<string>();

  public ISet<string> GetContent() {
    return entries;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReturnedImmutableFieldCollectionInsideLockStatementOfPublicMethod() {
      const string source = @"
using System.Collections.Generic;
using System.Collections.Immutable;

class Test {
  private readonly object syncObject = new object();
  private readonly ImmutableHashSet<string> entries = ImmutableHashSet.Create<string>();

  public ISet<string> GetContent() {
    lock(syncObject) {
      return entries;
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReturnedUnsafeFieldCollectionCopyInsideLockStatementOfPublicMethod() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly HashSet<string> entries = new HashSet<string>();

  public ISet<string> GetContent() {
    lock(syncObject) {
      return new HashSet<string>(entries);
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReturnedUnsafeFieldCollectionReturnedByParenthesizedLambdaInsideLockStatementOfPublicMethod() {
      const string source = @"
using System;
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly HashSet<string> entries = new HashSet<string>();

  public void DoIt() {
    lock(syncObject) {
      Func<ISet<string>> lambda = () => {
        return entries;
      };
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReturnedUnsafeFieldCollectionReturnedBySimpleLambdaInsideLockStatementOfPublicMethod() {
      const string source = @"
using System;
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly HashSet<string> entries = new HashSet<string>();

  public void DoIt() {
    lock(syncObject) {
      Func<int, ISet<string>> lambda = x => {
        return entries;
      };
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReturnedUnsafeFieldCollectionReturnedByAnonymousMethodInsideLockStatementOfPublicMethod() {
      const string source = @"
using System;
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly HashSet<string> entries = new HashSet<string>();

  public void DoIt() {
    lock(syncObject) {
      Func<ISet<string>> lambda = delegate {
        return entries;
      };
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReturnedUnsafeFieldCollectionReturnedByLocalFunctionInsideLockStatementOfPublicMethod() {
      const string source = @"
using System;
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly HashSet<string> entries = new HashSet<string>();

  public void DoIt() {
    lock(syncObject) {
      ISet<string> GetEntries() {
        return entries;
      }
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportParameterAssignedUnsafeFieldCollectionInsideLockStatementOfPublicMethod() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly HashSet<string> entries = new HashSet<string>();

  public void GetContent(ISet<string> entries) {
    lock(syncObject) {
      entries = this.entries;
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotRefReportParameterAssignedImmutableFieldCollectionInsideLockStatementOfPublicMethod() {
      const string source = @"
using System.Collections.Generic;
using System.Collections.Immutable;

class Test {
  private readonly object syncObject = new object();
  private readonly ImmutableHashSet<string> entries = ImmutableHashSet.Create<string>();

  public void GetContent(ISet<string> entries) {
    lock(syncObject) {
      entries = this.entries;
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportRefParameterAssignedNonCollectionFieldInsideLockStatementOfPublicMethod() {
      const string source = @"
using System.Collections.Generic;
using System.Collections.Immutable;

class Test {
  private readonly object syncObject = new object();
  private readonly int state = 1;

  public void GetContent(ref int state) {
    lock(syncObject) {
      state = this.state;
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportRefParameterAssignedCopyOfUnsafeFieldCollectionInsideLockStatementOfPublicMethod() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly HashSet<string> entries = new HashSet<string>();

  public void GetContent(ref ISet<string> entries) {
    lock(syncObject) {
      entries = new HashSet<string>(this.entries);
    }
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
