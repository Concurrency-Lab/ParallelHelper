using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Bugs;

namespace ParallelHelper.Test.Analyzer.Bugs {
  [TestClass]
  public class LinqToCollectionOnConcurrentCollectionAnalyzerTest : AnalyzerTestBase<LinqToCollectionOnConcurrentCollectionAnalyzer> {
    [TestMethod]
    public void ReportsLinqToListOnConcurrentQueue() {
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
      VerifyDiagnostic(source, new DiagnosticResultLocation(9, 12));
    }

    // TODO The immutable types cannot be resolved and adding them to the roslyn project does not help.
//    [TestMethod]
//    public void ReportsLinqToImmutableArrayOnConcurrentDictionary() {
//      const string source = @"
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Collections.Immutable;

//class Test {
//  private readonly ConcurrentDictionary<int, string> entries = new ConcurrentDictionary<int, string>();

//  public ImmutableArray<KeyValuePair<int, string>> All() {
//    return entries.ToImmutableArray();
//  }
//}";
//      VerifyDiagnostic(source, new DiagnosticResultLocation(9, 12));
//    }

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
  }
}
