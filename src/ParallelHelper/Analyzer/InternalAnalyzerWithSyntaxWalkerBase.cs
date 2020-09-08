using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Threading;

namespace ParallelHelper.Analyzer {
  /// <summary>
  /// Base analyzer implementation to share common context information.
  /// The <see cref="CSharpSyntaxWalker.Visit(SyntaxNode)"/> method is overriden to respect the context's cancellation token.
  /// </summary>
  /// <typeparam name="TRootNode">The syntax type of the root node of the applied analysis.</typeparam>
  public abstract class InternalAnalyzerWithSyntaxWalkerBase<TRootNode> : CSharpSyntaxWalker where TRootNode : SyntaxNode {
    /// <summary>
    /// Gets the analysis context used during the analysis.
    /// </summary>
    public IAnalysisContextWrapper<TRootNode> Context { get; }

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
    public TRootNode Root => Context.Root;

    /// <summary>
    /// Initializes the analyzer base.
    /// </summary>
    /// <param name="context">The analysis context to use during the analysis.</param>
    protected InternalAnalyzerWithSyntaxWalkerBase(IAnalysisContextWrapper<TRootNode> context) {
      Context = context;
    }

    /// <summary>
    /// Applies the analysis by visiting the root node of the analysis.
    /// </summary>
    public virtual void Analyze() {
      Visit(Root);
    }

    /// <summary>
    /// Basic visit method that respects the provided cancellation token.
    /// </summary>
    /// <param name="node">The node to visit</param>
    /// <exception cref="System.Threading.CancellationToken">Thrown when a cancellation was requested.</exception>
    public override void Visit(SyntaxNode node) {
      CancellationToken.ThrowIfCancellationRequested();
      base.Visit(node);
    }
  }
}
