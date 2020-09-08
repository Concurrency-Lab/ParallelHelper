using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Threading;

namespace ParallelHelper.Analyzer {
  /// <summary>
  /// An analysis context wrapper for <see cref="SemanticModelAnalysisContext"/>.
  /// </summary>
  internal class SemanticModelAnalysisContextWrapper : IAnalysisContextWrapper {
    private readonly SemanticModelAnalysisContext _instance;

    public CancellationToken CancellationToken => _instance.CancellationToken;

    public SemanticModel SemanticModel => _instance.SemanticModel;

    public SemanticModelAnalysisContextWrapper(SemanticModelAnalysisContext context) {
      _instance = context;
    }

    public void ReportDiagnostic(Diagnostic diagnostic) {
      _instance.ReportDiagnostic(diagnostic);
    }
  }
}
