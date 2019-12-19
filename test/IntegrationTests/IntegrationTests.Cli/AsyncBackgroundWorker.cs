using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationTests.Cli {
  public class AsyncBackgroundWorker {
    private readonly BlockingCollection<Func<CancellationToken, Task>> _jobs = new BlockingCollection<Func<CancellationToken, Task>>();
    private readonly CancellationTokenSource _cancellationToken = new CancellationTokenSource();

    private Task processTask;

    public void Add(Func<CancellationToken, Task> job) {
      _jobs.Add(job, _cancellationToken.Token);
    }

    private async Task ProcessJobsAsync() {
      while(!_cancellationToken.IsCancellationRequested) {
        if(_jobs.TryTake(out var job, Timeout.Infinite, _cancellationToken.Token)) {
          await job(_cancellationToken.Token);
        }
      }
    }

    public void Start() {
      processTask = Task.Run(async () => await ProcessJobsAsync(), _cancellationToken.Token);
    }

    public async Task Stop() {
      _cancellationToken.Cancel();
      await processTask;
    }
  }
}
