using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.Bugs {
  /// <summary>
  /// Analyzer that analyzes sources that apply null-checks against the task returned by an async method instead of the value.
  /// 
  /// <example>Illustrates a class with a method that invokes an async method and applies the null-check against the task
  /// instead of the task's value.
  /// <code>
  /// using System.Threading.Tasks;
  /// 
  /// class Sample {
  ///   public bool IsNotNull() {
  ///     return GetValueAsync() != null;
  ///   }
  ///   
  ///   private async Task&lt;object&gt; GetValueAsync() {
  ///     await Task.Delay(100);
  ///     return new object();
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class NullCheckAgainstTaskInsteadOfValueAnalyzer : DiagnosticAnalyzer {
    // TODO Flow-analysis to support null-checks against variables that hold the task of an asynchronous method invocation.
    public const string DiagnosticId = "PH_B013";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Null-Check Against Task Instead of Value";
    private static readonly LocalizableString MessageFormat = "The method '{0}' is asynchronous. Asynchronous methods should always return a Task object; thus, the null-check should probably made against its value.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    private static readonly string[] TaskTypes = {
      "System.Threading.Tasks.Task",
      "System.Threading.Tasks.Task`1"
    };

    private const string AsyncSuffix = "Async";

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeEqualsExpression, SyntaxKind.EqualsExpression);
    }

    private static void AnalyzeEqualsExpression(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : SyntaxNodeAnalyzerBase<BinaryExpressionSyntax> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(context) { }

      public override void Analyze() {
        AnalyzePotentialNullCheckAgainstAsyncMethod(Node.Left as LiteralExpressionSyntax, Node.Right as InvocationExpressionSyntax);
        AnalyzePotentialNullCheckAgainstAsyncMethod(Node.Right as LiteralExpressionSyntax, Node.Left as InvocationExpressionSyntax);
      }

      private void AnalyzePotentialNullCheckAgainstAsyncMethod(LiteralExpressionSyntax? potentialNullLiteral, InvocationExpressionSyntax? potentialAsyncMethod) {
        if(potentialNullLiteral == null || potentialAsyncMethod == null) {
          return;
        }
        if(!potentialNullLiteral.IsKind(SyntaxKind.NullLiteralExpression)) {
          return;
        }
        var method = SemanticModel.GetSymbolInfo(potentialAsyncMethod, CancellationToken).Symbol as IMethodSymbol;
        if(method != null && IsAsyncMethodInvocation(method)) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, Node.GetLocation(), method.Name));
        }
      }

      private bool IsAsyncMethodInvocation(IMethodSymbol method) {
        return method.IsAsync
          || (method.Name.EndsWith(AsyncSuffix) && IsTaskType(method.ReturnType));
      }

      private bool IsTaskType(ITypeSymbol type) {
        return TaskTypes.Any(taskType => SemanticModel.IsEqualType(type, taskType));
      }
    }
  }
}
