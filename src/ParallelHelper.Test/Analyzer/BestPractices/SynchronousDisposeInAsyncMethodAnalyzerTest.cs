using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.BestPractices;

namespace ParallelHelper.Test.Analyzer.BestPractices {
  [TestClass]
  public class SynchronousDisposeInAsyncMethodAnalyzerTest : AnalyzerTestBase<SynchronousDisposeInAsyncMethodAnalyzer> {
    [TestMethod]
    public void ReportsSynchronousDisposeInAsyncMethodOfAsyncDisposableStream() {
      const string source = @"
using System.IO;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync(string fileName) {
    using(var stream = File.OpenRead(fileName)) {
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 5));
    }

    [TestMethod]
    public void ReportsSynchronousDisposeWithUsingDeclarationInAsyncMethodOfAsyncDisposableStream() {
      const string source = @"
using System.IO;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync(string fileName) {
    using var stream = File.OpenRead(fileName);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 5));
    }

    [TestMethod]
    public void DoesNotReportAsynchronousDisposeInAsyncMethodOfAsyncDisposableStream() {
      const string source = @"
using System.IO;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync(string fileName) {
    await using(var stream = File.OpenRead(fileName)) {
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportSynchronousDisposeInNonAsyncMethodOfAsyncDisposableStream() {
      const string source = @"
using System.IO;
using System.Threading.Tasks;

class Test {
  public void DoWork(string fileName) {
    using(var stream = File.OpenRead(fileName)) {
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportSynchronousDisposeInAsyncMethodOfNotAsyncDisposable() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync(string fileName) {
    using(var disposable = new Disposable()) {
    }
  }
}

class Disposable : IDisposable {
  public void Dispose() { }
}";
      VerifyDiagnostic(source);
    }
  }
}
