using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace ParallelHelper.Test.Analyzer {
  /// <summary>
  /// Extension methods for the test compilation builder.
  /// </summary>
  internal static class TestCompilationBuilderExtensions {
    /// <summary>
    /// Verifies the diagnostics against the given compilation builder instance.
    /// </summary>
    /// <param name="compilationBuilder">The compilation builder instance to verify the diagnostics against.</param>
    /// <param name="expectedDiagnostics">The expected diagnostics that should be yielded.</param>
    public static void VerifyDiagnostic(this TestCompilationBuilder compilationBuilder, params DiagnosticResultLocation[] expectedDiagnostics) {
      var diagnosticResults = Analyze(compilationBuilder);
      Assert.AreEqual(expectedDiagnostics.Length, diagnosticResults.Length, "Invalid diagnostics count");
      for(var i = 0; i < expectedDiagnostics.Length; ++i) {
        var result = diagnosticResults[i];
        var expected = expectedDiagnostics[i];
        var span = result.Location.GetLineSpan();
        var actual = new DiagnosticResultLocation(span.Path, span.StartLinePosition.Line, span.StartLinePosition.Character + 1);
        Assert.AreEqual(expected, actual, "Invalid diagnostic");
      }
    }

    private static ImmutableArray<Diagnostic> Analyze(TestCompilationBuilder compilationBuilder) {
      var diagnostics = Task.Run(async () => await GetDiagnosticsAsync(compilationBuilder)).Result;
      foreach(var compilationDiagnostic in diagnostics.Compilation) {
        Console.WriteLine(compilationDiagnostic);
      }
      return diagnostics.Analyzer;
    }

    private static async Task<(ImmutableArray<Diagnostic> Analyzer, ImmutableArray<Diagnostic> Compilation)> GetDiagnosticsAsync(
      TestCompilationBuilder compilationBuilder
    ) {
      var analyzerDiagnostics = await compilationBuilder.BuildWithAnalyzers().GetAnalyzerDiagnosticsAsync();
      var compilationDiagnostics = compilationBuilder.Build().GetDiagnostics();
      return (
        // The result is ordered for better reproducability.
        analyzerDiagnostics.OrderBy(diagnostic => diagnostic.Location.SourceSpan).ToImmutableArray(),
        compilationDiagnostics
          .Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Error)
          .OrderBy(diagnostic => diagnostic.Location.SourceSpan)
          .ToImmutableArray()
      );
    }
  }
}
