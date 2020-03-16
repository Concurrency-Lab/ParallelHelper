using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Bugs;

namespace ParallelHelper.Test.Analyzer.Bugs {
  [TestClass]
  public class LinqToCollectionOnConcurrentDictionaryAnalyzerTest : AnalyzerTestBase<LinqToCollectionOnConcurrentDictionaryAnalyzer> {
    [TestMethod]
    public void ReportsLinqToListOnConcurrentDictionary() {
      const string source = @"
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;

class Test {
  private readonly ConcurrentDictionary<string, int> queue = new ConcurrentDictionary<string, int>();

  public List<KeyValuePair<string, int>> All() {
    return queue.ToList();
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(9, 12));
    }

    [TestMethod]
    public void ReportsLinqToImmutableArrayOnConcurrentDictionary() {
      const string source = @"
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

class Test {
  private readonly ConcurrentDictionary<int, string> entries = new ConcurrentDictionary<int, string>();

  public ImmutableArray<KeyValuePair<int, string>> All() {
    return entries.ToImmutableArray();
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(9, 12));
    }

    [TestMethod]
    public void DoesNotReportConcurrentDictionaryToArray() {
      const string source = @"
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

class Test {
  private readonly ConcurrentDictionary<int, string> entries = new ConcurrentDictionary<int, string>();

  public KeyValuePair<int, string>[] All() {
    return entries.ToArray();
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportConcurrentStackToArray() {
      const string source = @"
using System.Collections.Concurrent;
using System.Linq;

class Test {
  private readonly ConcurrentStack<string> queue = new ConcurrentStack<string>();

  public string[] All() {
    return queue.ToArray();
  }
}";
      VerifyDiagnostic(source);
    }
    [TestMethod]
    public void DoesNotReportLinqToListOnConcurrentQueue() {
      const string source = @"
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;

class Test {
  private readonly ConcurrentQueue<string> queue = new ConcurrentQueue<string>();

  public List<string> All() {
    return queue.ToList();
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
