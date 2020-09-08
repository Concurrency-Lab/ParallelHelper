using Microsoft.CodeAnalysis;
using System.Threading;

namespace ParallelHelper.Analyzer {
  /// <summary>
  /// Since the analysis contexts of roslyn do not share a common base type, this interface provides
  /// direct access to the necessary analysis context methods to make common analysis classes independent
  /// of the concrete context types.
  /// </summary>
  /// <typeparam name="TRootNode">The syntax type of the root node of the applied analysis.</typeparam>
  public interface IAnalysisContextWrapper<TRootNode> where TRootNode : SyntaxNode {
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
    TRootNode Root { get; }

    /// <summary>
    /// Reports the provided diagnostic.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to report.</param>
    void ReportDiagnostic(Diagnostic diagnostic);
  }
}
