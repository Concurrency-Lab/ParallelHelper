using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.BestPractices;
using System.Collections.Immutable;

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
    public void ReportsCreationOfNamedSemaphoreIfEnabled() {
      const string source = @"
using System.Threading;

class Test {
  private readonly Semaphore _semaphore = new Semaphore(1, 1, ""access"");
}";
      var options = ImmutableDictionary.Create<string, string>()
        .Add("dotnet_diagnostic.PH_P012.named", "report");
      VerifyDiagnostic(source, options, new DiagnosticResultLocation(4, 43));
    }

    [TestMethod]
    public void DoesNotReportCreationOfNamedSemaphoreByDefault() {
      const string source = @"
using System.Threading;

class Test {
  private readonly Semaphore _semaphore = new Semaphore(1, 1, ""access"");
}";
      var options = ImmutableDictionary.Create<string, string>()
        .Add("dotnet_diagnostic.PH_P012.named", "ignore");
      VerifyDiagnostic(source, options);
    }

    [TestMethod]
    public void DoesNotReportCreationOfNamedSemaphoreIfDisabled() {
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

    [TestMethod]
    public void DoesNotReportCreationOfUnresolvableType() {
      const string source = @"
class Test {
  private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
}";
      VerifyDiagnostic(source);
    }
  }
}
