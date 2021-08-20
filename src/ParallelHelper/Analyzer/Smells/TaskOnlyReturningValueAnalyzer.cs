using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Immutable;

namespace ParallelHelper.Analyzer.Smells {
  /// <summary>
  /// Analyzer that analyzes sources for the use of tasks that only return values instead of computing them.
  /// 
  /// <example>Illustrates a class with a method that computes a value and returns a task with the value.
  /// <code>
  /// class Sample {
  ///   public Task&lt;string&gt; DoWorkAsync() {
  ///     return Task.Run(() => "hello"));
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class TaskOnlyReturningValueAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S012";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Task only returning value";
    private static readonly LocalizableString MessageFormat = "Creating a task to return an already computed value is discouraged; use Task.FromResult(...) instead.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    private static readonly ClassMemberDescriptor[] TaskFactoryDescriptors = {
      new ClassMemberDescriptor("System.Threading.Tasks.Task", "Run"),
      new ClassMemberDescriptor("System.Threading.Tasks.TaskFactory", "StartNew"),
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeExpressionStatement, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeExpressionStatement(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerBase<InvocationExpressionSyntax> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper(context)) { }

      public override void Analyze() {
        if(!IsGenericTaskFactory()) {
          return;
        }
        var arguments = Root.ArgumentList.Arguments;
        if (arguments.Count == 0 || !IsExpressionOnlyReturningValue(arguments[0].Expression)) {
          return;
        }
        Context.ReportDiagnostic(Diagnostic.Create(Rule, Root.GetLocation()));
      }

      private bool IsGenericTaskFactory() {
        return SemanticModel.GetSymbolInfo(Root, CancellationToken).Symbol is IMethodSymbol method
          && TaskFactoryDescriptors.AnyContainsMember(SemanticModel, method)
          && IsGenericTaskType(method.ReturnType);
      }

      private static bool IsGenericTaskType(ITypeSymbol taskType) {
        return taskType is INamedTypeSymbol namedType && namedType.IsGenericType;
      }

      private bool IsExpressionOnlyReturningValue(ExpressionSyntax expression) {
        return (expression is ParenthesizedLambdaExpressionSyntax parenthesizedLambda && IsOnlyReturningValueExpression(parenthesizedLambda))
          || (expression is LambdaExpressionSyntax lambda && IsOnlyReturningValueExpression(lambda));
      }

      private bool IsOnlyReturningValueExpression(LambdaExpressionSyntax lambda) {
        if(lambda.Body is ExpressionSyntax expression) {
          return IsValueExpression(expression);
        }
        if(lambda.Body is BlockSyntax block) {
          if(block.Statements.Count == 1 && block.Statements[0] is ReturnStatementSyntax returnStatement) {
            return IsValueExpression(returnStatement.Expression);
          }
        }
        return false;
      }

      private bool IsValueExpression(ExpressionSyntax expression) {
        return IsConstant(expression) || SemanticModel.IsVariable(expression, CancellationToken);
      }

      private static bool IsConstant(ExpressionSyntax expression) {
        return expression is LiteralExpressionSyntax;
      }
    }
  }
}
