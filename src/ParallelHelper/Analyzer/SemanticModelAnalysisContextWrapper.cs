using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Threading;

namespace ParallelHelper.Analyzer {
  /// <summary>
  /// An analysis context wrapper for <see cref="SemanticModelAnalysisContext"/>.
  /// </summary>
  public class SemanticModelAnalysisContextWrapper : IAnalysisContextWrapper<SyntaxNode> {
    private readonly SemanticModelAnalysisContext _instance;

    public CancellationToken CancellationToken => _instance.CancellationToken;

    public SemanticModel SemanticModel => _instance.SemanticModel;

    public SyntaxNode Root => SemanticModel.SyntaxTree.GetRoot(CancellationToken);

    public SemanticModelAnalysisContextWrapper(SemanticModelAnalysisContext context) {
      _instance = context;
    }

    public void ReportDiagnostic(Diagnostic diagnostic) {
      _instance.ReportDiagnostic(diagnostic);
    }
  }
}
