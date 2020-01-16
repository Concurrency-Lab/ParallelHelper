using System;
using System.Collections.Generic;
using System.IO;

namespace IntegrationTests.Cli {
  public class FileReader {
    public async IAsyncEnumerable<byte[]> GetFileContents(string fileName, int chunkSize) {
      using var input = File.OpenRead(fileName);
      int len;
      var buffer = new byte[chunkSize];
      while((len = await input.ReadAsync(buffer)) != -1) {
        var output = new byte[len];
        Array.Copy(buffer, 0, output, 0, len);
        yield return buffer;
      }
    }
  }
}
