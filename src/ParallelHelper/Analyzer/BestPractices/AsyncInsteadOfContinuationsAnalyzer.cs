using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.BestPractices {
  /// <summary>
  /// Analyzer that analyzes sources for cases where a method returns a task built with continuations instead
  /// of using the async/await pattern.
  /// 
  /// <example>Illustrates a class with an asynchronous methods that unecessarily makes use of continuations.
  /// <code>
  /// class Sample {
  ///   public Task DoWorkTwiceAsync() {
  ///     return DoWorkAsync().ContinueWith(DoWorkAsync);
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
  public class AsyncInsteadOfContinuationsAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_P009";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Async Instead of Continuation";
    private static readonly LocalizableString MessageFormat = "Replace the continuations with the use of async/await for better readability.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    private static readonly string[] TaskTypes = {
      "System.Threading.Tasks.Task",
      "System.Threading.Tasks.Task`1"
    };
    private const string ContinueWithMethod = "ContinueWith";

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
        if(!IsReturningTask()) {
          return;
        }
        foreach(var continuation in GetContinuationsInSameActivationFrame()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, continuation.GetLocation()));
        }
      }

      private bool IsReturningTask() {
        var method = Node switch {
          MethodDeclarationSyntax methodDeclaration => SemanticModel.GetDeclaredSymbol(methodDeclaration, CancellationToken),
          LocalFunctionStatementSyntax localFunction => (IMethodSymbol)SemanticModel.GetDeclaredSymbol(localFunction, CancellationToken),
          _ => (IMethodSymbol)SemanticModel.GetSymbolInfo(Node, CancellationToken).Symbol
        };
        return method != null && IsTaskType(method.ReturnType);
      }

      private IEnumerable<InvocationExpressionSyntax> GetContinuationsInSameActivationFrame() {
        return Node.DescendantNodesInSameActivationFrame()
          .WithCancellation(CancellationToken)
          .OfType<InvocationExpressionSyntax>()
          .Where(IsContinuation);
      }

      private bool IsContinuation(InvocationExpressionSyntax invocation) {
        return SemanticModel.GetSymbolInfo(invocation, CancellationToken).Symbol is IMethodSymbol method
          && method.Name == ContinueWithMethod
          && IsTaskType(method.ReturnType);
      }

      private bool IsTaskType(ITypeSymbol type) {
        return TaskTypes.Any(typeName => SemanticModel.IsEqualType(type, typeName));
      }
    }
  }
}
