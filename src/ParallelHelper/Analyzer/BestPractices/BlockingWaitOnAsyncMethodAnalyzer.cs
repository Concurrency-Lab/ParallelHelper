using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Util;
using System;
using System.Collections.Immutable;

namespace ParallelHelper.Analyzer.BestPractices {
  /// <summary>
  /// Analyzer that analyzes sources for the use of blocking results from async methods/tasks without enclosing them with a gate-keeper.
  /// 
  /// <example>Illustrates a class that uses a blocking <see cref="System.Threading.Tasks.Task.Wait"/> on an async method without enclosing it with a 
  /// <see cref="System.Threading.Tasks.Task.Run(System.Action)"/> invocation to avoid blocking the potential synchronization context.
  /// <code>
  /// class Sample {
  ///   private object syncObject = new object();
  ///   
  ///   public void DoWork() {
  ///     DoWorkAsync().Wait();
  ///   }
  ///   
  ///   public async Task DoWorkAsync() {
  ///   
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class BlockingWaitOnAsyncMethodAnalyzer : DiagnosticAnalyzer {
    // TODO maybe limit this to only analyze code that potentially has a synchronization context.
    public const string DiagnosticId = "PH_P005";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Blocking Wait on Async Method";
    private static readonly LocalizableString MessageFormat = "The access is '{0}' is blocking. Either use the async/await pattern or use a gate-keeper to prevent capturing of the synchronization context.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    private const string AsyncSuffix = "Async";

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
      context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context) {
      new Analyzer<InvocationExpressionSyntax>(
        context,
        new TaskAnalysis(context.SemanticModel, context.CancellationToken).IsBlockingMethodInvocation, 
        node => (node.Expression as MemberAccessExpressionSyntax)?.Expression
      ).Analyze();
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context) {
      new Analyzer<MemberAccessExpressionSyntax>(
        context,
        new TaskAnalysis(context.SemanticModel, context.CancellationToken).IsBlockingPropertyAccess,
        node => node.Expression
      ).Analyze();
    }

    private class Analyzer<TExpression> : InternalAnalyzerBase<TExpression> where TExpression : ExpressionSyntax {
      private readonly Func<TExpression, bool> _isBlockingAccess;
      private readonly Func<TExpression, ExpressionSyntax?> _getInstanceExpression;

      public Analyzer(SyntaxNodeAnalysisContext context, Func<TExpression, bool> isBlockingAccess, Func<TExpression, ExpressionSyntax?> getInstanceExpression)
          : base(new SyntaxNodeAnalysisContextWrapper(context)) {
        _isBlockingAccess = isBlockingAccess;
        _getInstanceExpression = getInstanceExpression;
      }

      public override void Analyze() {
        var member = SemanticModel.GetSymbolInfo(Root, CancellationToken).Symbol;
        if(member != null && IsBlockingMemberAccessOnAsyncMethod()) {
          var access = $"{member.ContainingType.Name}.{member.Name}";
          Context.ReportDiagnostic(Diagnostic.Create(Rule, Root.GetLocation(), access));
        }
      }

      private bool IsBlockingMemberAccessOnAsyncMethod() {
        var instanceExpression = _getInstanceExpression(Root);
        return instanceExpression != null
          && IsPotentiallyAsyncMethod(instanceExpression)
          && _isBlockingAccess(Root);
      }

      private bool IsPotentiallyAsyncMethod(ExpressionSyntax expression) {
        return SemanticModel.GetSymbolInfo(expression, CancellationToken).Symbol is IMethodSymbol method
          && (method.IsAsync || method.Name.EndsWith(AsyncSuffix));
      }
    }
  }
}
