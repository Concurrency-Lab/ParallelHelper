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
    /// Analyzes the given source.
    /// </summary>
    /// <param name="source">The source code to analyze.</param>
    /// <returns>The diagnostics returned by the analysis.</returns>
    public ImmutableArray<Diagnostic> Analyze(string source) {
      var compilation = CompilationFactory.CreateCompilation(source);
      var diagnostics = Task.Run(async () => await GetDiagnosticsAsync(compilation)).Result;
      foreach(var compilationDiagnostic in diagnostics.Compilation) {
        Console.WriteLine(compilationDiagnostic);
      }
      return diagnostics.Analyzer;
    }

    private async Task<(ImmutableArray<Diagnostic> Analyzer, ImmutableArray<Diagnostic> Compilation)> GetDiagnosticsAsync(Compilation compilation) {
      var analyzerDiagnostics = await compilation
          .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new TAnalyzer()))
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
      var diagnosticResults = Analyze(source);
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
