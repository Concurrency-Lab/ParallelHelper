using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Threading;

namespace ParallelHelper.Analyzer {
  /// <summary>
  /// Base analyzer implementation to share common context information.
  /// The <see cref="CSharpSyntaxWalker.Visit(SyntaxNode)"/> method is overriden to respect the context's cancellation token.
  /// </summary>
  /// <typeparam name="TContext">The type of the analysis context.</typeparam>
  public abstract class InternalAnalyzerWithSyntaxWalkerBase<TContext> : CSharpSyntaxWalker {
    /// <summary>
    /// Gets the analysis context used during the analysis.
    /// </summary>
    public TContext Context { get; }

    /// <summary>
    /// Gets the cancellation token to respect for cancellations.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Initializes the analyzer base.
    /// </summary>
    /// <param name="context">The analysis context to use during the analysis.</param>
    /// <param name="cancellationToken">The cancellation token to respect for cancellations.</param>
    protected InternalAnalyzerWithSyntaxWalkerBase(TContext context, CancellationToken cancellationToken) {
      Context = context;
      CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Applies the analysis.
    /// </summary>
    public abstract void Analyze();

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
