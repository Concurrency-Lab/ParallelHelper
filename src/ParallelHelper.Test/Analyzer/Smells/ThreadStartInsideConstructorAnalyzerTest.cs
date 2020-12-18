using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class ThreadStartInsideConstructorAnalyzerTest : AnalyzerTestBase<ThreadStartInsideConstructorAnalyzer> {
    [TestMethod]
    public void ReportsThreadStartInConstructor() {
      const string source = @"
using System.Threading;

class Test {
  public Test() {
    var thread = new Thread(() => {});
    thread.Start();
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 5));
    }

    [TestMethod]
    public void ReportsTaskRunInConstructor() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Test() {
    Task.Run(() => {});
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 5));
    }

    [TestMethod]
    public void ReportsTaskFactoryStartNewInConstructor() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public Test() {
    Task.Factory.StartNew(() => {});
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 5));
    }

    [TestMethod]
    public void DoesNotReportThreadCreationInConstructor() {
      const string source = @"
using System.Threading;

class Test {
  private readonly Thread thread;

  public Test() {
    thread = new Thread(() => {});
  }

  public void Start() {
    thread.Start();
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportTaskRunInDifferentActivationFrameInConstructor() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public Test() {
    Func<Task> start = () => Task.Run(() => {});
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
