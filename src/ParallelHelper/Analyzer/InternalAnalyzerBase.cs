using Microsoft.CodeAnalysis;
using System.Threading;

namespace ParallelHelper.Analyzer {
  /// <summary>
  /// Base analyzer implementation to share common context information.
  /// </summary>
  /// <typeparam name="TContext">The type of the analysis context.</typeparam>
  internal abstract class InternalAnalyzerBase {
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
    /// Initializes the analyzer base.
    /// </summary>
    /// <param name="context">The analysis context to use during the analysis.</param>
    protected InternalAnalyzerBase(IAnalysisContextWrapper context) {
      Context = context;
    }

    /// <summary>
    /// Applies the analysis.
    /// </summary>
    public abstract void Analyze();
  }
}
