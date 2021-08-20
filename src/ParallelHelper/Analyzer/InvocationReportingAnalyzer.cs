using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using System.Collections.Generic;

namespace ParallelHelper.Analyzer {
  /// <summary>
  /// Analyzer base that reports any invocation of the provided methods.
  /// </summary>
  internal class InvocationReportingAnalyzer : InternalAnalyzerBase<InvocationExpressionSyntax> {
    private readonly DiagnosticDescriptor _rule;
    private readonly IReadOnlyCollection<ClassMemberDescriptor> _methodsToReport;

    public InvocationReportingAnalyzer(
      SyntaxNodeAnalysisContext context,
      DiagnosticDescriptor rule,
      IReadOnlyCollection<ClassMemberDescriptor> methodsToReport
    ) : base(new SyntaxNodeAnalysisContextWrapper(context)) {
      _rule = rule;
      _methodsToReport = methodsToReport;
    }

    public override void Analyze() {
      if(SemanticModel.GetSymbolInfo(Root, CancellationToken).Symbol is IMethodSymbol method && IsDiscouragedMethod(method)) {
        var methodName = $"{method.ContainingType.Name}.{method.Name}";
        Context.ReportDiagnostic(Diagnostic.Create(_rule, Root.GetLocation(), methodName));
      }
    }

    private bool IsDiscouragedMethod(IMethodSymbol method) {
      return _methodsToReport.AnyContainsMember(SemanticModel, method);
    }
  }
}
