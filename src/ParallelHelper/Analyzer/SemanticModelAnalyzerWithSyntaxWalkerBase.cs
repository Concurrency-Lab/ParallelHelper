using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ParallelHelper.Analyzer {
  /// <summary>
  /// Base analyzer for implementations that work with the semantic model analysis context and a syntax walker.
  /// </summary>
  public abstract class SemanticModelAnalyzerWithSyntaxWalkerBase : InternalAnalyzerWithSyntaxWalkerBase {
    /// <summary>
    /// Gets the root node of the semantic model.
    /// </summary>
    public SyntaxNode Root => SemanticModel.SyntaxTree.GetRoot(CancellationToken);

    /// <summary>
    /// Initializes the semantic model analyzer with a syntax walker base.
    /// </summary>
    /// <param name="context">The semantic model analysis context to use during the analysis.</param>
    protected SemanticModelAnalyzerWithSyntaxWalkerBase(SemanticModelAnalysisContext context) : base(new SemanticModelAnalysisContextWrapper(context)) { }

    /// <summary>
    /// Applies the analysis by visiting the root node of the semantic model's syntax tree.
    /// </summary>
    public override void Analyze() {
      Visit(Root);
    }
  }
}
