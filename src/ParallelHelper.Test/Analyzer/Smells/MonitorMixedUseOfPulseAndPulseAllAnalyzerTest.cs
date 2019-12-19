using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class MonitorMixedUseOfPulseAndPulseAllAnalyzerTest : AnalyzerTestBase<MonitorMixedUseOfPulseAndPulseAllAnalyzer> {
    [TestMethod]
    public void ReportsCombinationOfPulseAndPulseAllOnSameSyncObject() {
      const string source = @"
using System.Threading;

class Test {
  private const int max = 10;
  private readonly object syncObject = new object();
  private int count;

  public void Take() {
    lock(syncObject) {
      while(count == 0) {
        Monitor.Wait(syncObject);
      }
      count--;
      Monitor.Pulse(syncObject);
    }
  }

  public void Put() {
    lock(syncObject) {
      while(count >= max) {
        Monitor.Wait(syncObject);
      }
      count++;
      Monitor.PulseAll(syncObject);
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(14, 7));
    }

    [TestMethod]
    public void DoesNotReportDoubleUseOfPulseAllOnSameSyncObject() {
      const string source = @"
using System.Threading;

class Test {
  private const int max = 10;
  private readonly object syncObject = new object();
  private int count;

  public void Take() {
    lock(syncObject) {
      while(count == 0) {
        Monitor.Wait(syncObject);
      }
      count--;
      Monitor.PulseAll(syncObject);
    }
  }

  public void Put() {
    lock(syncObject) {
      while(count >= max) {
        Monitor.Wait(syncObject);
      }
      count++;
      Monitor.PulseAll(syncObject);
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportUnrelatedUseOfPulseAndPulseAll() {
      const string source = @"
using System.Threading;

class Test {
  private const int max = 10;
  private readonly object syncObject1 = new object();
  private readonly object syncObject2 = new object();
  private int count1;
  private int count2;

  public void Take1() {
    lock(syncObject1) {
      while(count1 == 0) {
        Monitor.Wait(syncObject1);
      }
      count1--;
    }
  }

  public void Put1() {
    lock(syncObject1) {
      count1++;
      Monitor.Pulse(syncObject1);
    }
  }

  public void Take2() {
    lock(syncObject2) {
      while(count2 == 0) {
        Monitor.Wait(syncObject2);
      }
      count2--;
      Monitor.PulseAll(syncObject2);
    }
  }

  public void Put2() {
    lock(syncObject2) {
      while(count2 >= max) {
        Monitor.Wait(syncObject2);
      }
      count2++;
      Monitor.PulseAll(syncObject2);
    }
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
