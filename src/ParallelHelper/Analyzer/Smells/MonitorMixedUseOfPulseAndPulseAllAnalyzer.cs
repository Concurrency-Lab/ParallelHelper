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
  /// Analyzer that analyzes lock-statements that combine the use of <see cref="System.Threading.Monitor.Pulse(object)"/>
  /// and <see cref="System.Threading.Monitor.PulseAll(object)"/> for the same synchronization object.
  /// 
  /// <example>The combined use of <c>Monitor.Pulse</c> and <c>Monitor.PulseAll</c> with the same synchronization object.
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
  ///       Monitor.PulseAll();
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
  public class MonitorMixedUseOfPulseAndPulseAllAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S017";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Monitor.Pulse is used in combination with Monitor.PulseAll";
    private static readonly LocalizableString MessageFormat = "The combined use of Monitor.Pulse in combination with Monitor.PulseAll with the same synchronization object indicates an error.";
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

    private class Analyzer : SemanticModelAnalyzerBase {
      private readonly MonitorAnalysis _monitorAnalysis;

      public Analyzer(SemanticModelAnalysisContext context) : base(context) {
        _monitorAnalysis = new MonitorAnalysis(SemanticModel, CancellationToken);
      }

      public override void Analyze() {
        var syncObjectsWithPulseAll = GetAllSyncObjectsWithPulseAllInvocations();
        foreach(var pulseInvocations in GetAllMonitorPulseInvocationsWithAnySyncObjectIn(syncObjectsWithPulseAll)) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, pulseInvocations.GetLocation()));
        }
      }

      private ISet<ISymbol> GetAllSyncObjectsWithPulseAllInvocations() {
        return Root.DescendantNodesAndSelf()
          .WithCancellation(CancellationToken)
          .OfType<InvocationExpressionSyntax>()
          .Where(i => i.ArgumentList.Arguments.Count > 0)
          .Where(_monitorAnalysis.IsMonitorPulseAll)
          .Select(TryGetSyncObject)
          .IsNotNull()
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