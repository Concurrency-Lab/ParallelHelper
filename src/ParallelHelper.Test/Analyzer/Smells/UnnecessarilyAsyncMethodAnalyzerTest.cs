using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class UnnecessarilyAsyncMethodAnalyzerTest : AnalyzerTestBase<UnnecessarilyAsyncMethodAnalyzer> {
    [TestMethod]
    public void ReportsAwaitAsSingleStatement() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWork() {
    await Task.Run(() => {});
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(4, 3));
    }

    [TestMethod]
    public void ReportsSingleAwaitAsLastStatement() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWork() {
    int x = 0;
    x++;
    await Task.Run(() => {});
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(4, 3));
    }

    [TestMethod]
    public void DoesNotReportsSingleAwaitAsIntermediateStatement() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task DoWork() {
    int x = 0;
    await Task.Run(() => {});
    x++;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void ReportsExpressionBodiedAwait() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task<int> DoWork() => await Task.Run(() => 1);
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(4, 3));
    }

    [TestMethod]
    public void DoesNotReportAwaitNestedInExpressionBody() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task<int> DoWork() => 1 + (await Task.Run(() => 1));
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void ReportsAwaitInsideSingleReturnStatement() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task<int> DoWork(bool predicate) {
    return await Task.Run(() => 1);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(4, 3));
    }

    [TestMethod]
    public void ReportsMultipleAwaitsThatAreReturnOnly() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task<int> DoWork(bool predicate) {
    if(predicate) {
      return await Task.Run(() => 2);
    }
    return await Task.Run(() => 1);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(4, 3));
    }

    [TestMethod]
    public void DoesNotReportMultipleAwaitsIfOneIsNotReturned() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task<int> DoWork(bool predicate) {
    await Task.Run(() => { });
    if(predicate) {
      return await Task.Run(() => 2);
    }
    return await Task.Run(() => 1);
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAwaitInsideUsing() {
      const string source = @"
using System.IO;
using System.Threading.Tasks;

class Test {
  public async Task<string> GetFileContent(string file) {
    using(StreamReader stream = new StreamReader(file)) {
      return await stream.ReadToEndAsync();
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAwaitInsideTryStatement() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task<int> DoWork() {
    try {
      return await Task.Run(() => 1);
    } finally {}
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAwaitInsideFinallyBlock() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  public async Task<int> DoWork() {
    try {
    } finally {
      return await Task.Run(() => 1);
    }
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
