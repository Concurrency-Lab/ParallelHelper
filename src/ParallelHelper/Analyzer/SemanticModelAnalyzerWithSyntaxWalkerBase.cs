using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ParallelHelper.Analyzer {
  /// <summary>
  /// Base analyzer for implementations that work with the semantic model analysis context and a syntax walker.
  /// </summary>
  public abstract class SemanticModelAnalyzerWithSyntaxWalkerBase : InternalAnalyzerWithSyntaxWalkerBase<SyntaxNode> {
    /// <summary>
    /// Initializes the semantic model analyzer with a syntax walker base.
    /// </summary>
    /// <param name="context">The semantic model analysis context to use during the analysis.</param>
    protected SemanticModelAnalyzerWithSyntaxWalkerBase(SemanticModelAnalysisContext context) : base(new SemanticModelAnalysisContextWrapper(context)) { }
  }
}
