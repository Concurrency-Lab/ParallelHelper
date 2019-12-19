using System.Collections.Generic;
using System.Threading.Tasks;

namespace IntegrationTests.Cli {
  public class DataProcessing {
    public int AccumulateParallel(int[] data) {
      int sum = 0;
      Parallel.ForEach(data, d => sum += d);
      return sum;
    }

    public Dictionary<int, int> HistogramParallel(int[] data) {
      var histogram = new Dictionary<int, int>();
      Parallel.ForEach(data, d => {
        lock(histogram) {
          var count = 1;
          if(histogram.TryGetValue(d, out var oldCount)) {
            count += oldCount;
          }
          histogram[d] = count;
        }
      });
      return histogram;
    }
  }
}
