using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ParallelHelper.Analyzer {
  /// <summary>
  /// Base analyzer for implementations that work with the semantic model analysis context.
  /// </summary>
  internal abstract class SyntaxNodeAnalyzerBase<TSyntaxNode> : InternalAnalyzerBase<SyntaxNodeAnalysisContext> where TSyntaxNode : SyntaxNode {
    /// <summary>
    /// Gets the semantic model used during the analysis.
    /// </summary>
    public SemanticModel SemanticModel { get; }

    /// <summary>
    /// Gets the node the analysis applied to.
    /// </summary>
    public TSyntaxNode Node;

    /// <summary>
    /// Initializes the syntax node analyzer base.
    /// </summary>
    /// <param name="context">The syntax node analysis context to use during the analysis.</param>
    protected SyntaxNodeAnalyzerBase(SyntaxNodeAnalysisContext context) : base(context, context.CancellationToken) {
      SemanticModel = context.SemanticModel;
      Node = (TSyntaxNode)context.Node;
    }
  }
}
