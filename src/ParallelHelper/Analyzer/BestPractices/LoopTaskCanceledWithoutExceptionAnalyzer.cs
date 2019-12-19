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
  /// Analyzer that analyzes sources for cases where task-based loops are actually canceled
  /// without firing the <see cref="System.OperationCanceledException"/> exception. Therefore, the task's
  /// state is not set to canceled.
  /// 
  /// <example>Illustrates a class with a loop checking <see cref="System.Threading.CancellationToken.IsCancellationRequested"/>
  /// but not throwing the proper exception.
  /// <code>
  /// class Sample {
  ///   private object syncObject = new object();
  ///   
  ///   public async Task DoWork(CancellationToken cancellationToken) {
  ///     while(!cancellationToken.IsCancellationRequested) {
  ///       // Do work
  ///     }
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class LoopTaskCanceledWithoutExceptionAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_P008";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Missing OperationCanceledException in Task";
    private static readonly LocalizableString MessageFormat = "Throw an OperationCanceledException when a cancellation was requested to mark the task as canceled.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    private const string OperationCanceledExceptionType = "System.OperationCanceledException";

    private static readonly string[] CancellationTokenTypes = {
      "System.Threading.CancellationToken",
      "System.Threading.CancellationTokenSource"
    };
    private const string IsCancellationRequestedProperty = "IsCancellationRequested";
    private const string ThrowIfCancellationRequestedMethod = "ThrowIfCancellationRequested";

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
        if(IsAsyncMethod() && ContainsCanceledLoopWithoutThrowingException()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, Node.GetSignatureLocation()));
        }
      }

      private bool ContainsCanceledLoopWithoutThrowingException() {
        return ContainsCanceledLoop() 
          && !InvokesThrowIfCancellationRequested() 
          && !ThrowsOperationCanceledException();
      }

      private bool IsAsyncMethod() {
        return Node.Modifiers.Any(SyntaxKind.AsyncKeyword);
      }

      private bool ContainsCanceledLoop() {
        return GetAllLoopConditions()
          .SelectMany(condition => condition.DescendantNodesAndSelf())
          .WithCancellation(CancellationToken)
          .OfType<MemberAccessExpressionSyntax>()
          .Any(AccessesIsCancellationRequestedProperty);
      }

      private IEnumerable<ExpressionSyntax> GetAllLoopConditions() {
        var whileConditions = Node.DescendantNodes()
          .WithCancellation(CancellationToken)
          .OfType<WhileStatementSyntax>()
          .Select(whileStatement => whileStatement.Condition);
        var forConditions = Node.DescendantNodes()
          .WithCancellation(CancellationToken)
          .OfType<ForStatementSyntax>()
          .Select(forStatement => forStatement.Condition);
        var doConditions = Node.DescendantNodes()
          .WithCancellation(CancellationToken)
          .OfType<DoStatementSyntax>()
          .Select(doStatement => doStatement.Condition);
        return whileConditions.Concat(forConditions).Concat(doConditions);
      }

      private bool AccessesIsCancellationRequestedProperty(MemberAccessExpressionSyntax memberAccess) {
        return SemanticModel.GetSymbolInfo(memberAccess, CancellationToken).Symbol is IPropertySymbol property
          && property.Name.Equals(IsCancellationRequestedProperty)
          && IsCancellationTokenType(property.ContainingType);
      }

      private bool InvokesThrowIfCancellationRequested() {
        return Node.DescendantNodes()
          .WithCancellation(CancellationToken)
          .OfType<InvocationExpressionSyntax>()
          .Any(IsThrowIfCancellationRequestedInvocation);
      }

      private bool IsThrowIfCancellationRequestedInvocation(InvocationExpressionSyntax invocation) {
        return SemanticModel.GetSymbolInfo(invocation, CancellationToken).Symbol is IMethodSymbol method
          && method.Name.Equals(ThrowIfCancellationRequestedMethod)
          && IsCancellationTokenType(method.ContainingType);
      }

      private bool ThrowsOperationCanceledException() {
        var throwExpressions = Node.DescendantNodes()
          .WithCancellation(CancellationToken)
          .OfType<ThrowExpressionSyntax>()
          .Select(throwExpression => throwExpression.Expression);
        var throwStatements = Node.DescendantNodes()
          .WithCancellation(CancellationToken)
          .OfType<ThrowStatementSyntax>()
          .Select(throwStatement => throwStatement.Expression)
          .IsNotNull(); // TODO capture the exception that is re-thrown
        return throwExpressions.Concat(throwStatements).Any(IsOperationCanceledException);
      }

      private bool IsOperationCanceledException(ExpressionSyntax expression) {
        var type = SemanticModel.GetTypeInfo(expression, CancellationToken).Type;
        return SemanticModel.IsEqualType(type, OperationCanceledExceptionType);
      }

      private bool IsCancellationTokenType(ITypeSymbol type) {
        return CancellationTokenTypes
          .WithCancellation(CancellationToken)
          .Any(typeName => SemanticModel.IsEqualType(type, typeName));
      }
    }
  }
}
