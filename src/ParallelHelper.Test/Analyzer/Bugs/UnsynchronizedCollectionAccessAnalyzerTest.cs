using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Bugs;

namespace ParallelHelper.Test.Analyzer.Bugs {
  [TestClass]
  public class UnsynchronizedCollectionAccessAnalyzerTest : AnalyzerTestBase<UnsynchronizedCollectionAccessAnalyzer> {
    [TestMethod]
    public void ReportsUnsynchronizedMethodReadAccessOnListWithSynchronizedWriteAccess() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly List<string> entries = new List<string>();

  public void Add(string entry) {
    lock(syncObject) {
      entries.Add(entry);
    }
  }

  public bool Contains(string entry) {
    return entries.Contains(entry);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(14, 12));
    }

    [TestMethod]
    public void ReportsUnsynchronizedIndexerReadAccessOnListWithSynchronizedWriteAccess() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly List<string> entries = new List<string>();

  public void Add(string entry) {
    lock(syncObject) {
      entries.Add(entry);
    }
  }

  public string Get(int position) {
    return entries[position];
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(14, 12));
    }

    [TestMethod]
    public void ReportsUnsynchronizedIndexerReadAccessOnListWithSynchronizedIndexerWriteAccess() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly List<string> entries = new List<string>();

  public void Add(int position, string entry) {
    lock(syncObject) {
      entries[position] = entry;
    }
  }

  public string Get(int position) {
    return entries[position];
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(14, 12));
    }

    [TestMethod]
    public void ReportsUnsynchronizedIndexerReadWriteAccessOnListWithSynchronizedIndexerWriteAccess() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly List<int> entries = new List<int>();

  public void Add(int position, int entry) {
    lock(syncObject) {
      entries[position] = entry;
    }
  }

  public int GetAndIncrement(int position) {
    return entries[position]++;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(14, 12));
    }

    [TestMethod]
    public void ReportsUnsynchronizedIndexerReadAccessOnListWithSynchronizedIndexerReadWriteAccess() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly List<int> entries = new List<int>();

  public void Increment(int position) {
    lock(syncObject) {
      ++entries[position];
    }
  }

  public int Get(int position) {
    return entries[position];
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(14, 12));
    }

    [TestMethod]
    public void ReportsUnsynchronizedMethodReadAccessOnDictionaryWithSynchronizedWriteAccess() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly Dictionary<int, string> entries = new Dictionary<int, string>();

  public void Add(int key, string entry) {
    lock(syncObject) {
      entries.Add(key, entry);
    }
  }

  public bool TryGet(int key, out string entry) {
    return entries.TryGetValue(key, out entry);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(14, 12));
    }

    [TestMethod]
    public void ReportsUnsynchronizedMethodReadAccessOnDictionaryWithSynchronizedAddAssignmentWriteAccess() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly Dictionary<int, int> entries = new Dictionary<int, int>();

  public void Add(int key, int count) {
    lock(syncObject) {
      entries[key] += count;
    }
  }

  public bool TryGet(int key, out int entry) {
    return entries.TryGetValue(key, out entry);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(14, 12));
    }

    [TestMethod]
    public void DoesNotReportUnsynchronizedMethodReadAccessOnDictionaryWithSynchronizedUnaryIndexerReadAccess() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly Dictionary<int, bool> entries = new Dictionary<int, bool>();

  public bool GetNegated(int key, string entry) {
    lock(syncObject) {
      return !entries[key];
    }
  }

  public bool TryGet(int key, out bool entry) {
    return entries.TryGetValue(key, out entry);
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportSynchronizedMethodReadAccessOnListWithUnsynchronizedWriteAccess() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly List<string> entries = new List<string>();

  public void Add(string entry) {
    entries.Add(entry);
  }

  public bool Contains(string entry) {
    lock(syncObject) {
      return entries.Contains(entry);
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportSynchronizedIndexerReadAccessOnListWithUnsynchronizedWriteAccess() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly List<string> entries = new List<string>();

  public void Add(string entry) {
    entries.Add(entry);
  }

  public string Get(int position) {
    lock(syncObject) {
      return entries[position];
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportUnsynchronizedMethodReadAccessOnListWithUnsynchronizedWriteAccess() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly List<string> entries = new List<string>();

  public void Add(string entry) {
    entries.Add(entry);
  }

  public bool Contains(string entry) {
    return entries.Contains(entry);
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportUnsynchronizedIndexerReadAccessOnListWithUnsynchronizedWriteAccess() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly List<string> entries = new List<string>();

  public void Add(string entry) {
    entries.Add(entry);
  }

  public string Get(int position) {
    return entries[position];
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportUnsynchronizedMethodReadAccessInPrivateMethodOnListWithSynchronizedWriteAccess() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly List<string> entries = new List<string>();

  public void Add(string entry) {
    lock(syncObject) {
      entries.Add(entry);
    }
  }

  private bool Contains(string entry) {
    return entries.Contains(entry);
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportUnsynchronizedReadAccessInConstructorOnListWithSynchronizedWriteAccess() {
      const string source = @"
using System.Collections.Generic;

class Test {
  private readonly object syncObject = new object();
  private readonly List<string> entries = new List<string>();

  public Test(string entry) {
    entries.Contains(entry);
  }

  public void Add(string entry) {
    lock(syncObject) {
      entries.Add(entry);
    }
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
