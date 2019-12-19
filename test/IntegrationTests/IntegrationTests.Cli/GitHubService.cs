using System.Net;
using System.Threading.Tasks;

namespace IntegrationTests.Cli {
  public class GitHubService {
    private readonly WebClient _client = new WebClient();

    public async Task<string> GetPublicGistsAsync() {
      return await _client.DownloadStringTaskAsync("https://api.github.com/gists/public");
    }

    public Task<string> GetEventsAsync() {
      using(var client = new WebClient()) {
        return client.DownloadStringTaskAsync("https://api.github.com/events");
      }
    }
  }
}
