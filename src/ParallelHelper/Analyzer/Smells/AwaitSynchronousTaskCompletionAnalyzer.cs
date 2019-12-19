using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Immutable;

namespace ParallelHelper.Analyzer.Smells {
  /// <summary>
  /// Analyzer that analyzes sources for the combined use of <c>await</c> and a synchronous task completion such as
  /// <see cref="System.Threading.Tasks.Task.FromResult{TResult}(TResult)"/>.
  /// 
  /// <example>Illustrates a class with a method that awaits a synchronously completed task.
  /// <code>
  /// class Sample {
  ///   public async Task DoWorkAsync() {
  ///     await Task.FromResult(0);
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class AwaitSynchronousTaskCompletionAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S020";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Awaiting Synchronous Task Completions";
    private static readonly LocalizableString MessageFormat = "Creating a completed Task to await it is discouraged; use a synchronous flow.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    private const string TaskType = "System.Threading.Tasks.Task";
    private const string FromResultMethod = "FromResult";
    private const string CompletedTaskProperty = "CompletedTask";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeAwaitExpression, SyntaxKind.AwaitExpression);
    }

    private static void AnalyzeAwaitExpression(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : SyntaxNodeAnalyzerBase<AwaitExpressionSyntax> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(context) { }

      public override void Analyze() {
        if(!AwaitsSynchronouslyCompletedTask()) {
          return;
        }
        Context.ReportDiagnostic(Diagnostic.Create(Rule, Node.GetLocation()));
      }

      private bool AwaitsSynchronouslyCompletedTask() {
        return IsTaskFromResult(Node.Expression)
          || IsCompletedTask(Node.Expression);
      }

      private bool IsTaskFromResult(ExpressionSyntax expression) {
        return expression is InvocationExpressionSyntax invocation
          && SemanticModel.GetSymbolInfo(invocation, CancellationToken).Symbol is IMethodSymbol method
          && method.Name.Equals(FromResultMethod)
          && SemanticModel.IsEqualType(method.ContainingType, TaskType);
      }

      private bool IsCompletedTask(ExpressionSyntax expression) {
        return expression is MemberAccessExpressionSyntax memberAccess
          && SemanticModel.GetSymbolInfo(memberAccess, CancellationToken).Symbol is IPropertySymbol property
          && property.Name.Equals(CompletedTaskProperty)
          && SemanticModel.IsEqualType(property.ContainingType, TaskType);
      }
    }
  }
}
