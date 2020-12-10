using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ParallelHelper.Test.Analyzer {
  /// <summary>
  /// Base class for analyzer implementations.
  /// </summary>
  /// <typeparam name="TAnalyzer">The type of the analyzer under test.</typeparam>
  public class AnalyzerTestBase<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new() {
    /// <summary>
    /// Creates a new compilation builder instance with the tested analyzer.
    /// </summary>
    /// <returns>A compilation builder that already contains the tested analyzer.</returns>
    public TestCompilationBuilder CreateAnalyzerCompilationBuilder() {
      return TestCompilationBuilder.Create()
        .AddAnalyzers(new TAnalyzer());
    }

    /// <summary>
    /// Verifies that the given diagnostics are reported when analyzing the given source.
    /// </summary>
    /// <param name="source">The source to analyze.</param>
    /// <param name="expectedDiagnostics">The expected diagnostics.</param>
    public void VerifyDiagnostic(string source, params DiagnosticResultLocation[] expectedDiagnostics) {
      CreateAnalyzerCompilationBuilder()
        .AddSourceTexts(source)
        .VerifyDiagnostic(expectedDiagnostics);
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
      CreateAnalyzerCompilationBuilder()
        .AddSourceTexts(source)
        .WithAnalyzerOptions(analyzerOptions)
        .VerifyDiagnostic(expectedDiagnostics);
    }
  }
}
