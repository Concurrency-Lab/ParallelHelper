using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Bugs;

namespace ParallelHelper.Test.Analyzer.Bugs {
  [TestClass]
  public class SinglePulseforVariableWaitConditionsTest : AnalyzerTestBase<SinglePulseforVariableWaitConditionsAnalyzer> {
    [TestMethod]
    public void ReportsPulseSignalingParameterDependantWait() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count;

  public void Take(int amount) {
    lock(syncObject) {
      while(amount > count) {
        Monitor.Wait(syncObject);
      }
      count -= amount;
    }
  }

  public void Put(int amount) {
    lock(syncObject) {
      count += amount;
      Monitor.Pulse(syncObject);
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(19, 7));
    }

    [TestMethod]
    public void DoesNotReportPulseSignalingNotParameterDependantWait() {
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
    public void ReportsPulseAllSignalingParameterDependantWait() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count;

  public void Take(int amount) {
    lock(syncObject) {
      while(amount > count) {
        Monitor.Wait(syncObject);
      }
      count -= amount;
    }
  }

  public void Put(int amount) {
    lock(syncObject) {
      count += amount;
      Monitor.PulseAll(syncObject);
    }
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
