using Microsoft.CodeAnalysis;
using System.Threading;

namespace ParallelHelper.Analyzer {
  /// <summary>
  /// Base analyzer implementation to share common context information.
  /// </summary>
  /// <typeparam name="TRootNode">The syntax type of the root node of the applied analysis.</typeparam>
  internal abstract class InternalAnalyzerBase<TRootNode> where TRootNode : SyntaxNode {
    /// <summary>
    /// Gets the analysis context used during the analysis.
    /// </summary>
    public IAnalysisContextWrapper Context { get; }

    /// <summary>
    /// Gets the cancellation token to respect for cancellations.
    /// </summary>
    public CancellationToken CancellationToken => Context.CancellationToken;

    /// <summary>
    /// Gets the semantic model of the currently analyzed document.
    /// </summary>
    public SemanticModel SemanticModel => Context.SemanticModel;

    /// <summary>
    /// Gets the root node of the currently applied analysis.
    /// </summary>
    public TRootNode Root { get; }

    /// <summary>
    /// Initializes the analyzer base.
    /// </summary>
    /// <param name="context">The analysis context to use during the analysis.</param>
    protected InternalAnalyzerBase(IAnalysisContextWrapper context) {
      Context = context;
      Root = (TRootNode)context.Root;
    }

    /// <summary>
    /// Applies the analysis.
    /// </summary>
    public abstract void Analyze();
  }
}
