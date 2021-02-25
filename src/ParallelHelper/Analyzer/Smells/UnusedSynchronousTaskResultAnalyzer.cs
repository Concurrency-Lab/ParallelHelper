using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.Smells {
  /// <summary>
  /// Analyzer that analyzes sources for the use of the use of <see cref="System.Threading.Tasks.Task.FromResult{TResult}(TResult)" /> for
  /// synchronous completion althrough task's the return-value is not used.
  /// 
  /// <example>Illustrates a method that completes synchronously with Task.FromResult although the return value is not used.
  /// <code>
  /// class Sample {
  ///   public Task DoWorkAsync() {
  ///     return Task.FromResult(0);
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class UnusedSynchronousTaskResultAnalyzer : DiagnosticAnalyzer {
    // TODO maybe this can be made more generic instead of only Task.FromResult.
    public const string DiagnosticId = "PH_S025";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Unused Synchronous Task Result";
    private static readonly LocalizableString MessageFormat = "The result returned by the synchronous task completion has not any usage; use Task.CompletedTask instead.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    private const string FromResultMethod = "FromResult";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(
        AnalyzeMethodOrAnonymousFunction,
        SyntaxKind.MethodDeclaration,
        SyntaxKind.SimpleLambdaExpression,
        SyntaxKind.ParenthesizedLambdaExpression,
        SyntaxKind.AnonymousMethodExpression,
        SyntaxKind.LocalFunctionStatement
      );
    }

    private static void AnalyzeMethodOrAnonymousFunction(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerBase<SyntaxNode> {
      private readonly TaskAnalysis _taskAnalysis;

      // TODO async void methods?
      // TODO unnecessary use of Task.FromResult in general?
      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper(context)) {
        _taskAnalysis = new TaskAnalysis(context.SemanticModel, context.CancellationToken);
      }

      public override void Analyze() {
        foreach(var unnecessaryFromResult in GetUnnecessaryFromResultInvocations()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, unnecessaryFromResult.GetLocation()));
        }
      }

      private IEnumerable<InvocationExpressionSyntax> GetUnnecessaryFromResultInvocations() {
        return GetFromResultInvocationCandidates()
          .OfType<InvocationExpressionSyntax>()
          .Where(IsTaskFromResultInvocation);
      }

      private IEnumerable<ExpressionSyntax> GetFromResultInvocationCandidates() {
        var awaitedTasks = GetAwaitStatementExpressions();
        if(!ReturnsTaskObjectWithoutValue()) {
          return awaitedTasks;
        }
        return awaitedTasks.Concat(GetReturnValues());
      }

      private bool IsTaskFromResultInvocation(InvocationExpressionSyntax invocation) {
        return SemanticModel.GetSymbolInfo(invocation, CancellationToken).Symbol is IMethodSymbol method
          && method.Name.Equals(FromResultMethod)
          && _taskAnalysis.IsTaskType(method!.ReturnType);
      }

      private bool ReturnsTaskObjectWithoutValue() {
        return SemanticModel.TryGetMethodSymbolFromMethodOrFunctionDeclaration(Root, out var method, CancellationToken)
          && _taskAnalysis.IsTaskType(method!.ReturnType);
      }


      private IEnumerable<ExpressionSyntax> GetReturnValues() {
        var expressionBody = GetExpressionBodyOfMethodOrFunction(Root);
        if(expressionBody != null) {
          return new[] { expressionBody };
        }
        return Root.DescendantNodesInSameActivationFrame()
          .OfType<ReturnStatementSyntax>()
          .Select(returnStatement => returnStatement.Expression)
          .IsNotNull();
      }

      public ExpressionSyntax? GetExpressionBodyOfMethodOrFunction(SyntaxNode node) {
        return node switch {
          BaseMethodDeclarationSyntax method => method.ExpressionBody?.Expression,
          AnonymousFunctionExpressionSyntax function => function.Body is ExpressionSyntax expression ? expression : null,
          LocalFunctionStatementSyntax function => function.ExpressionBody?.Expression,
          _ => null
        };
      }

      private IEnumerable<ExpressionSyntax> GetAwaitStatementExpressions() {
        return Root.DescendantNodesInSameActivationFrame()
          .OfType<ExpressionStatementSyntax>()
          .Select(expressionStatement => expressionStatement.Expression)
          .OfType<AwaitExpressionSyntax>()
          .Select(awaitExpression => awaitExpression.Expression)
          .IsNotNull();
      }
    }
  }
}
