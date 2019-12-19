using System;
using System.Collections.Concurrent;
using System.Threading;

namespace IntegrationTests.Cli {
  public class BackgroundWorker {
    private readonly Thread _thread;
    private readonly ConcurrentQueue<Action> _jobs = new ConcurrentQueue<Action>();
    private readonly CancellationTokenSource _cancellationToken = new CancellationTokenSource();

    public BackgroundWorker() {
      _thread = new Thread(ProcessJobs);
    }

    public void Add(Action job) {
      _jobs.Enqueue(job);
    }

    private void ProcessJobs() {
      while(!_cancellationToken.IsCancellationRequested) {
        if(!_jobs.TryDequeue(out var job)) {
          Thread.Sleep(100);
          continue;
        }
        job();
      }
    }

    public void Start() {
      _thread.Start();
    }

    public void Stop() {
      _cancellationToken.Cancel();
      _thread.Abort();
    }
  }
}
