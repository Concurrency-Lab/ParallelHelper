using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace ParallelHelper.Test.Analyzer {
  /// <summary>
  /// Base class for analyzer implementations.
  /// </summary>
  /// <typeparam name="TAnalyzer">The type of the analyzer under test.</typeparam>
  public class AnalyzerTestBase<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new() {
    /// <summary>
    /// Verifies that the given diagnostics are reported when analyzing the given source.
    /// </summary>
    /// <param name="source">The source to analyze.</param>
    /// <param name="expectedDiagnostics">The expected diagnostics.</param>
    public void VerifyDiagnostic(string source, params DiagnosticResultLocation[] expectedDiagnostics) {
      var compilationBuilder = TestCompilationBuilder.Create()
        .AddSourceTexts(source)
        .AddAnalyzers(new TAnalyzer());
      VerifyDiagnostic(compilationBuilder, expectedDiagnostics);
    }

    /// <summary>
    /// Verifies that the given diagnostics are reported when analyzing the given source.
    /// </summary>
    /// <param name="source">The source to analyze.</param>
    /// <param name="analyzerOptions">The analyzer options (.editorconfig settings) to pass to the analyzer.</param>
    /// <param name="expectedDiagnostics">The expected diagnostics.</param>
    public void VerifyDiagnostic(
      string source,
      ImmutableDictionary<string, string> analyzerOptions,
      params DiagnosticResultLocation[] expectedDiagnostics
    ) {
      var compilationBuilder = TestCompilationBuilder.Create()
        .AddSourceTexts(source)
        .AddAnalyzers(new TAnalyzer())
        .WithAnalyzerOptions(analyzerOptions);
      VerifyDiagnostic(compilationBuilder, expectedDiagnostics);
    }

    private void VerifyDiagnostic(TestCompilationBuilder compilationBuilder, DiagnosticResultLocation[] expectedDiagnostics) {
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

    private ImmutableArray<Diagnostic> Analyze(TestCompilationBuilder compilationBuilder) {
      var diagnostics = Task.Run(async () => await GetDiagnosticsAsync(compilationBuilder)).Result;
      foreach(var compilationDiagnostic in diagnostics.Compilation) {
        Console.WriteLine(compilationDiagnostic);
      }
      return diagnostics.Analyzer;
    }

    private async Task<(ImmutableArray<Diagnostic> Analyzer, ImmutableArray<Diagnostic> Compilation)> GetDiagnosticsAsync(
      TestCompilationBuilder compilationBuilder
    ) {
      var analyzerDiagnostics = await compilationBuilder
        .BuildWithAnalyzers()
        .GetAnalyzerDiagnosticsAsync();
      // The result is ordered for better reproducability.
      return (
        analyzerDiagnostics.OrderBy(diagnostic => diagnostic.Location.SourceSpan).ToImmutableArray(),
        compilationBuilder.Build().GetDiagnostics()
          .Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Error)
          .OrderBy(diagnostic => diagnostic.Location.SourceSpan)
          .ToImmutableArray()
      );
    }
  }
}
