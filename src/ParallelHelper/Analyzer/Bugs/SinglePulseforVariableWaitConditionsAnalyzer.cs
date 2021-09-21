using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.Bugs {
  /// <summary>
  /// Analyzer that analyzes lock-statements that make use of <see cref="System.Threading.Monitor.Wait(object)"/>
  /// signaled by a <see cref="System.Threading.Monitor.Pulse(object)"/> while the enclosing loop appears 
  /// to be waiting for semantically different conditions per thread. This probability is observed by checking if the
  /// condition is dependent on a method parameter.
  /// 
  /// <example>The use of <c>Monitor.Wait</c> with an enclosed while-statement whose condition is dependant on method parameter.
  /// <code>
  /// class Sample {
  ///   private readonly object syncObject = new object();
  ///   private int count = 0;
  /// 
  ///   public void Take(int amount) {
  ///     lock(syncObject) {
  ///       while(amount > count) {
  ///         Monitor.Wait(syncObject);
  ///       }
  ///       count -= amount;
  ///     }
  ///   }
  /// 
  ///   public void Put(int amount) {
  ///     lock(syncObject) {
  ///       count += amount;
  ///       Monitor.Pulse(syncObject);
  ///     }
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class SinglePulseforVariableWaitConditionsAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_B004";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Single Pulse for Variable Wait Conditions";
    private static readonly LocalizableString MessageFormat = "It appears that at least one Monitor.Wait depends on a method parameter (chance for threads with semantically different wait conditions), Monitor.PulseAll might be more suitable here.";
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

    private class Analyzer : MonitorAwareAnalyzerWithSyntaxWalkerBase<SyntaxNode> {
      private readonly ISet<ISymbol> _syncObjectsWithParameterDependantWait = new HashSet<ISymbol>();

      public Analyzer(SemanticModelAnalysisContext context) : base(new SemanticModelAnalysisContextWrapper(context)) { }

      public override void Analyze() {
        base.Analyze();
        foreach(var pulseInvocation in GetAllMonitorPulsesThatTargetParameterDependantWaits()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, pulseInvocation.GetLocation()));
        }
      }

      public override void VisitInvocationExpression(InvocationExpressionSyntax node) {
        if(IsParameterDependantWaitAtCurrentPosition(node) && TryGetSyncObject(node, out var syncObject)) {
          _syncObjectsWithParameterDependantWait.Add(syncObject!);
        }
        base.VisitInvocationExpression(node);
      }

      private IEnumerable<InvocationExpressionSyntax> GetAllMonitorPulsesThatTargetParameterDependantWaits() {
        return Root.DescendantNodes()
          .WithCancellation(CancellationToken)
          .OfType<InvocationExpressionSyntax>()
          .Where(MonitorAnalysis.IsMonitorPulse)
          .Where(IsTargetingParameterDependantWait);
      }

      private bool IsTargetingParameterDependantWait(InvocationExpressionSyntax invocation) {
        return TryGetSyncObject(invocation, out var syncObject)
          && _syncObjectsWithParameterDependantWait.Contains(syncObject!);
      }

      private bool IsParameterDependantWaitAtCurrentPosition(InvocationExpressionSyntax node) {
        return IsInsideLoopEnclosedByLock
          && MonitorAnalysis.IsMonitorWait(node)
          && IsParameterDependant(LoopsEnclosedByLock.Peek());
      }

      private bool IsParameterDependant(SyntaxNode syntax) {
        return syntax.DescendantNodes()
          .WithCancellation(CancellationToken)
          .OfType<IdentifierNameSyntax>()
          .Select(i => SemanticModel.GetSymbolInfo(i, CancellationToken).Symbol)
          .OfType<IParameterSymbol>()
          .Any();
      }

      private bool TryGetSyncObject(InvocationExpressionSyntax invocation, out ISymbol? syncObject) {
        var arguments = invocation.ArgumentList.Arguments;
        if(arguments.Count == 0) {
          syncObject = null;
          return false;
        }
        syncObject = SemanticModel.GetSymbolInfo(arguments[0].Expression, CancellationToken).Symbol;
        return syncObject != null;
      }
    }
  }
}
