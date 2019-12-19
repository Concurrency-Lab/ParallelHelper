using System.Threading;

namespace IntegrationTests.Cli {
  public class Warehouse {
    private readonly object _id;
    private readonly int _max;

    private int _count;

    public Warehouse(int id, int max) {
      _id = id;
      _max = max;
    }

    public void Put() {
      lock (_id) {
        while (_count == _max) {
          Monitor.Wait(_id);
        }
        _count++;
        Monitor.Pulse(_id);
      }
    }

    public void Take() {
      lock (_id) {
        if (_count == _max) {
          Monitor.Wait(_id);
        }
        Monitor.PulseAll(_id);
      }
    }
  }
}
