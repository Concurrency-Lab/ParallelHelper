using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ParallelHelper.Analyzer {
  /// <summary>
  /// Base analyzer for implementations that work with the semantic model analysis context.
  /// </summary>
  internal abstract class SyntaxNodeAnalyzerBase<TSyntaxNode> : InternalAnalyzerBase where TSyntaxNode : SyntaxNode {
    /// <summary>
    /// Gets the node the analysis applied to.
    /// </summary>
    public TSyntaxNode Node;

    /// <summary>
    /// Initializes the syntax node analyzer base.
    /// </summary>
    /// <param name="context">The syntax node analysis context to use during the analysis.</param>
    protected SyntaxNodeAnalyzerBase(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper(context)) {
      Node = (TSyntaxNode)context.Node;
    }
  }
}
