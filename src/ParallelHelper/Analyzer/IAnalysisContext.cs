using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Threading;

namespace ParallelHelper.Analyzer {
  /// <summary>
  /// This interface provides access to the required properties and methods of an analysis context.
  /// The reason for it is that the roslyn provided context types do not share a common base type.
  /// </summary>
  public interface IAnalysisContext {
    /// <summary>
    /// Gets the cancellation token of the currently active analysis.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the semantic model of the currently analyzed document.
    /// </summary>
    SemanticModel SemanticModel { get; }

    /// <summary>
    /// Gets the root node of the currently applied analysis.
    /// </summary>
    SyntaxNode Root { get; }

    /// <summary>
    /// Gets the analyzer configuration of the analyzed syntax tree.
    /// </summary>
    AnalyzerConfigOptions Options { get; }

    /// <summary>
    /// Reports the provided diagnostic.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to report.</param>
    void ReportDiagnostic(Diagnostic diagnostic);
  }
}
