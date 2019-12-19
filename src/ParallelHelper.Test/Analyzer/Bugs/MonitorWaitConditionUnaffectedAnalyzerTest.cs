using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Bugs;

namespace ParallelHelper.Test.Analyzer.Bugs {
  [TestClass]
  public class MonitorWaitConditionUnaffectedAnalyzerTest : AnalyzerTestBase<MonitorWaitConditionUnaffectedAnalyzer> {
    [TestMethod]
    public void ReportsPulseOfWaitWithConstantWaitCondition() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count;

  public void Take() {
    lock(syncObject) {
      while(1 == 1) {
        Monitor.Wait(syncObject);
      }
      --count;
    }
  }

  public void Put() {
    lock(syncObject) {
      Monitor.Pulse(syncObject);
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(18, 7));
    }

    [TestMethod]
    public void ReportsPulseOfWaitWithUnaffectedWaitConditionWhenNoFieldChanged() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count = 0;

  public void Take() {
    lock(syncObject) {
      while(count == 0) {
        Monitor.Wait(syncObject);
      }
      --count;
    }
  }

  public void Put() {
    lock(syncObject) {
      Monitor.Pulse(syncObject);
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(18, 7));
    }

    [TestMethod]
    public void ReportsPulseOfWaitWithUnaffectedWaitConditionWhenOtherFieldChangedPostfixUnaryExpression() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count = 0;
  private int max = 0;

  public void Take() {
    lock(syncObject) {
      while(count == 0) {
        Monitor.Wait(syncObject);
      }
      --count;
    }
  }

  public void Put() {
    lock(syncObject) {
      max++;
      Monitor.PulseAll(syncObject);
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(20, 7));
    }

    [TestMethod]
    public void ReportsPulseOfWaitWithUnaffectedWaitConditionWhenOtherFieldChangedPrefixUnaryExpression() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count = 0;
  private int max = 0;

  public void Take() {
    lock(syncObject) {
      while(count == 0) {
        Monitor.Wait(syncObject);
      }
      --count;
    }
  }

  public void Put() {
    lock(syncObject) {
      --max;
      Monitor.Pulse(syncObject);
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(20, 7));
    }

    [TestMethod]
    public void ReportsPulseOfWaitWithUnaffectedWaitConditionWhenOtherFieldChangedThroughMethodSideEffect() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count = 0;
  private int max = 0;

  public void Take() {
    lock(syncObject) {
      while(count == 0) {
        Monitor.Wait(syncObject);
      }
      --count;
    }
  }

  public void Put() {
    lock(syncObject) {
      Increment(ref max);
      Monitor.PulseAll(syncObject);
    }
  }
   
  private void Increment(ref int number) {
    ++number;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(20, 7));
    }

    [TestMethod]
    public void ReportsPulseOfWaitWithUnaffectedWaitConditionWithThis() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count = 0;
  private int max = 0;

  public void Take() {
    lock(syncObject) {
      while(this.count == 0) {
        Monitor.Wait(syncObject);
      }
      --count;
    }
  }

  public void Put() {
    lock(syncObject) {
      this.max++;
      Monitor.PulseAll(syncObject);
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(20, 7));
    }

    [TestMethod]
    public void ReportsPulseOfAllWaitsWithUnaffectedWaitCondition() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count = 0;
  private int max = 0;

  public void Take() {
    lock(syncObject) {
      while(this.count == 0) {
        Monitor.Wait(syncObject);
      }
      --count;
    }
  }

  public void Take10() {
    lock(syncObject) {
      while(this.count < 10) {
        Monitor.Wait(syncObject);
      }
      this.count -= 10;
    }
  }

  public void Put() {
    lock(syncObject) {
      Monitor.PulseAll(syncObject);
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(28, 7), new DiagnosticResultLocation(28, 7));
    }

    [TestMethod]
    public void DoesNotReportPulseOfWaitWithWaitConditionWhenFieldChangedPostfixUnaryExpression() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count = 0;

  public void Take() {
    lock(syncObject) {
      while(count == 0) {
        Monitor.Wait(syncObject);
      }
      --count;
    }
  }

  public void Put() {
    lock(syncObject) {
      count++;
      Monitor.PulseAll(syncObject);
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportPulseOfWaitWithFieldChangedPrefixUnaryExpression() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count = 0;

  public void Take() {
    lock(syncObject) {
      while(count == 0) {
        Monitor.Wait(syncObject);
      }
      --count;
    }
  }

  public void Put() {
    lock(syncObject) {
      --count;
      Monitor.Pulse(syncObject);
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportPulseOfWaitFieldChangedThroughMethodSideEffectRef() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count = 0;

  public void Take() {
    lock(syncObject) {
      while(count == 0) {
        Monitor.Wait(syncObject);
      }
      --count;
    }
  }

  public void Put() {
    lock(syncObject) {
      Increment(ref count);
      Monitor.PulseAll(syncObject);
    }
  }

  private void Increment(ref int number) {
    ++number;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportPulseOfWaitFieldChangedThroughMethodSideEffectOut() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count = 0;

  public void Take() {
    lock(syncObject) {
      while(count == 0) {
        Monitor.Wait(syncObject);
      }
      --count;
    }
  }

  public void Reset() {
    lock(syncObject) {
      Reset(ref count);
      Monitor.PulseAll(syncObject);
    }
  }

  private void Reset(ref int number) {
    number = 10;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportPulseOfWaitConditionWithThis() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count = 0;

  public void Take() {
    lock(syncObject) {
      while(this.count == 0) {
        Monitor.Wait(syncObject);
      }
      --count;
    }
  }

  public void Put() {
    lock(syncObject) {
      this.count++;
      Monitor.PulseAll(syncObject);
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportPulseOfWaitMonitoringProperty() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count = 0;

  public int Count => count;

  public void Take() {
    lock(syncObject) {
      while(Count == 0) {
        Monitor.Wait(syncObject);
      }
      --count;
    }
  }

  public void Put() {
    lock(syncObject) {
      Monitor.PulseAll(syncObject);
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportPulseOfWaitMonitoringMethod() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count = 0;

  public void Take() {
    lock(syncObject) {
      while(GetCount() == 0) {
        Monitor.Wait(syncObject);
      }
      --count;
    }
  }

  public void Put() {
    lock(syncObject) {
      Monitor.PulseAll(syncObject);
    }
  }

  public int GetCount() => count;
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportPulseOfWaitMonitoringFieldOfForeignObject() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count = 0;
  private Test other = new Test();

  public void Take() {
    lock(syncObject) {
      while(other.count == 0) {
        Monitor.Wait(syncObject);
      }
      --count;
    }
  }

  public void Put() {
    lock(syncObject) {
      Monitor.Pulse(syncObject);
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportPulseOfWaitMonitoringFieldOfForeignObjectWithThis() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count = 0;
  private Test other = new Test();

  public void Take() {
    lock(syncObject) {
      while(this.other.count == 0) {
        Monitor.Wait(syncObject);
      }
      --count;
    }
  }

  public void Put() {
    lock(syncObject) {
      Monitor.Pulse(syncObject);
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportPulseOnDifferentSyncObjectThanWait() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject1 = new object();
  private readonly object syncObject2 = new object();
  private int count = 0;

  public void Take() {
    lock(syncObject1) {
      while(count == 0) {
        Monitor.Wait(syncObject1);
      }
      --count;
    }
  }

  public void Put() {
    lock(syncObject2) {
      Monitor.Pulse(syncObject2);
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportPulseOfWaitIfWaitConditionDependsOnParameter() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count = 0;

  public void Take(int amount) {
    lock(syncObject) {
      while(count < amount) {
        Monitor.Wait(syncObject);
      }
      count -= amount;
    }
  }

  public void Put() {
    lock(syncObject) {
      Monitor.Pulse(syncObject);
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportPulseOfWaitIfWaitConditionDependsOnLocalVariable() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count = 0;

  public void Take(int amount) {
    lock(syncObject) {
      var expected = amount;
      while(count < expected) {
        Monitor.Wait(syncObject);
      }
      count -= expected;
    }
  }

  public void Put() {
    lock(syncObject) {
      Monitor.Pulse(syncObject);
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportPulseOfWaitWhenPulseIsNotEnclosedByLock() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count = 0;

  public void Take() {
    lock(syncObject) {
      while(count == 0) {
        Monitor.Wait(syncObject);
      }
      count--;
    }
  }

  public void Put() {
    Monitor.Pulse(syncObject);
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportPulseOfWaitWhenWaitIsNotEnclosedByLock() {
      const string source = @"
using System.Threading;

class Test {
  private readonly object syncObject = new object();
  private int count = 0;

  public void Take() {
    while(count == 0) {
      Monitor.Wait(syncObject);
    }
    --count;
  }

  public void Put() {
    lock(syncObject) {
      Monitor.Pulse(syncObject);
    }
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
