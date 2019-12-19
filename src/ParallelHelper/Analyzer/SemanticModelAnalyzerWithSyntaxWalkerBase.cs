using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ParallelHelper.Analyzer {
  /// <summary>
  /// Base analyzer for implementations that work with the semantic model analysis context and a syntax walker.
  /// </summary>
  internal abstract class SemanticModelAnalyzerWithSyntaxWalkerBase : InternalAnalyzerWithSyntaxWalkerBase<SemanticModelAnalysisContext> {
    /// <summary>
    /// Gets the semantic model used during the analysis.
    /// </summary>
    public SemanticModel SemanticModel => Context.SemanticModel;

    /// <summary>
    /// Gets the root node of the semantic model.
    /// </summary>
    public SyntaxNode Root => SemanticModel.SyntaxTree.GetRoot(CancellationToken);

    /// <summary>
    /// Initializes the semantic model analyzer with a syntax walker base.
    /// </summary>
    /// <param name="context">The semantic model analysis context to use during the analysis.</param>
    protected SemanticModelAnalyzerWithSyntaxWalkerBase(SemanticModelAnalysisContext context) : base(context, context.CancellationToken) { }

    /// <summary>
    /// Applies the analysis by visiting the root node of the semantic model's syntax tree.
    /// </summary>
    public override void Analyze() {
      Visit(Root);
    }
  }
}
