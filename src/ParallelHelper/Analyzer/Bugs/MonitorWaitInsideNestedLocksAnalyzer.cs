using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.Bugs {
  /// <summary>
  /// Analyzer that analyzes sources for <see cref="System.Threading.Monitor.Wait(object)"/> invocations that
  /// occur inside nested lock-statements.
  /// 
  /// <example>The invocation of <c>Monitor.Wait</c> inside two nested lock-statements.
  /// <code>
  /// class Sample {
  ///   private readonly object syncObject1 = new object();
  ///   private readonly object syncObject2 = new object();
  /// 
  ///   public void DoWork() {
  ///     lock(syncObject1) {
  ///       lock(syncObject2) {
  ///         Monitor.Wait(syncObject2);
  ///       }
  ///     }
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class MonitorWaitInsideNestedLocksAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_B002";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Monitor.Wait inside Nested Locks";
    private static readonly LocalizableString MessageFormat = "The use of Monitor.Wait inside nested lock-statements is discouraged since it only releases the lock on the provided synchronization object.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSemanticModelAction(AnalyzeSemanticModel);
    }

    private static void AnalyzeSemanticModel(SemanticModelAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : MonitorAwareSemanticModelAnalyzerWithSyntaxWalkerBase {
      public Analyzer(SemanticModelAnalysisContext context) : base(context) { }

      public override void VisitInvocationExpression(InvocationExpressionSyntax node) {
        if(LockDepth > 1 && AtLeastTwoEnclosingLocksUseDistinctSyncObjects() && MonitorAnalysis.IsMonitorWait(node)) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation()));
        }
        base.VisitInvocationExpression(node);
      }

      private bool AtLeastTwoEnclosingLocksUseDistinctSyncObjects() {
        return EnclosingLocks.WithCancellation(CancellationToken)
          .Select(l => l.Expression)
          .Select(s => SemanticModel.GetSymbolInfo(s, CancellationToken).Symbol)
          .IsNotNull()
          .Distinct()
          .Count() > 1;
      }
    }
  }
}
