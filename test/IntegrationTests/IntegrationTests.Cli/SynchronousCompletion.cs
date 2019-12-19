using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationTests.Cli {
  class SynchronousCompletion {
    public async Task DoItAsync() {
      await Task.FromResult(0);
    }

    public Task DoOtherAsync() {
      return Task.FromResult(0);
    }
    public async Task<int> GetItAsync() {
      return await Task.FromResult(0);
    }

    public Task<int> GetOtherAsync() {
      return Task.FromResult(0);
    }
  }
}
