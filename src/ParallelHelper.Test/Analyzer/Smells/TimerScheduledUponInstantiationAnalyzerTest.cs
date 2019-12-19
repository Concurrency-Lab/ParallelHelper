using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class TimerScheduledUponInstantiationAnalyzerTest : AnalyzerTestBase<TimerScheduledUponInstantiationAnalyzer> {
    [TestMethod]
    public void DoesNotReportNonTimerTypes() {
      const string source = @"
using System;

class Timer {
  public Timer(Action<object> callback, object arg, int dueTime, int period) {}
}

class Test {
  public void Run() {
    var timer = new Timer(Elapsed, null, 10, 10);
  }

  private void Elapsed(object arg) {}
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTimerWithUnspecifiedDueTime() {
      const string source = @"
using System.Threading;

class Test {
  public void Run() {
    var timer = new Timer(Elapsed);
  }

  private void Elapsed(object arg) {}
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTimerWithDueTimeMinusOne() {
      const string source = @"
using System.Threading;

class Test {
  public void Run() {
    var timer = new Timer(Elapsed, null, -1, -1);
  }

  private void Elapsed(object arg) {}
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTimerWithDueTimeMinusOneLong() {
      const string source = @"
using System.Threading;

class Test {
  public void Run() {
    var timer = new Timer(Elapsed, null, -1L, -1);
  }

  private void Elapsed(object arg) {}
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTimerWithDueTimeInfinite() {
      const string source = @"
using System.Threading;

class Test {
  public void Run() {
    var timer = new Timer(Elapsed, null, Timeout.Infinite, Timeout.Infinite);
  }

  private void Elapsed(object arg) {}
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void ReportsTimerWithDueTimeZero() {
      const string source = @"
using System.Threading;

class Test {
  public void Run() {
    var timer = new Timer(Elapsed, null, 0, -1);
  }

  private void Elapsed(object arg) {}
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 17));
    }

    [TestMethod]
    public void ReportsTimerWithDueTimeAboveZero() {
      const string source = @"
using System.Threading;

class Test {
  public void Run() {
    var timer = new Timer(Elapsed, null, 230, -1);
  }

  private void Elapsed(object arg) {}
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 17));
    }

    [TestMethod]
    public void ReportsTimerWithNonInfiniteDueTimeFromLocalVariable() {
      const string source = @"
using System.Threading;

class Test {
  public void Run() {
    var timeout = 20;
    var timer = new Timer(Elapsed, null, timeout, -1);
  }

  private void Elapsed(object arg) {}
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 17));
    }

    [TestMethod]
    public void ReportsTimerWithNonInfiniteDueTimeFromField() {
      const string source = @"
using System.Threading;

class Test {
  private const int Timeout = 10;

  public void Run() {
    var timer = new Timer(Elapsed, null, Timeout, -1);
  }

  private void Elapsed(object arg) {}
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 17));
    }
  }
}
