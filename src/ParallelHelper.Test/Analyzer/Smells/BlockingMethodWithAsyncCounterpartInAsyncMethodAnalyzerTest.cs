using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class BlockingMethodWithAsyncCounterpartInAsyncMethodAnalyzerTest : AnalyzerTestBase<BlockingMethodWithAsyncCounterpartInAsyncMethodAnalyzer> {
    [TestMethod]
    public void ReportsReadOfTypeWithReadAsyncInAsyncMethod() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    using(var client = new TcpClient())
    using(var reader = new StreamReader(client.GetStream())) {
      var buffer = new char[1024];
      reader.Read(buffer, 0, buffer.Length);
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(10, 7));
    }

    [TestMethod]
    public void ReportsReadOfTypeWithReadAsyncInAsyncParenthesizedLambda() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    Task.Run(async () => {
      using(var client = new TcpClient())
      using(var reader = new StreamReader(client.GetStream())) {
        var buffer = new char[1024];
        reader.Read(buffer, 0, buffer.Length);
      }
    });
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(11, 9));
    }

    [TestMethod]
    public void ReportsReadOfTypeWithReadAsyncInAsyncSimpleLambda() {
      const string source = @"
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    Action<int> test = async _ => {
      using(var client = new TcpClient())
      using(var reader = new StreamReader(client.GetStream())) {
        var buffer = new char[1024];
        reader.Read(buffer, 0, buffer.Length);
      }
    };
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(12, 9));
    }

    [TestMethod]
    public void ReportsReadOfTypeWithReadAsyncInAsyncAnonymousMethod() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    Task.Run(async delegate () {
      using(var client = new TcpClient())
      using(var reader = new StreamReader(client.GetStream())) {
        var buffer = new char[1024];
        reader.Read(buffer, 0, buffer.Length);
      }
    });
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(11, 9));
    }

    [TestMethod]
    public void ReportsAccessToMethodReturningVoidWithAsyncCounterpart() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    DoIt();
  }

  public void DoIt() {
  }

  public Task DoItAsync() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 5));
    }

    [TestMethod]
    public void ReportsAccessToMethodReturningValueWithAsyncCounterpartReturningValueOfSameType() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    var value = GetIt();
  }

  public int GetIt() {
    return 1;
  }

  public Task<int> GetItAsync() {
    return Task.FromResult(1);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 17));
    }

    [TestMethod]
    public void ReportsAccessToMethodReturningValueWithAsyncCounterpartReturningValueIfBothAreGenericArguments() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    var value = GetIt<int>();
  }

  public T GetIt<T>() {
    return default;
  }

  public Task<T> GetItAsync<T>() {
    return Task.FromResult<T>(default);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 17));
    }

    [TestMethod]
    public void ReportsAccessToMethodReturningValueWithAsyncCounterpartReturningValueIfBothAreGenericTypeArguments() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test<T> {
  public async Task DoWorkAsync() {
    var value = GetIt();
  }

  public T GetIt() {
    return default;
  }

  public Task<T> GetItAsync() {
    return Task.FromResult<T>(default);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 17));
    }

    [TestMethod]
    public void ReportsAccessToSemaphoreSlimWait() {
      const string source = @"
using System.Threading;
using System.Threading.Tasks;

class Test {
  private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1);
  private int count;

  public async Task<int> IncrementAndGetAsync() {
    _mutex.Wait();
    try {
      return ++count;
    } finally {
      _mutex.Release();
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(9, 5));
    }

    [TestMethod]
    public void ReportsReadOfTypeWithReadAsyncInAsyncMethodIfExclusionEntryIsInvalid() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    using(var client = new TcpClient())
    using(var reader = new StreamReader(client.GetStream())) {
      var buffer = new char[1024];
      reader.Read(buffer, 0, buffer.Length);
    }
  }
}";
      CreateAnalyzerCompilationBuilder()
        .AddSourceTexts(source)
        .AddAnalyzerOption("dotnet_diagnostic.PH_S019.exclusions", "System.IO.StreamReader;ReadLine,Read,ReadBlock")
        .VerifyDiagnostic(new DiagnosticResultLocation(10, 7));
    }

    [TestMethod]
    public void ReportsAccessToMethodReturningValueWithAsyncCounterpartReturningValueOfDifferentTypeIfMatchReturnTypeIsDisabled() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    var value = GetIt();
  }

  public double GetIt() {
    return 1.0;
  }

  public Task<int> GetItAsync() {
    return Task.FromResult(1);
  }
}";
      CreateAnalyzerCompilationBuilder()
        .AddSourceTexts(source)
        .AddAnalyzerOption("dotnet_diagnostic.PH_S019.returnType", "ignore")
        .VerifyDiagnostic(new DiagnosticResultLocation(7, 17));
    }

    [TestMethod]
    public void ReportsAccessToMethodReturningValueWithAsyncCounterpartReturningNoValueIfMatchReturnTypeIsDisabled() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    var value = GetIt();
  }

  public int GetIt() {
    return 1;
  }

  public Task GetItAsync() {
    return Task.CompletedTask;
  }
}";
      CreateAnalyzerCompilationBuilder()
        .AddSourceTexts(source)
        .AddAnalyzerOption("dotnet_diagnostic.PH_S019.returnType", "ignore")
        .VerifyDiagnostic(new DiagnosticResultLocation(7, 17));
    }

    [TestMethod]
    public void ReportsAccessToMethodWithAsyncCounterPartWithDifferentParameterCount() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    var value = GetIt();
  }

  public int GetIt() {
    return 1;
  }

  public Task<int> GetItAsync(int value) {
    return Task.FromResult(value);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 17));
    }

    [TestMethod]
    public void ReportsAccessToMethodWithAsyncCounterPartWithSameParameterTypes() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    DoIt(1);
  }

  public void DoIt(int value) {
  }

  public Task DoItAsync(int value) {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 5));
    }

    [TestMethod]
    public void DoesNotReportAccessToAsyncCounterPartWhereOnlyTheFirstParameterTypeIsEqualByDefault() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    DoIt(1, 2, 3);
  }

  public void DoIt(int value, int value2, int value3) {
  }

  public Task DoItAsync(int value, string value2, string value3) {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 5));
    }

    [TestMethod]
    public void ReportsAccessToMethodWithAsyncCounterPartWithDifferentParameterCountAndTypesIfParameterTypeMatchCountIsZero() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    DoIt(1);
  }

  public void DoIt(int value) {
  }

  public Task DoItAsync(string value, string second) {
    return Task.CompletedTask;
  }
}";
      CreateAnalyzerCompilationBuilder()
        .AddSourceTexts(source)
        .AddAnalyzerOption("dotnet_diagnostic.PH_S019.parameterTypeMatchCount", "0")
        .VerifyDiagnostic(new DiagnosticResultLocation(7, 5));
    }

    [TestMethod]
    public void ReportsAccessToMethodWithAsyncCounterPartIfCounterPartHasOneMoreParameterButMatchCountIsOne() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    DoIt(1);
  }

  public void DoIt(int value) {
  }

  public Task DoItAsync(int value, string second) {
    return Task.CompletedTask;
  }
}";
      CreateAnalyzerCompilationBuilder()
        .AddSourceTexts(source)
        .AddAnalyzerOption("dotnet_diagnostic.PH_S019.parameterTypeMatchCount", "1")
        .VerifyDiagnostic(new DiagnosticResultLocation(7, 5));
    }

    [TestMethod]
    public void ReportsAccessToMethodWithAsyncCounterPartIfCounterPartHasOneLessParameterButMatchCountIsOne() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    DoIt(1, new object());
  }

  public void DoIt(int value, object second) {
  }

  public Task DoItAsync(int value) {
    return Task.CompletedTask;
  }
}";
      CreateAnalyzerCompilationBuilder()
        .AddSourceTexts(source)
        .AddAnalyzerOption("dotnet_diagnostic.PH_S019.parameterTypeMatchCount", "1")
        .VerifyDiagnostic(new DiagnosticResultLocation(7, 5));
    }

    [TestMethod]
    public void ReportsAccessToMethodWithAsyncCounterPartWithDifferentParameterCountAndTypesIfParameterTypeMatchCountIsIgnore() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    DoIt(1);
  }

  public void DoIt(int value) {
  }

  public Task DoItAsync(string value, string second) {
    return Task.CompletedTask;
  }
}";
      CreateAnalyzerCompilationBuilder()
        .AddSourceTexts(source)
        .AddAnalyzerOption("dotnet_diagnostic.PH_S019.parameterTypeMatchCount", "ignore")
        .VerifyDiagnostic(new DiagnosticResultLocation(7, 5));
    }

    [TestMethod]
    public void ReportsAccessToMethodWithAsyncCounterPartIfCounterPartHasMoreParametersThanTheOriginalWithMatchAllParameters() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    DoIt(1);
  }

  public void DoIt(int value) {
  }

  public Task DoItAsync(int value, int a, int b, int c) {
    return Task.CompletedTask;
  }
}";
      CreateAnalyzerCompilationBuilder()
        .AddSourceTexts(source)
        .AddAnalyzerOption("dotnet_diagnostic.PH_S019.parameterTypeMatchCount", "10")
        .VerifyDiagnostic(new DiagnosticResultLocation(7, 5));
    }

    [TestMethod]
    public void DoesNotReportReadOfTypeWithReadAsyncInNonAsyncMethod() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public void DoWorkAsync() {
    using(var client = new TcpClient())
    using(var reader = new StreamReader(client.GetStream())) {
      var buffer = new char[1024];
      reader.Read(buffer, 0, buffer.Length);
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReadOfTypeWithReadAsyncInNonAsyncParenthesizedLambda() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    Task.Run(() => {
      using(var client = new TcpClient())
      using(var reader = new StreamReader(client.GetStream())) {
        var buffer = new char[1024];
        reader.Read(buffer, 0, buffer.Length);
      }
    });
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReadOfTypeWithReadAsyncInNonAsyncSimpleLambda() {
      const string source = @"
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    Action<int> test = _ => {
      using(var client = new TcpClient())
      using(var reader = new StreamReader(client.GetStream())) {
        var buffer = new char[1024];
        reader.Read(buffer, 0, buffer.Length);
      }
    };
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReadOfTypeWithReadAsyncInNonAsyncAnonymousMethod() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public void DoWork() {
    Task.Run(delegate () {
      using(var client = new TcpClient())
      using(var reader = new StreamReader(client.GetStream())) {
        var buffer = new char[1024];
        reader.Read(buffer, 0, buffer.Length);
      }
    });
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReadAsyncInAsyncMethod() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    using(var client = new TcpClient())
    using(var reader = new StreamReader(client.GetStream())) {
      var buffer = new char[1024];
      await reader.ReadAsync(buffer, 0, buffer.Length);
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAccessToMethodReturningTaskWithAsyncCounterpart() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    await DoIt();
  }

  public Task DoIt() {
    return Task.CompletedTask;
  }

  public Task DoItAsync() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAccessToMethodReturningValueTaskWithAsyncCounterpart() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    await DoIt();
  }

  public Task<int> DoIt() {
    return Task.FromResult(0);
  }

  public Task DoItAsync() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAccessToBlockingMethodWithAsyncCounterPartWhenInSeparateActivationFrame() {
      const string source = @"
using System.IO;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    await Task.Run(() => {
      DoIt();
    });
  }

  public void DoIt() {
  }

  public Task DoItAsync() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReadOfTypeWithReadAsyncInAsyncMethodIfExcludedAsSingleTypeAndMethodEntry() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    using(var client = new TcpClient())
    using(var reader = new StreamReader(client.GetStream())) {
      var buffer = new char[1024];
      reader.Read(buffer, 0, buffer.Length);
    }
  }
}";
      CreateAnalyzerCompilationBuilder()
        .AddSourceTexts(source)
        .AddAnalyzerOption("dotnet_diagnostic.PH_S019.exclusions", "System.IO.StreamReader:Read")
        .VerifyDiagnostic();
    }

    [TestMethod]
    public void DoesNotReportReadOfTypeWithReadAsyncInAsyncMethodIfExcludedAsSingleTypeEntry() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    using(var client = new TcpClient())
    using(var reader = new StreamReader(client.GetStream())) {
      var buffer = new char[1024];
      reader.Read(buffer, 0, buffer.Length);
    }
  }
}";
      CreateAnalyzerCompilationBuilder()
        .AddSourceTexts(source)
        .AddAnalyzerOption("dotnet_diagnostic.PH_S019.exclusions", "System.IO.StreamReader:ReadLine,Read,ReadBlock")
        .VerifyDiagnostic();
    }

    [TestMethod]
    public void DoesNotReportReadOfTypeWithReadAsyncInAsyncMethodIfExcludedAsMultipleTypeEntry() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    using(var client = new TcpClient())
    using(var reader = new StreamReader(client.GetStream())) {
      var buffer = new char[1024];
      reader.Read(buffer, 0, buffer.Length);
    }
  }
}";
      const string exclusions = @"Microsoft.EntityFrameworkCore.DbContext:Add,AddRange
Microsoft.EntityFrameworkCore.DbSet`1:Add,AddRange
System.IO.StreamReader:ReadLine,Read,ReadBlock";
      CreateAnalyzerCompilationBuilder()
        .AddSourceTexts(source)
        .AddAnalyzerOption("dotnet_diagnostic.PH_S019.exclusions", exclusions)
        .VerifyDiagnostic();
    }

    [TestMethod]
    public void DoesNotReportAccessToMethodReturningVoidWithAsyncCounterpartReturningValue() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    DoIt();
  }

  public void DoIt() {
  }

  public Task<int> DoItAsync() {
    return Task.FromResult(1);
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAccessToMethodReturningValueWithAsyncCounterpartReturningValueOfDifferentType() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    var value = GetIt();
  }

  public double GetIt() {
    return 1.0;
  }

  public Task<int> GetItAsync() {
    return Task.FromResult(1);
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAccessToMethodReturningValueWithAsyncCounterpartReturningNoValue() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    var value = GetIt();
  }

  public int GetIt() {
    return 1;
  }

  public Task GetItAsync() {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAccessToMethodWithAsyncCounterpartThatIsNotAwaitable() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    DoIt();
  }

  public void DoIt() {
  }

  public void DoItAsync() {
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAccessToMethodReturningValueWithAsyncCounterpartReturningValueIfOnlyCounterpartIsGeneric() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    var value = GetIt<int>();
  }

  public int GetIt() {
    return 1;
  }

  public Task<T> GetItAsync<T>() {
    return Task.FromResult<T>(default);
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAccessToMethodReturningValueWithAsyncCounterpartReturningValueIfCounterpartIsNotGeneric() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    var value = GetIt<int>();
  }

  public T GetIt<T>() {
    return default;
  }

  public Task<string> GetItAsync() {
    return Task.FromResult<string>(default);
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAccessToAsyncCounterPartWhereTheFirstParameterTypeDiffersByDefault() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    DoIt(1);
  }

  public void DoIt(int value) {
  }

  public Task DoItAsync(string value) {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAccessToMethodWithAsyncCounterPartWhereTheSingleParameterTypesDifferAndMatchOneParameterIsEnabled() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    DoIt(1);
  }

  public void DoIt(int value) {
  }

  public Task DoItAsync(string value) {
    return Task.CompletedTask;
  }
}";
      CreateAnalyzerCompilationBuilder()
        .AddSourceTexts(source)
        .AddAnalyzerOption("dotnet_diagnostic.PH_S019.parameterTypeMatchCount", "1")
        .VerifyDiagnostic();
    }

    [TestMethod]
    public void DoesNotReportAccessToMethodWithAsyncCounterPartWhereTheTwoParameterTypesDifferAndMatchTwoParameterIsEnabled() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    DoIt(1, 2);
  }

  public void DoIt(int value, int value2) {
  }

  public Task DoItAsync(int value, string second) {
    return Task.CompletedTask;
  }
}";
      CreateAnalyzerCompilationBuilder()
        .AddSourceTexts(source)
        .AddAnalyzerOption("dotnet_diagnostic.PH_S019.parameterTypeMatchCount", "2")
        .VerifyDiagnostic();
    }

    [TestMethod]
    public void DoesNotReportAccessToMethodWithAsyncCounterPartWhereTheCounterPartDoesNotHaveTheThirdParameterButMatchThreeIsEnabled() {
      const string source = @"
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync() {
    DoIt(1, 2, 3);
  }

  public void DoIt(int value, int value2, int value3) {
  }

  public Task DoItAsync(int value, int value2) {
    return Task.CompletedTask;
  }
}";
      CreateAnalyzerCompilationBuilder()
        .AddSourceTexts(source)
        .AddAnalyzerOption("dotnet_diagnostic.PH_S019.parameterTypeMatchCount", "3")
        .VerifyDiagnostic();
    }
  }
}
