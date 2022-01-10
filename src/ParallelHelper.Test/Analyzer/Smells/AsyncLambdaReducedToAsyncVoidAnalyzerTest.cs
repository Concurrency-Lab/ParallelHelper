using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class AsyncLambdaReducedToAsyncVoidAnalyzerTest : AnalyzerTestBase<AsyncLambdaReducedToAsyncVoidAnalyzer> {
    [TestMethod]
    public void ReportsAsyncParenthesizedLambdaPassedToMethodAcceptingAction() {
      const string source = @"
using System;
using System.IO;
using System.Net;

interface IWorkQueue {
  void ScheduleWork(Action job);
}

class FileDownloader {
  private readonly WebClient webClient;
  private readonly IWorkQueue queue;

  public void DownloadFile(Uri address, StreamWriter output) {
    queue.ScheduleWork(
      async () => {
        var body = await webClient.DownloadStringTaskAsync(address);
        await output.WriteAsync(body);
      }
    );
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(15, 7));
    }

    [TestMethod]
    public void ReportsAsyncSimpleLambdaPassedToMethodAcceptingAction() {
      const string source = @"
using System;
using System.IO;
using System.Net;

interface IWorkQueue {
  void ScheduleWork(Uri address, Action<Uri> job);
}

class FileDownloader {
  private readonly WebClient webClient;
  private readonly IWorkQueue queue;

  public void DownloadFile(Uri address, StreamWriter output) {
    queue.ScheduleWork(
      address,
      async a => {
        var body = await webClient.DownloadStringTaskAsync(a);
        await output.WriteAsync(body);
      }
    );
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(16, 7));
    }

    [TestMethod]
    public void ReportsAsyncLambdaAssignedToField() {
      const string source = @"
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

class Test {
  private Action<Uri> OnDownloadRequested;

  public void SetDownloadClient(WebClient webClient) {
     OnDownloadRequested = async address => {
      await webClient.DownloadStringTaskAsync(address);
    };
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(10, 28));
    }

    [TestMethod]
    public void DoesNotReportAsyncParenthesizedLambdaPassedToMethodAcceptingFuncReturningTask() {
      const string source = @"
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

interface IWorkQueue {
  void ScheduleWork(Func<Task> job);
}

class FileDownloader {
  private readonly WebClient webClient;
  private readonly IWorkQueue queue;

  public void DownloadFile(Uri address, StreamWriter output) {
    queue.ScheduleWork(
      async () => {
        var body = await webClient.DownloadStringTaskAsync(address);
        await output.WriteAsync(body);
      }
    );
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAsyncParenthesizedLambdaPassedToMethodAcceptingFuncReturningTaskAndActionOverloads() {
      const string source = @"
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

interface IWorkQueue {
  void ScheduleWork(Action job);
  void ScheduleWork(Func<Task> job);
}

class FileDownloader {
  private readonly WebClient webClient;
  private readonly IWorkQueue queue;

  public void DownloadFile(Uri address, StreamWriter output) {
    queue.ScheduleWork(
      async () => {
        var body = await webClient.DownloadStringTaskAsync(address);
        await output.WriteAsync(body);
      }
    );
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAsyncLambdaAssignedToEvent() {
      const string source = @"
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

class Test {
  public event Action<Uri> OnDownloadRequested;

  public void AddDownloadClient(WebClient webClient) {
    OnDownloadRequested += async address => {
      await webClient.DownloadStringTaskAsync(address);
    };
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
