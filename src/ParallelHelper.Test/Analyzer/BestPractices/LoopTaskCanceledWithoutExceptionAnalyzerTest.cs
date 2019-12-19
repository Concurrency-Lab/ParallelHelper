using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.BestPractices;

namespace ParallelHelper.Test.Analyzer.BestPractices {
  [TestClass]
  public class LoopTaskCanceledWithoutExceptionAnalyzerTest : AnalyzerTestBase<LoopTaskCanceledWithoutExceptionAnalyzer> {
    [TestMethod]
    public void ReportsWhileLoopInAsyncMethodWithoutException() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  public async Task DoWork(CancellationToken cancellationToken) {
    while(!cancellationToken.IsCancellationRequested) {
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 3));
    }

    [TestMethod]
    public void ReportsForLoopInAsyncMethodWithoutException() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  public async Task DoWork(CancellationToken cancellationToken) {
    for(int i = 0; i < 10 && !cancellationToken.IsCancellationRequested; i++) {
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 3));
    }

    [TestMethod]
    public void ReportsDoWhileLoopInAsyncMethodWithoutException() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  public async Task DoWork(CancellationToken cancellationToken) {
    do {
    } while(!cancellationToken.IsCancellationRequested);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 3));
    }

    [TestMethod]
    public void DoesNotReportWhileLoopInAsyncMethodIfExceptionIsThrownExplicitely() {
      const string source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class Test {
  public async Task DoWork(CancellationToken cancellationToken) {
    while(!cancellationToken.IsCancellationRequested) {
    }
    if(cancellationToken.IsCancellationRequested) {
      throw new OperationCanceledException();
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportWhileLoopInAsyncMethodIfThrowIfCancellationRequestedIsUsed() {
      const string source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class Test {
  public async Task DoWork(CancellationToken cancellationToken) {
    while(!cancellationToken.IsCancellationRequested) {
    }
    cancellationToken.ThrowIfCancellationRequested();
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportWhileLoopInNonAsyncMethodWithoutException() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  public void DoWork(CancellationToken cancellationToken) {
    while(!cancellationToken.IsCancellationRequested) {
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportMethodWithoutLoopInAsyncMethodWithoutException() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  public async Task DoWork(CancellationToken cancellationToken) {
    if(!cancellationToken.IsCancellationRequested) {
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportWhileLoopInAsyncMethodWithoutExceptionAndWithoutIsCancellationRequestedCheck() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  public async Task DoWork(CancellationToken cancellationToken) {
    while(true) {
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportWhileLoopInAsyncMethodWithoutExceptionIfIsCancellationRequestedCheckIsNotPartOfCondition() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  public async Task DoWork(CancellationToken cancellationToken) {
    while(true) {
      if(cancellationToken.IsCancellationRequested) {
        break;
      }
    }
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
