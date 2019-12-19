using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class MonitorSinglePulseSignalsMultipleWaitsAnalyzerTest : AnalyzerTestBase<MonitorSinglePulseSignalsMultipleWaitsAnalyzer> {
    [TestMethod]
    public void ReportsTwoPulseOnTwoWaitsWithSameSyncObject() {
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
      Monitor.Pulse(syncObject);
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(14, 7), new DiagnosticResultLocation(24, 7));
    }

    [TestMethod]
    public void ReportsOnePulseOnTwoWaitsWithSameSyncObject() {
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
      Monitor.Pulse(syncObject);
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(24, 7));
    }

    [TestMethod]
    public void DoesNotReportTwoPulseAllOnTwoWaitsWithSameSyncObject() {
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
    public void DoesNotReportOnePulseOnOneWait() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count;

  public void Take() {
    lock(syncObject) {
      while(count == 0) {
        Monitor.Wait(syncObject);
      }
      count--;
    }
  }

  public void Put() {
    lock(syncObject) {
      count++;
      Monitor.Pulse(syncObject);
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTwoPulseOnTwoWaitsWithDifferentSyncObject() {
      const string source = @"
using System.Threading;

class Test {
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
    }
  }

  public void Put2() {
    lock(syncObject2) {
      count2++;
      Monitor.Pulse(syncObject2);
    }
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
