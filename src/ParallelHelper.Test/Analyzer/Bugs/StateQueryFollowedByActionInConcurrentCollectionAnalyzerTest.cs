using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Bugs;

namespace ParallelHelper.Test.Analyzer.Bugs {
  [TestClass]
  public class StateQueryFollowedByActionInConcurrentCollectionAnalyzerTest : AnalyzerTestBase<StateQueryFollowedByActionInConcurrentCollectionAnalyzer> {
    [TestMethod]
    public void ReportsEmptyCheckInWhileConditionFollowedByTake() {
      const string source = @"
using System.Collections.Concurrent;

class Test {
  private readonly ConcurrentQueue<string> entries = new ConcurrentQueue<string>();

  public void DequeueAll() {
    while(!entries.IsEmpty) {
      entries.TryDequeue(out var _);
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(8, 7));
    }

    [TestMethod]
    public void ReportsEmptyCheckInForConditionFollowedByTake() {
      const string source = @"
using System.Collections.Concurrent;

class Test {
  private readonly ConcurrentQueue<string> entries = new ConcurrentQueue<string>();

  public void Dequeue(int count) {
    for(int i = 0; i < count && !entries.IsEmpty; i++) {
      entries.TryDequeue(out var _);
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(8, 7));
    }

    [TestMethod]
    public void ReportsContainsCheckFollowedByIndexerAccess() {
      const string source = @"
using System.Collections.Concurrent;

class Test {
  private readonly ConcurrentDictionary<string, int> entries = new ConcurrentDictionary<string, int>();

  public int GetOrDefault(string key, int defaultValue) {
    if(entries.ContainsKey(key)) {
      return entries[key];
    }
    return defaultValue;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(8, 14));
    }

    [TestMethod]
    public void DoesNotReportUnrelatedContainsCheckFollowedByIndexerAccess() {
      const string source = @"
using System.Collections.Concurrent;

class Test {
  private readonly ConcurrentDictionary<string, int> entries1 = new ConcurrentDictionary<string, int>();
  private readonly ConcurrentDictionary<string, int> entries2 = new ConcurrentDictionary<string, int>();

  public int GetOrDefault(string key, int defaultValue) {
    if(entries1.ContainsKey(key)) {
      return entries2[key];
    }
    return defaultValue;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportUnrelatedCondition() {
      const string source = @"
using System.Collections.Concurrent;

class Test {
  private readonly ConcurrentStack<string> entries = new ConcurrentStack<string>();

  public string GetIfNull(string value) {
    if(value == null) {
      entries.TryPop(out value);
    }
    return value;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportIndexerAccessWithoutCondition() {
      const string source = @"
using System.Collections.Concurrent;

class Test {
  private readonly ConcurrentDictionary<string, int> entries = new ConcurrentDictionary<string, int>();

  public int GetOrDefault(string key, int defaultValue) {
    return entries[key];
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTakeInsideCondition() {
      const string source = @"
using System.Collections.Concurrent;

class Test {
  private readonly ConcurrentQueue<string> entries = new ConcurrentQueue<string>();

  public void DequeueAll() {
    while(entries.TryDequeue(out var _)) {
    }
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
