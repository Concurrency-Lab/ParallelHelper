using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Bugs;

namespace ParallelHelper.Test.Analyzer.Bugs {
  [TestClass]
  public class TaskCreationOnDisposedValueAnalyzerTest : AnalyzerTestBase<TaskCreationOnDisposedValueAnalyzer> {
    [TestMethod]
    public void ReportsReturnWithTaskThatIsCreatedByTheInvocationOfMemberOfObjectDisposedByEnclosingUsingDeclaration() {
      const string source = @"
using System.Net;
using System.Threading.Tasks;

class Test {
  public Task<string> GetEventsAsync() {
    using(var client = new WebClient()) {
      return client.DownloadStringTaskAsync(""https://api.github.com/events"");
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 7));
    }

    [TestMethod]
    public void ReportsReturnWithTaskThatIsCreatedByTheInvocationOfMemberOfObjectDisposedByEnclosingUsingIdentifier() {
      const string source = @"
using System.Net;
using System.Threading.Tasks;

class Test {
  public Task<string> GetEventsAsync() {
    var client = new WebClient();
    using(client) {
      return client.DownloadStringTaskAsync(""https://api.github.com/events"");
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(8, 7));
    }

    [TestMethod]
    public void ReportsReturnWithTaskThatIsCreatedByTheInvocationOfMemberOfObjectDisposedByEnclosingUsingAssignment() {
      const string source = @"
using System.Net;
using System.Threading.Tasks;

class Test {
  public Task<string> GetEventsAsync() {
    WebClient client;
    using(client = new WebClient()) {
      return client.DownloadStringTaskAsync(""https://api.github.com/events"");
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(8, 7));
    }

    [TestMethod]
    public void ReportsReturnWithTaskThatIsCreatedByTheInvocationOfNestedMemberOfObjectDisposedByEnclosingUsingDeclaration() {
      const string source = @"
using System.Net;
using System.Threading.Tasks;

class Test {
  public Task<string> GetEventsAsync() {
    using(var nested = new NestedWebClient()) {
      return nested.Client.DownloadStringTaskAsync(""https://api.github.com/events"");
    }
  }
}

class NestedWebClient : IDisposable {
  public WebClient Client { get; } = new WebClient();

  public void Dispose() {
    Client.Dispose();
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(7, 7));
    }

    [TestMethod]
    public void DoesNotReportReturnOfAwaitedTaskThatIsCreatedByTheInvocationOfMemberOfObjectDisposedByEnclosingUsing() {
      const string source = @"
using System.Net;
using System.Threading.Tasks;

class Test {
  public async Task<string> GetEventsAsync() {
    using(var client = new WebClient()) {
      return await client.DownloadStringTaskAsync(""https://api.github.com/events"");
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReturnTaskThatIsCreatedByTheInvocationOfMemberOfNotDisposedObject() {
      const string source = @"
using System.Net;
using System.Threading.Tasks;

class Test {
  public async Task<string> GetEventsAsync() {
    var client = new WebClient();
    return await client.DownloadStringTaskAsync(""https://api.github.com/events"");
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
