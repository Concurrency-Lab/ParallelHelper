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
  /// Analyzer that analyzes sources for (potentially) async methods that blockingly wait for tasks.
  /// 
  /// <example>Illustrates a class with an asynchronous method that blockingly waits for the tasks completion.
  /// <code>
  /// class Sample {
  ///   public async Task DoWorkTwiceAsync() {
  ///     DoWorkAsync().Wait();
  ///     DoWorkAsync().Wait();
  ///     return Task.CompletedTask;
  ///   }
  ///   
  ///   public Task DoWorkAsync() {
  ///     return Task.CompletedTask;
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class BlockingWaitInAsyncMethodAnalyzer : DiagnosticAnalyzer {
    // TODO Using a DFA to refine the exclusion of previously awaited tasks can reduce the number of false negatives.
    public const string DiagnosticId = "PH_S026";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Blocking Wait in Async Method";
    private static readonly LocalizableString MessageFormat = "The method appears to be asynchronous, but it synchronously waits for the completion of a task. Use 'await' instead.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    private const string ConfigureAwaitMethod = "ConfigureAwait";

    private static readonly string[] WhenMethods = { "WhenAll", "WhenAny" };

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

      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper(context)) {
        _taskAnalysis = new TaskAnalysis(context.SemanticModel, context.CancellationToken);
      }

      public override void Analyze() {
        if(!IsAsyncMethod()) {
          return;
        }
        foreach(var blockingTaskUsage in GetBlockingTaskUsages()){
          Context.ReportDiagnostic(Diagnostic.Create(Rule, blockingTaskUsage.GetLocation()));
        }
      }

      private bool IsAsyncMethod() {
        return Root.IsMethodOrFunctionWithAsyncModifier()
          || _taskAnalysis.IsMethodOrFunctionReturningTask(Root);
      }

      private IEnumerable<ExpressionSyntax> GetBlockingTaskUsages() {
        var awaitedTasks = GetAllAwaitedTasks().ToImmutableHashSet();
        return GetBlockingMethodInvocationsWithout(awaitedTasks)
          .Cast<ExpressionSyntax>()
          .Concat(GetBlockingPropertyAccessesWithout(awaitedTasks));
      }

      private IEnumerable<InvocationExpressionSyntax> GetBlockingMethodInvocationsWithout(ISet<ISymbol> excludedTasks) {
        return Root.DescendantNodesInSameActivationFrame()
          .WithCancellation(CancellationToken)
          .OfType<InvocationExpressionSyntax>()
          .Where(_taskAnalysis.IsBlockingMethodInvocation)
          .Where(invocation => !IsPotentiallyDesignatingSymbolOf(invocation.Expression, excludedTasks));
      }

      private IEnumerable<MemberAccessExpressionSyntax> GetBlockingPropertyAccessesWithout(ISet<ISymbol> excludedTasks) {
        return Root.DescendantNodesInSameActivationFrame()
          .WithCancellation(CancellationToken)
          .OfType<MemberAccessExpressionSyntax>()
          .Where(_taskAnalysis.IsBlockingPropertyAccess)
          .Where(memberAccess => !IsPotentiallyDesignatingSymbolOf(memberAccess, excludedTasks));
      }

      private bool IsPotentiallyDesignatingSymbolOf(ExpressionSyntax expression, ISet<ISymbol> symbols) {
        var memberAccess = expression as MemberAccessExpressionSyntax;
        if(memberAccess == null) {
          return true;
        }
        var symbol = SemanticModel.GetSymbolInfo(memberAccess.Expression, CancellationToken).Symbol;
        return symbol != null && symbols.Contains(symbol);
      }

      private IEnumerable<ISymbol> GetAllAwaitedTasks() {
        return Root.DescendantNodesInSameActivationFrame()
          .OfType<AwaitExpressionSyntax>()
          .SelectMany(GetTasksAwaitedByAwaitExpression);
      }

      private IEnumerable<ISymbol> GetTasksAwaitedByAwaitExpression(AwaitExpressionSyntax awaitExpression) {
        return GetTasksFromAwaitedExpression(UnwrapPotentialConfigureAwait(awaitExpression.Expression));
      }

      private ExpressionSyntax UnwrapPotentialConfigureAwait(ExpressionSyntax expression) {
        if (expression is InvocationExpressionSyntax invocation && invocation.Expression is MemberAccessExpressionSyntax memberAccess
            && IsConfigureAwaitInvocation(invocation)) {
          return memberAccess.Expression;
        }
        return expression;
      }

      private IEnumerable<ISymbol> GetTasksFromAwaitedExpression(ExpressionSyntax expression) {
        if (expression is InvocationExpressionSyntax invocation && IsTaskWhenMethodInvocation(invocation)) {
          return GetPassedTaskVariables(invocation);
        }
        var symbol = SemanticModel.GetSymbolInfo(expression, CancellationToken).Symbol;
        if (symbol.IsVariable()) {
          return new[] { symbol };
        }
        return Enumerable.Empty<ISymbol>();
      }

      private IEnumerable<ISymbol> GetPassedTaskVariables(InvocationExpressionSyntax invocation) {
        return invocation.ArgumentList.Arguments
          .WithCancellation(CancellationToken)
          .Select(argument => argument.Expression)
          .Select(expression => SemanticModel.GetSymbolInfo(expression, CancellationToken).Symbol)
          .Where(symbol => symbol.IsVariable());
      }

      private bool IsTaskWhenMethodInvocation(InvocationExpressionSyntax invocation) {
        return SemanticModel.GetSymbolInfo(invocation, CancellationToken).Symbol is IMethodSymbol method
          && WhenMethods.Any(name => name == method.Name)
          && _taskAnalysis.IsTaskType(method.ContainingType);
      }

      private bool IsConfigureAwaitInvocation(InvocationExpressionSyntax invocation) {
        return SemanticModel.GetSymbolInfo(invocation, CancellationToken).Symbol is IMethodSymbol method
          && ConfigureAwaitMethod == method.Name
          && _taskAnalysis.IsTaskType(method.ContainingType);
      }
    }
  }
}
