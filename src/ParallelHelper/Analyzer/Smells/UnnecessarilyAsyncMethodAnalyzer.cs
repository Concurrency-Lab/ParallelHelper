using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace ParallelHelper.Analyzer.Smells {
  /// <summary>
  /// Analyzer that analyzes sources for methods that are unnecessarily declared async.
  /// 
  /// <example>Illustrates a class with an asynchronous method whose last operation awaits an asynchronous method
  /// instead returning the task object directly.
  /// <code>
  /// class Sample {
  ///   public async Task DoWorkAsync() {
  ///     // ...
  ///     await DoOtherWorkAsync();
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class UnnecessarilyAsyncMethodAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S006";

    private const string Category = "Concurrency";

    private const string GenericTaskType = "System.Threading.Tasks.Task`1";

    private static readonly LocalizableString Title = "Unnecessarily Async Methods";
    private static readonly LocalizableString MessageFormat = "Declaring methods async unnecessarily is discouraged.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : SyntaxNodeAnalyzerBase<MethodDeclarationSyntax> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(context) { }

      public override void Analyze() {
        if(!HasMethodBody() || !IsAsyncMethod() || ReturnsVoid() || !AwaitsOnlyFinalExpression()) {
          return;
        }
        Context.ReportDiagnostic(Diagnostic.Create(Rule, Root.GetSignatureLocation()));
      }

      private bool HasMethodBody() {
        return Root.Body != null || Root.ExpressionBody != null;
      }

      private bool IsAsyncMethod() {
        return Root.Modifiers.WithCancellation(CancellationToken).Any(modifier => modifier.IsKind(SyntaxKind.AsyncKeyword));
      }

      private bool ReturnsVoid() {
        return GetReturnType()?.SpecialType == SpecialType.System_Void;
      }

      private ITypeSymbol GetReturnType() {
        return SemanticModel.GetTypeInfo(Root.ReturnType, CancellationToken).Type;
      }

      private bool AwaitsOnlyFinalExpression() {
        if(Root.ExpressionBody != null) {
          return IsAwaitOnlyExpressionBody();
        } else if(!IsAsyncMethodReturningValues()) {
          return HasAtMostOneAwaitInBody() && IsFinalStatementAwait();
        }

        var returnOnlyAwaitScanner = new ReturnOnlyAwaitScanner(CancellationToken);
        returnOnlyAwaitScanner.Visit(Root);
        return returnOnlyAwaitScanner.UsesOnlyUnscopedReturnAwaits;
      }

      private bool IsAwaitOnlyExpressionBody() {
        return Root.ExpressionBody.Expression is AwaitExpressionSyntax awaitExpression && !HasNestedAwait(awaitExpression);
      }

      private bool IsAsyncMethodReturningValues() {
        return SemanticModel.IsEqualType(GetReturnType(), GenericTaskType);
      }

      private bool HasAtMostOneAwaitInBody() {
        return Root.Body.DescendantNodes().WithCancellation(CancellationToken).OfType<AwaitExpressionSyntax>().Count() <= 1;
      }

      private bool IsFinalStatementAwait() {
        var statements = Root.Body.Statements;
        return statements.Count > 0 && (statements[statements.Count - 1] as ExpressionStatementSyntax)?.Expression is AwaitExpressionSyntax;
      }

      private bool HasNestedAwait(SyntaxNode node) {
        return node.DescendantNodes().WithCancellation(CancellationToken).OfType<AwaitExpressionSyntax>().Any();
      }
    }

    // TODO: Support for inner methods and lambda expressions? Should be fine simply not visiting these cases.
    private class ReturnOnlyAwaitScanner : CSharpSyntaxWalker {
      public bool UsesOnlyUnscopedReturnAwaits { get; set; } = true;

      private readonly CancellationToken _cancellationToken;

      private int _usingDepth = 0;
      private int _tryDepth = 0;
      private int _awaitDepth = 0;

      public ReturnOnlyAwaitScanner(CancellationToken cancellationToken) {
        _cancellationToken = cancellationToken;
      }

      public override void Visit(SyntaxNode node) {
        _cancellationToken.ThrowIfCancellationRequested();
        base.Visit(node);
      }

      public override void VisitUsingStatement(UsingStatementSyntax node) {
        ++_usingDepth;
        base.VisitUsingStatement(node);
        --_usingDepth;
      }

      public override void VisitTryStatement(TryStatementSyntax node) {
        ++_tryDepth;
        base.VisitTryStatement(node);
        --_usingDepth;
      }

      public override void VisitAwaitExpression(AwaitExpressionSyntax node) {
        if(IsInsideScopedStatement() || !IsEnclosedByReturnStatement(node)) {
          UsesOnlyUnscopedReturnAwaits = false;
          return;
        }
        ++_awaitDepth;
        base.VisitAwaitExpression(node);
        --_awaitDepth;
      }

      private bool IsEnclosedByReturnStatement(SyntaxNode node) {
        return node.Parent is ReturnStatementSyntax;
      }

      private bool IsInsideScopedStatement() {
        return _usingDepth > 0 || _tryDepth > 0 || _awaitDepth > 0;
      }
    }
  }
}
