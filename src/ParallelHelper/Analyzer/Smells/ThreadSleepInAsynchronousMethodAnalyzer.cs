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
  /// Analyzer that analyzes sources for the use of <c>Thread.Sleep(...)</c> inside asynchronous methods.
  /// 
  /// <example>Illustrates a class with an asynchronous method that makes use of <c>Thread.Sleep(...)</c>.
  /// <code>
  /// class Sample {
  ///   public async Task DoWork() {
  ///     Thread.Sleep(100);
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class ThreadSleepInAsynchronousMethodAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S013";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Thread.Sleep in Async Method";
    private static readonly LocalizableString MessageFormat = "The use of Thread.Sleep inside asynchronous methods is discouraged. Use 'await Task.Delay(...)' instead.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    private const string ThreadType = "System.Threading.Thread";
    private const string SleepMethod = "Sleep";

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(
        AnalyzeMethodDeclaration,
        SyntaxKind.MethodDeclaration,
        SyntaxKind.SimpleLambdaExpression,
        SyntaxKind.ParenthesizedLambdaExpression,
        SyntaxKind.AnonymousMethodExpression,
        SyntaxKind.LocalFunctionStatement
      );
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : SyntaxNodeAnalyzerBase<SyntaxNode> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(context) { }

      public override void Analyze() {
        if(!Root.IsMethodOrFunctionWithAsyncModifier()) {
          return;
        }
        foreach(var invocation in GetThreadSleepInvocations()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
        }
      }

      private IEnumerable<InvocationExpressionSyntax> GetThreadSleepInvocations() {
        return Root.DescendantNodesInSameActivationFrame()
          .WithCancellation(CancellationToken)
          .OfType<InvocationExpressionSyntax>()
          .Where(IsThreadSleepInvocation);
      }

      private bool IsThreadSleepInvocation(InvocationExpressionSyntax invocation) {
        return SemanticModel.GetSymbolInfo(invocation, CancellationToken).Symbol is IMethodSymbol methodSymbol
          && SleepMethod.Equals(methodSymbol.Name)
          && SemanticModel.IsEqualType(methodSymbol.ContainingType, ThreadType);
      }
    }
  }
}
