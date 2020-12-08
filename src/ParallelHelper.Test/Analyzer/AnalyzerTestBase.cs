using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelHelper.Test.Analyzer {
  /// <summary>
  /// Base class for analyzer implementations.
  /// </summary>
  /// <typeparam name="TAnalyzer">The type of the analyzer under test.</typeparam>
  public class AnalyzerTestBase<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new() {
    private ImmutableArray<Diagnostic> Analyze(string[] sources, ImmutableDictionary<string, string> analyzerOptions) {
      var compilation = CompilationFactory.CreateCompilation(sources);
      var diagnostics = Task.Run(async () => await GetDiagnosticsAsync(compilation, analyzerOptions)).Result;
      foreach(var compilationDiagnostic in diagnostics.Compilation) {
        Console.WriteLine(compilationDiagnostic);
      }
      return diagnostics.Analyzer;
    }

    private async Task<(ImmutableArray<Diagnostic> Analyzer, ImmutableArray<Diagnostic> Compilation)> GetDiagnosticsAsync(
      Compilation compilation,
      ImmutableDictionary<string, string> analyzerOptions
    ) {
      var optionsProvider = new TestAnalyzerConfigOptionsProvider(analyzerOptions);
      var analyzerDiagnostics = await compilation
          .WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new TAnalyzer()),
            new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty, optionsProvider)
          )
          .GetAnalyzerDiagnosticsAsync();
      // The result is ordered for better reproducability.
      return (
        analyzerDiagnostics.OrderBy(diagnostic => diagnostic.Location.SourceSpan).ToImmutableArray(),
        compilation.GetDiagnostics()
          .Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Error)
          .OrderBy(diagnostic => diagnostic.Location.SourceSpan)
          .ToImmutableArray()
      );
    }

    /// <summary>
    /// Verifies that the given diagnostics are reported when analyzing the given source.
    /// </summary>
    /// <param name="source">The source to analyze.</param>
    /// <param name="expectedDiagnostics">The expected diagnostics.</param>
    public void VerifyDiagnostic(string source, params DiagnosticResultLocation[] expectedDiagnostics) {
      VerifyDiagnostic(new[] { source }, expectedDiagnostics);
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
      VerifyDiagnostic(new[] { source }, analyzerOptions, expectedDiagnostics);
    }

    /// <summary>
    /// Verifies that the given diagnostics are reported when analyzing the given collection of sources.
    /// </summary>
    /// <param name="sources">The sources to analyze.</param>
    /// <param name="expectedDiagnostics">The expected diagnostics.</param>
    public void VerifyDiagnostic(IReadOnlyCollection<string> sources, params DiagnosticResultLocation[] expectedDiagnostics) {
      VerifyDiagnostic(sources, ImmutableDictionary.Create<string, string>(), expectedDiagnostics);
    }

    /// <summary>
    /// Verifies that the given diagnostics are reported when analyzing the given collection of sources.
    /// </summary>
    /// <param name="sources">The sources to analyze.</param>
    /// <param name="analyzerOptions">The analyzer options (.editorconfig settings) to pass to the analyzer.</param>
    /// <param name="expectedDiagnostics">The expected diagnostics.</param>
    public void VerifyDiagnostic(
      IReadOnlyCollection<string> sources,
      ImmutableDictionary<string, string> analyzerOptions,
      params DiagnosticResultLocation[] expectedDiagnostics
    ) {
      var diagnosticResults = Analyze(sources.ToArray(), analyzerOptions);
      Assert.AreEqual(expectedDiagnostics.Length, diagnosticResults.Length, "Invalid diagnostics count");

      for(var i = 0; i < expectedDiagnostics.Length; ++i) {
        var result = diagnosticResults[i];
        var expected = expectedDiagnostics[i];
        var span = result.Location.GetLineSpan();
        var actual = new DiagnosticResultLocation(span.Path, span.StartLinePosition.Line, span.StartLinePosition.Character + 1);
        Assert.AreEqual(expected, actual, "Invalid diagnostic");
      }
    }
  }
}
