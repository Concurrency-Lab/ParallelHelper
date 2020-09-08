using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ParallelHelper.Analyzer {
  /// <summary>
  /// Base analyzer for implementations that work with the semantic model analysis context.
  /// </summary>
  /// <typeparam name="TRootNode">The syntax type of the root node of the applied analysis.</typeparam>
  internal abstract class SyntaxNodeAnalyzerWithSyntaxWalkerBase<TRootNode>
      : InternalAnalyzerWithSyntaxWalkerBase<TRootNode> where TRootNode : SyntaxNode {
    /// <summary>
    /// Initializes the syntax node analyzer base.
    /// </summary>
    /// <param name="context">The syntax node analysis context to use during the analysis.</param>
    protected SyntaxNodeAnalyzerWithSyntaxWalkerBase(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper<TRootNode>(context)) {
    }
    /// <summary>
    /// Applies the analysis by visiting the node.
    /// </summary>
    public override void Analyze() {
      Visit(Root);
    }
  }
}
