using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationTests.Cli {
  public class Watchdog {
    private readonly int _intervalMs;
    private readonly Func<CancellationToken, Task<bool>> _healthcheckAsync;
    private readonly Task _monitor;
    private readonly CancellationTokenSource _cancellationToken;

    public Watchdog(int intervalMs, Func<CancellationToken, Task<bool>> healthcheckAsync) {
      _intervalMs = intervalMs;
      _healthcheckAsync = healthcheckAsync;
      _monitor = Task.Run(Monitor);
    }

    private async Task Monitor() {
      while(!_cancellationToken.IsCancellationRequested) {
        if(!await _healthcheckAsync(_cancellationToken.Token)) {
          Console.WriteLine("healthcheck failed");
        }
        await Task.Delay(_intervalMs);
      }
    }

    private async Task NetworkMonitor() {
      while (!_cancellationToken.IsCancellationRequested) {
        if (!await _healthcheckAsync(_cancellationToken.Token)) {
          Console.WriteLine("healthcheck failed");
        }
        await Task.Delay(_intervalMs);
      }
    }

    private async Task CheckConnection() {
      using (var client = new TcpClient()) {
        await client.ConnectAsync("localhost", 80);
        var buffer = Encoding.UTF8.GetBytes("hello");
        await client.GetStream().WriteAsync(buffer, 0, buffer.Length);
      }
    }
  }
}
