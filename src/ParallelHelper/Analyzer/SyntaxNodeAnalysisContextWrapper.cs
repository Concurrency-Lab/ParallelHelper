using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Threading;

namespace ParallelHelper.Analyzer {
  /// <summary>
  /// An analysis context wrapper for <see cref="SyntaxNodeAnalysisContext"/>.
  /// </summary>
  public class SyntaxNodeAnalysisContextWrapper : IAnalysisContext {
    private readonly SyntaxNodeAnalysisContext _instance;

    public CancellationToken CancellationToken => _instance.CancellationToken;

    public SemanticModel SemanticModel => _instance.SemanticModel;

    public SyntaxNode Root => _instance.Node;

    /// <summary>
    /// Initializes a new instance with the specified syntax node analysis context.
    /// </summary>
    /// <param name="context">The syntax node analysis to wrap.</param>
    public SyntaxNodeAnalysisContextWrapper(SyntaxNodeAnalysisContext context) {
      _instance = context;
    }

    public void ReportDiagnostic(Diagnostic diagnostic) {
      _instance.ReportDiagnostic(diagnostic);
    }
  }
}
