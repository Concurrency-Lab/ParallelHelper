using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections;
using System.Collections.Generic;
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
    private static readonly LocalizableString MessageFormat = "Asynchronous methods should always return a Task object; thus, the null-check should probably made against its value.";
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
      context.RegisterSyntaxNodeAction(AnalyzeCandidate, SyntaxKind.MethodDeclaration, SyntaxKind.AnonymousMethodExpression,
        SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.LocalFunctionStatement);
    }

    private static void AnalyzeCandidate(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : SyntaxNodeAnalyzerBase<SyntaxNode> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(context) { }

      public override void Analyze() {
        var onlyInitializedTaskVariables = GetVariablesInitializedWithAsyncMethodInvocation()
          .Except(GetAllWrittenVariables())
          .ToImmutableHashSet();
        foreach(var equalityCheck in GetEqualityCheckBinaryExpressions()) {
          AnalyzePotentialNullCheckAgainstAsyncMethodTask(equalityCheck, onlyInitializedTaskVariables);
        }
      }

      private IEnumerable<ILocalSymbol> GetAllWrittenVariables() {
        return GetAllAssignedExpressions()
          .Concat(GetAllNodesWrittenByRef())
          .Select(variable => SemanticModel.GetSymbolInfo(variable, CancellationToken).Symbol)
          .OfType<ILocalSymbol>();
      }

      private IEnumerable<ExpressionSyntax> GetAllAssignedExpressions() {
        // The activation frame is not respected here to account for the possibility that the
        // captured variable may change through another activation frame.
        return Root.DescendantNodes()
          .WithCancellation(CancellationToken)
          .OfType<AssignmentExpressionSyntax>()
          .Select(assignment => assignment.Left);
      }

      private IEnumerable<ExpressionSyntax> GetAllNodesWrittenByRef() {
        // The activation frame is not respected here to account for the possibility that the
        // captured variable may change through another activation frame.
        return Root.DescendantNodes()
          .WithCancellation(CancellationToken)
          .OfType<ArgumentSyntax>()
          .Where(argument => argument.RefKindKeyword.IsKind(SyntaxKind.RefKeyword))
          .Select(argument => argument.Expression);
      }

      private IEnumerable<ILocalSymbol> GetVariablesInitializedWithAsyncMethodInvocation() {
        return Root.DescendantNodesInSameActivationFrame()
          .WithCancellation(CancellationToken)
          .OfType<LocalDeclarationStatementSyntax>()
          .SelectMany(declaration => declaration.Declaration.Variables)
          .Where(variable => IsAsyncMethodInvocation(variable.Initializer?.Value))
          .Select(variable => SemanticModel.GetDeclaredSymbol(variable, CancellationToken))
          .Cast<ILocalSymbol>();
      }

      private IEnumerable<BinaryExpressionSyntax> GetEqualityCheckBinaryExpressions() {
        return Root.DescendantNodesInSameActivationFrame()
          .WithCancellation(CancellationToken)
          .OfType<BinaryExpressionSyntax>()
          .Where(binary => binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression));
      }

      private void AnalyzePotentialNullCheckAgainstAsyncMethodTask(BinaryExpressionSyntax expression, ISet<ILocalSymbol> tasksToInclude) {
        if(IsNullCheckAgainstAsyncMethodTask(expression, tasksToInclude)) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, expression.GetLocation()));
        }
      }

      private bool IsNullCheckAgainstAsyncMethodTask(BinaryExpressionSyntax expression, ISet<ILocalSymbol> tasksToInclude) {
        return IsNullCheckAgainstAsyncMethodTask(expression.Left as LiteralExpressionSyntax, expression.Right, tasksToInclude)
          || IsNullCheckAgainstAsyncMethodTask(expression.Right as LiteralExpressionSyntax, expression.Left, tasksToInclude);
      }

      private bool IsNullCheckAgainstAsyncMethodTask(LiteralExpressionSyntax? potentialNullLiteral, ExpressionSyntax potentialAsyncMethodTask,
          ISet<ILocalSymbol> tasksToInclude) {
        if(potentialNullLiteral == null || !potentialNullLiteral.IsKind(SyntaxKind.NullLiteralExpression)) {
          return false;
        }
        var symbol = SemanticModel.GetSymbolInfo(potentialAsyncMethodTask, CancellationToken).Symbol;
        return tasksToInclude.Contains(symbol) || IsAsyncMethodInvocation(symbol);
      }

      private bool IsAsyncMethodInvocation(ExpressionSyntax? expression) {
        return expression is InvocationExpressionSyntax invocation
          && SemanticModel.GetSymbolInfo(invocation, CancellationToken).Symbol is IMethodSymbol method
          && IsAsyncMethodInvocation(method);
      }

      private bool IsAsyncMethodInvocation(ISymbol symbol) {
        if(symbol is IMethodSymbol method) {
          return method.IsAsync
            || (method.Name.EndsWith(AsyncSuffix) && IsTaskType(method.ReturnType));
        }
        return false;
      }

      private bool IsTaskType(ITypeSymbol type) {
        return TaskTypes.Any(taskType => SemanticModel.IsEqualType(type, taskType));
      }
    }
  }
}
