using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ParallelHelper.Analyzer {
  /// <summary>
  /// Base analyzer for implementations that work with the semantic model analysis context.
  /// </summary>
  internal abstract class SemanticModelAnalyzerBase : InternalAnalyzerBase {
    /// <summary>
    /// Gets the root node of the semantic model.
    /// </summary>
    public SyntaxNode Root => SemanticModel.SyntaxTree.GetRoot(CancellationToken);

    /// <summary>
    /// Initializes the semantic model analyzer base.
    /// </summary>
    /// <param name="context">The semantic model analysis context to use during the analysis.</param>
    protected SemanticModelAnalyzerBase(SemanticModelAnalysisContext context) : base(new SemanticModelAnalysisContextWrapper(context)) {
    }
  }
}
