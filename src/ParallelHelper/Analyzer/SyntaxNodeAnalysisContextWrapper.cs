using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Threading;

namespace ParallelHelper.Analyzer {
  /// <summary>
  /// An analysis context wrapper for <see cref="SyntaxNodeAnalysisContext"/>.
  /// </summary>
  /// <typeparam name="TRootNode">The syntax type of the root node of the applied analysis.</typeparam>
  internal class SyntaxNodeAnalysisContextWrapper<TRootNode> : IAnalysisContextWrapper<TRootNode> where TRootNode : SyntaxNode {
    private readonly SyntaxNodeAnalysisContext _instance;

    public CancellationToken CancellationToken => _instance.CancellationToken;

    public SemanticModel SemanticModel => _instance.SemanticModel;

    public TRootNode Root { get; }

    public SyntaxNodeAnalysisContextWrapper(SyntaxNodeAnalysisContext context) {
      _instance = context;
      Root = (TRootNode)context.Node;
    }

    public void ReportDiagnostic(Diagnostic diagnostic) {
      _instance.ReportDiagnostic(diagnostic);
    }
  }
}
