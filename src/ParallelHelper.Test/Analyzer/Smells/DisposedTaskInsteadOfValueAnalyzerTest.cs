using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class DisposedTaskInsteadOfValueAnalyzerTest : AnalyzerTestBase<DisposedTaskInsteadOfValueAnalyzer> {
    [TestMethod]
    public void ReportsUsingExpressionOfTaskWithDisposableInterfaceValue() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    using(CreateAsync()) {
    }
  }
  
  private Task<IDisposable> CreateAsync() {
    return Task.FromResult<IDisposable>(new SomeDisposable());
  }
}

class SomeDisposable : IDisposable {
  public void Dispose() {}
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 11));
    }

    [TestMethod]
    public void ReportsUsingExpressionOfTaskWithDisposableImplementationValue() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    using(CreateAsync()) {
    }
  }
  
  private Task<SomeDisposable> CreateAsync() {
    return Task.FromResult(new SomeDisposable());
  }
}

class SomeDisposable : IDisposable {
  public void Dispose() {}
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 11));
    }

    [TestMethod]
    public void ReportsUsingExpressionOfTaskWithImplicitDisposableImplementationValue() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    using(CreateAsync()) {
    }
  }
  
  private Task<SubType> CreateAsync() {
    return Task.FromResult(new SubType());
  }
}

class SubType : SomeDisposable {
}

class SomeDisposable : IDisposable {
  public void Dispose() {}
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 11));
    }

    [TestMethod]
    public void DoesNotReportUsingExpressionOfTaskWithoutValue() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    using(CreateAsync()) {
    }
  }
  
  private Task CreateAsync() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportUsingExpressionOfTaskWithoutDisposableValue() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    using(CreateAsync()) {
    }
  }
  
  private Task<int> CreateAsync() {
    return Task.FromResult(1);
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportUsingStatementOfTaskWithDisposableInterfaceValue() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    using(var task = CreateAsync()) {
    }
  }
  
  private Task<IDisposable> CreateAsync() {
    return Task.FromResult<IDisposable>(new SomeDisposable());
  }
}

class SomeDisposable : IDisposable {
  public void Dispose() {}
}";
      VerifyDiagnostic(source);
    }
  }
}
