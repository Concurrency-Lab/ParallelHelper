using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.BestPractices;

namespace ParallelHelper.Test.Analyzer.BestPractices {
  [TestClass]
  public class PreferSlimSynchronizationAnalyzerTest : AnalyzerTestBase<PreferSlimSynchronizationAnalyzer> {
    [TestMethod]
    public void ReportsCreationOfUnnamedSemaphore() {
      const string source = @"
using System.Threading;

class Test {
  private readonly Semaphore _semaphore = new Semaphore(1, 1);
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(4, 43));
    }

    [TestMethod]
    public void DoesNotReportCreationOfNamedSemaphore() {
      const string source = @"
using System.Threading;

class Test {
  private readonly Semaphore _semaphore = new Semaphore(1, 1, ""access"");
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportCreationOfSemaphoreSlim() {
      const string source = @"
using System.Threading;

class Test {
  private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
}";
      VerifyDiagnostic(source);
    }
  }
}
