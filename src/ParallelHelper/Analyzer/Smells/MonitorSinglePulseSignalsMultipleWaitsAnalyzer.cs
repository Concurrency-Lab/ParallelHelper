using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.Smells {
  /// <summary>
  /// Analyzer that analyzes lock-statements that send a single signal using <see cref="System.Threading.Monitor.Pulse(object)"/>
  /// that could be received by multiple <see cref="System.Threading.Monitor.Wait(object)"/> invocations.
  /// 
  /// <example>Multiple <c>Monitor.Wait</c> invocations targetted by <c>Monitor.Pulse</c>.
  /// <code>
  /// class Sample {
  ///   private readonly object syncObject = new object();
  ///   private const int max = 10;
  ///   private int count = 0;
  /// 
  ///   public void Take() {
  ///     lock(syncObject) {
  ///       while(count > 0) {
  ///         Monitor.Wait(syncObject);
  ///       }
  ///       count--;
  ///       Monitor.Pulse();
  ///     }
  ///   }
  /// 
  ///   public void Put() {
  ///     lock(syncObject) {
  ///       while(count >= max) {
  ///         Monitor.Wait(syncObject);
  ///       }
  ///       count++;
  ///       Monitor.Pulse();
  ///     }
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class MonitorSinglePulseSignalsMultipleWaitsAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S016";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Monitor.Pulse with multiple Monitor.Wait";
    private static readonly LocalizableString MessageFormat = "Usually, it is necessary to invoke Monitor.PulseAll if there are multiple Monitor.Wait invocations.";
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

    private class Analyzer : InternalAnalyzerBase<SyntaxNode> {
      private readonly MonitorAnalysis _monitorAnalysis;

      public Analyzer(SemanticModelAnalysisContext context) : base(new SemanticModelAnalysisContextWrapper(context)) {
        _monitorAnalysis = new MonitorAnalysis(SemanticModel, CancellationToken);
      }

      public override void Analyze() {
        var syncObjectsWithMultipleWaits = GetAllSyncObjectsWithMultipleMonitorWaitInvocations();
        foreach(var pulseInvocations in GetAllMonitorPulseInvocationsWithAnySyncObjectIn(syncObjectsWithMultipleWaits)) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, pulseInvocations.GetLocation()));
        }
      }

      private ISet<ISymbol> GetAllSyncObjectsWithMultipleMonitorWaitInvocations() {
        return Root.DescendantNodesAndSelf()
          .WithCancellation(CancellationToken)
          .OfType<InvocationExpressionSyntax>()
          .Where(i => i.ArgumentList.Arguments.Count > 0)
          .Where(_monitorAnalysis.IsMonitorWait)
          .Select(TryGetSyncObject)
          .IsNotNull()
          .GroupBy(s => s)
          .Where(g => g.Count() > 1)
          .Select(g => g.Key)
          .ToImmutableHashSet();
      }

      private IEnumerable<InvocationExpressionSyntax> GetAllMonitorPulseInvocationsWithAnySyncObjectIn(ISet<ISymbol> syncObjects) {
        return Root.DescendantNodesAndSelf()
          .WithCancellation(CancellationToken)
          .OfType<InvocationExpressionSyntax>()
          .Where(i => i.ArgumentList.Arguments.Count > 0)
          .Where(_monitorAnalysis.IsMonitorPulse)
          .Where(i => IsSyncObjectOfInvocationInside(i, syncObjects));
      }

      private ISymbol? TryGetSyncObject(InvocationExpressionSyntax invocation) {
        return SemanticModel.GetSymbolInfo(invocation.ArgumentList.Arguments[0].Expression, CancellationToken).Symbol;
      }

      private bool IsSyncObjectOfInvocationInside(InvocationExpressionSyntax invocation, ISet<ISymbol> syncObjects) {
        var syncObject = TryGetSyncObject(invocation);
        return syncObject != null && syncObjects.Contains(syncObject);
      }
    }
  }
}