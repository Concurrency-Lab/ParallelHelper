using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Threading;

namespace ParallelHelper.Analyzer {
  /// <summary>
  /// An analysis context wrapper for <see cref="SyntaxNodeAnalysisContext"/>.
  /// </summary>
  internal class SyntaxNodeAnalysisContextWrapper : IAnalysisContextWrapper {
    private readonly SyntaxNodeAnalysisContext _instance;

    public CancellationToken CancellationToken => _instance.CancellationToken;

    public SemanticModel SemanticModel => _instance.SemanticModel;

    public SyntaxNodeAnalysisContextWrapper(SyntaxNodeAnalysisContext context) {
      _instance = context;
    }

    public void ReportDiagnostic(Diagnostic diagnostic) {
      _instance.ReportDiagnostic(diagnostic);
    }
  }
}
