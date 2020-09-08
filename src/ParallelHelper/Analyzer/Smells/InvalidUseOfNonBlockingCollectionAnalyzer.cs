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
  /// Analyzer that analyzes sources for the use of non-blocking collections where a blocking collection would be appropriate.
  /// 
  /// <example>Illustrates a class with a method that erroneously uses a non-blocking collection.
  /// <code>
  /// class Sample {
  ///   private readonly ConcurrentQueue&lt;T&gt; _queue = new ConcurrentQueue&lt;T&gt;();
  ///   public void DoWork() {
  ///     while(true) {
  ///       if(!_queue.TryDequeue(out var item)) {
  ///         // Often a thread sleep is used to reduce the cpu pressure.
  ///         Thread.Sleep(100);
  ///         continue;
  ///       }
  ///       // do work...
  ///     }
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class InvalidUseOfNonBlockingCollectionAnalyzer : DiagnosticAnalyzer {
    // TODO support if/else besides if->continue.
    public const string DiagnosticId = "PH_S011";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Invalid use of Non-Blocking Collection";
    private static readonly LocalizableString MessageFormat = "The use of a blocking collection instead of a non-blocking appears to be more suitable here.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public static readonly string[] ConcurrentCollectionTypes = {
      "System.Collections.Concurrent.ConcurrentBag`1",
      "System.Collections.Concurrent.ConcurrentQueue`1",
      "System.Collections.Concurrent.ConcurrentStack`1"
    };

    public static readonly string[] NonBlockingConsumptionMethods = {
      "TryTake", "TryDequeue"
    };

    public static string ThreadType = "System.Threading.Thread";
    public static string SleepMethod = "Sleep";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeExpressionStatement, SyntaxKind.WhileStatement);
    }

    private static void AnalyzeExpressionStatement(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerBase<WhileStatementSyntax> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper<WhileStatementSyntax>(context)) { }

      public override void Analyze() {
        foreach(var ifStatement in GetAllSleepingAndContinuingIfStatementsInLoop()) {
          if(GetAllNonBlockingConsumptionsInside(ifStatement.Condition).Any()) {
            Context.ReportDiagnostic(Diagnostic.Create(Rule, ifStatement.GetLocation()));
          }
        }
      }

      private IEnumerable<IfStatementSyntax> GetAllSleepingAndContinuingIfStatementsInLoop() {
        // TODO ensure that the continue statement does not belong to another loop.
        return Root.Statement.DescendantNodesAndSelf()
          .WithCancellation(CancellationToken)
          .OfType<IfStatementSyntax>()
          .Where(ContainsContinueStatement)
          .Where(ContainsThreadSleep);
      }

      private bool ContainsContinueStatement(SyntaxNode node) {
        return node.DescendantNodes()
          .WithCancellation(CancellationToken)
          .OfType<ContinueStatementSyntax>()
          .Any();
      }

      private bool ContainsThreadSleep(SyntaxNode node) {
        return node.DescendantNodes()
          .WithCancellation(CancellationToken)
          .OfType<InvocationExpressionSyntax>()
          .Any(IsThreadSleep);
      }

      private bool IsThreadSleep(InvocationExpressionSyntax invocation) {
        return SemanticModel.GetSymbolInfo(invocation, CancellationToken).Symbol is IMethodSymbol methodSymbol
          && SleepMethod.Equals(methodSymbol.Name)
          && SemanticModel.IsEqualType(methodSymbol.ContainingType, ThreadType);
      }

      private IEnumerable<InvocationExpressionSyntax> GetAllNonBlockingConsumptionsInside(SyntaxNode node) {
        return node.DescendantNodesAndSelf()
          .WithCancellation(CancellationToken)
          .OfType<InvocationExpressionSyntax>()
          .Where(IsNonBlockingConsumption);
      }

      private bool IsNonBlockingConsumption(InvocationExpressionSyntax invocation) {
        return SemanticModel.GetSymbolInfo(invocation, CancellationToken).Symbol is IMethodSymbol methodSymbol
          && NonBlockingConsumptionMethods.Any(m => m.Equals(methodSymbol.Name))
          && ConcurrentCollectionTypes.Any(t => SemanticModel.IsEqualType(methodSymbol.ContainingType, t));
      }
    }
  }
}
