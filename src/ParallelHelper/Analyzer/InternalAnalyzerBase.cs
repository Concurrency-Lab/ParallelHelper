using System.Threading;

namespace ParallelHelper.Analyzer {
  /// <summary>
  /// Base analyzer implementation to share common context information.
  /// </summary>
  /// <typeparam name="TContext">The type of the analysis context.</typeparam>
  internal abstract class InternalAnalyzerBase<TContext> {
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
    protected InternalAnalyzerBase(TContext context, CancellationToken cancellationToken) {
      Context = context;
      CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Applies the analysis.
    /// </summary>
    public abstract void Analyze();
  }
}
