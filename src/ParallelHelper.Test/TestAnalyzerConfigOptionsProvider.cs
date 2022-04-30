using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace ParallelHelper.Test {
  /// <summary>
  /// Implementation of <see cref="AnalyzerConfigOptionsProvider"/> to provide analyzer settings
  /// during test executions.
  /// </summary>
  internal class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider {
    private readonly AnalyzerConfigOptions _options;

    public TestAnalyzerConfigOptionsProvider(ImmutableDictionary<string, string> options) {
      _options = new TestAnalyzerConfigOptions(options);
    }

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) {
      return _options;
    }

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) {
      throw new System.NotImplementedException();
    }

    private class TestAnalyzerConfigOptions : AnalyzerConfigOptions {
      private readonly ImmutableDictionary<string, string> _options;

      public TestAnalyzerConfigOptions(ImmutableDictionary<string, string> options) {
        _options = options.WithComparers(KeyComparer);
      }

      public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) {
        return _options.TryGetValue(key, out value);
      }
    }
  }
}
