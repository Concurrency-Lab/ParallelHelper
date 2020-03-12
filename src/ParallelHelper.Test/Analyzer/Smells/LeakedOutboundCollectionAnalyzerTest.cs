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

  private ISet<string> GetContent() {
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

  private ISet<string> GetContent() {
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

  private ISet<string> GetContent() {
    lock(syncObject) {
      return new HashSet<string>(entries);
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReturnedUnsafeFieldCollectionReturnedByLambdaInsideLockStatementOfPublicMethod() {
      const string source = @"
using System;
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly HashSet<string> entries = new HashSet<string>();

  private void DoIt() {
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
    public void DoesNotReportReturnedUnsafeFieldCollectionReturnedByAnonymousMethodInsideLockStatementOfPublicMethod() {
      const string source = @"
using System;
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly HashSet<string> entries = new HashSet<string>();

  private void DoIt() {
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

  private void DoIt() {
    lock(syncObject) {
      ISet<string> GetEntries() {
        return entries;
      }
    }
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
