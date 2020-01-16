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
    public void ReportsSynchronousDisposeInAsyncMethodOfImplicitelyAsyncDisposableInstance() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync(string fileName) {
    using(var resource = new Resource()) {
    }
  }

  class Resource : IDisposable {
    public void Dispose() {}
    public Task DisposeAsync() {
      return Task.CompletedTask;
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 5));
    }

    [TestMethod]
    public void ReportsSynchronousDisposeInAsyncMethodOfImplicitelyAsyncDisposableInstanceThroughBaseType() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync(string fileName) {
    using(var resource = new Resource()) {
    }
  }

  class Resource : ResourceBase {
  }

  class ResourceBase {
    public void Dispose() {}
    public Task DisposeAsync() {
      return Task.CompletedTask;
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 5));
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
