using System.Threading;
using System.Threading.Tasks;

namespace IntegrationTests.Cli {
  public class WarehouseAsync {
    private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1);
    private readonly int _max;

    private int _count;

    public WarehouseAsync(int max) {
      _max = max;
    }

    public async Task PutAsync(CancellationToken cancellationToken = default) {
      _mutex.Wait();
      _count++;
      _mutex.Release();
    }

    public async Task<bool> TryTakeAsync(CancellationToken cancellationToken = default) {
      await _mutex.WaitAsync();
      try {
        if(_count == 0) {
          return false;
        }
        --_count;
        return true;
      } finally {
        _mutex.Release();
      }
    }
  }
}
