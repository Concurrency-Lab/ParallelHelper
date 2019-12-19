using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.Bugs {
  /// <summary>
  /// Analyzer that analyzes that the conditional loop enclosed by <see cref="System.Threading.Monitor.Wait(object)"/>
  /// is affected by the changes applied in the lock-statement enclosing a signaling <see cref="System.Threading.Monitor.Pulse(object)"/>
  /// or <see cref="System.Threading.Monitor.PulseAll(object)"/>.
  /// 
  /// <example>The use of <c>Monitor.Wait</c> with an enclosed while-statement whose condition is not affected by the signaling pulse.
  /// <code>
  /// class Sample {
  ///   private readonly object syncObject = new object();
  ///   private int count = 0;
  /// 
  ///   public void Take() {
  ///     lock(syncObject) {
  ///       while(count == 0) {
  ///         Monitor.Wait(syncObject);
  ///       }
  ///       count--;
  ///     }
  ///   }
  /// 
  ///   public void Put(int amount) {
  ///     lock(syncObject) {
  ///       Monitor.Pulse(syncObject);
  ///     }
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class MonitorWaitConditionUnaffectedAnalyzer : DiagnosticAnalyzer {
    // TODO Find waits that are not affected by a pulse (there might be multiple and possibly only one of them is necessary).
    //      Alternatively, report Pulse(All) that do not appear to affect the conditional loop of the affected Waits.
    // TODO Check method-invocations and properties.

    // Possible changes through:
    //   - Assignments
    //   - ref/out
    //   - Method invocations (side-effects on fields/properties)
    //   - object's member is accessed
    // Best is: Keep it simple and only analyze conditional loops that solely use fields of the current object without member accesses.
    public const string DiagnosticId = "PH_B005";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Monitor Signal Without Conditional Loop Effect";
    private static readonly LocalizableString MessageFormat = "The signaled changes in the enclosing lock-statement do not have an effect on the conditional loop of the Monitor.Wait on line {0}.";
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
      // TODO support modifications of array elements?
      private readonly IDictionary<ISymbol, IDictionary<InvocationExpressionSyntax, ISet<IFieldSymbol>>> _monitoredFieldsPerSyncObject
        = new Dictionary<ISymbol, IDictionary<InvocationExpressionSyntax, ISet<IFieldSymbol>>>();

      private readonly IDictionary<LockStatementSyntax, ISet<InvocationExpressionSyntax>> _pulsesPerLock
        = new Dictionary<LockStatementSyntax, ISet<InvocationExpressionSyntax>>();
      private readonly IDictionary<LockStatementSyntax, ISet<IFieldSymbol>> _fieldChangesPerLock
        = new Dictionary<LockStatementSyntax, ISet<IFieldSymbol>>();

      public Analyzer(SemanticModelAnalysisContext context) : base(context) { }

      public override void Analyze() {
        base.Analyze();
        foreach(var (pulseInvocation, waitInvocation) in _pulsesPerLock.Keys.SelectMany(GetPulseInvocationsWithoutEffectOnWait)) {
          var waitLine = waitInvocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
          Context.ReportDiagnostic(Diagnostic.Create(Rule, pulseInvocation.GetLocation(), waitLine));
        }
      }

      private IEnumerable<(InvocationExpressionSyntax Pulse, InvocationExpressionSyntax SignaledWait)> GetPulseInvocationsWithoutEffectOnWait(
          LockStatementSyntax lockStatement) {
        var changedFields = GetAllChangedFields(lockStatement);
        return _pulsesPerLock[lockStatement]
          .WithCancellation(CancellationToken)
          .Select(pulse => new { Pulse = pulse, SignaledWaits = GetByPulseSignaledWaits(pulse) })
          .SelectMany(e => e.SignaledWaits.Select(signaledWait => new { e.Pulse, SignaledWait = signaledWait.Key, MonitoredFields = signaledWait.Value }))
          .Where(e => !e.MonitoredFields.Any(changedFields.Contains))
          .Select(e => (e.Pulse, e.SignaledWait));
      }

      private IDictionary<InvocationExpressionSyntax, ISet<IFieldSymbol>> GetByPulseSignaledWaits(InvocationExpressionSyntax pulseInvocation) {
        if(MonitorAnalysis.TryGetSyncObjectFromMonitorMethod(pulseInvocation, out var syncObject)
            && _monitoredFieldsPerSyncObject.TryGetValue(syncObject!, out var monitoredFieldsPerWait)) {
          return monitoredFieldsPerWait;
        }
        return new Dictionary<InvocationExpressionSyntax, ISet<IFieldSymbol>>();
      }

      private ISet<IFieldSymbol> GetAllChangedFields(LockStatementSyntax lockStatement) {
        return _fieldChangesPerLock.TryGetValue(lockStatement, out var changes) ? changes : new HashSet<IFieldSymbol>();
      }

      public override void VisitInvocationExpression(InvocationExpressionSyntax node) {
        if(MonitorAnalysis.IsMonitorPulse(node) || MonitorAnalysis.IsMonitorPulseAll(node)) {
          TrackMonitorPulseInsideLock(node);
        } else if(MonitorAnalysis.IsMonitorWait(node) && MonitorAnalysis.TryGetSyncObjectFromMonitorMethod(node, out var syncObject)) {
          TrackAllMonitoredFields(syncObject!, node);
        }
        base.VisitInvocationExpression(node);
      }

      private void TrackAllMonitoredFields(ISymbol syncObject, InvocationExpressionSyntax waitInvocation) {
        var monitoredFields = GetAllConditionalLoopMonitoredFieldsIfNoOtherIdentifiersUsed();
        if(monitoredFields == null) {
          return;
        }
        IDictionary<InvocationExpressionSyntax, ISet<IFieldSymbol>> monitoredFieldsPerWait;
        if(!_monitoredFieldsPerSyncObject.TryGetValue(syncObject, out monitoredFieldsPerWait)) {
          monitoredFieldsPerWait = new Dictionary<InvocationExpressionSyntax, ISet<IFieldSymbol>>();
          _monitoredFieldsPerSyncObject.Add(syncObject, monitoredFieldsPerWait);
        }
        monitoredFieldsPerWait.Add(waitInvocation, monitoredFields);
      }

      public override void VisitArgument(ArgumentSyntax node) {
        if(IsArgumentWithSideEffects(node)) {
          TryTrackWrittenFieldInsideLock(node.Expression);
        }
        base.VisitArgument(node);
      }

      public override void VisitAssignmentExpression(AssignmentExpressionSyntax node) {
        TryTrackWrittenFieldInsideLock(node.Left);
        base.VisitAssignmentExpression(node);
      }

      public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node) {
        if(IsWritingUnaryOperator(node.OperatorToken)) {
          TryTrackWrittenFieldInsideLock(node.Operand);
        }
        base.VisitPostfixUnaryExpression(node);
      }

      public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node) {
        if(IsWritingUnaryOperator(node.OperatorToken)) {
          TryTrackWrittenFieldInsideLock(node.Operand);
        }
        base.VisitPrefixUnaryExpression(node);
      }

      private static bool IsWritingUnaryOperator(SyntaxToken token) {
        return token.IsKind(SyntaxKind.PlusPlusToken) || token.IsKind(SyntaxKind.MinusMinusToken);
      }

      private void TryTrackWrittenFieldInsideLock(ExpressionSyntax expression) {
        if(IsInsideLock && TryGetObjectInternalField(expression, out var field)) {
          TrackWrittenFieldInsideLock(field!);
        }
      }

      private bool IsArgumentWithSideEffects(ArgumentSyntax argument) {
        return argument.RefKindKeyword.IsKind(SyntaxKind.RefKeyword) || argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword);
      }

      private void TrackWrittenFieldInsideLock(IFieldSymbol field) {
        TrackInCurrentLockDictionary(_fieldChangesPerLock, field);
      }

      private void TrackMonitorPulseInsideLock(InvocationExpressionSyntax invocation) {
        TrackInCurrentLockDictionary(_pulsesPerLock, invocation);
      }

      private void TrackInCurrentLockDictionary<TValue>(IDictionary<LockStatementSyntax, ISet<TValue>> dictionary, TValue value) {
        if(!IsInsideLock) {
          return;
        }
        ISet<TValue> valuesOfCurrentLock;
        var currentLock = EnclosingLocks.Peek();
        if(!dictionary.TryGetValue(currentLock, out valuesOfCurrentLock)) {
          valuesOfCurrentLock = new HashSet<TValue>();
          dictionary.Add(currentLock, valuesOfCurrentLock);
        }
        valuesOfCurrentLock.Add(value);
      }

      private bool TryGetObjectInternalField(ExpressionSyntax expression, out IFieldSymbol? field) {
        var identifier = expression as SimpleNameSyntax;
        if(identifier == null && expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Expression is ThisExpressionSyntax) {
          identifier = memberAccess.Name;
        }
        if(identifier == null) {
          field = null;
          return false;
        }
        field = SemanticModel.GetSymbolInfo(identifier, CancellationToken).Symbol as IFieldSymbol;
        return field != null;
      }

      private ISet<IFieldSymbol>? GetAllConditionalLoopMonitoredFieldsIfNoOtherIdentifiersUsed() {
        if(!IsInsideLoopEnclosedByLock) {
          return null;
        }

        var condition = LoopsEnclosedByLock.Peek().Condition;
        if(ContainsMemberOfForeignObject(condition)) {
          return null;
        }

        var identifierSymbolsInCondition = GetAllIdentifierSymbolsInside(condition);
        if(!ContainsOnlyFields(identifierSymbolsInCondition)) {
          return null;
        }
        return identifierSymbolsInCondition.Cast<IFieldSymbol>().ToImmutableHashSet();
      }

      private static bool ContainsOnlyFields(ISet<ISymbol?> symbols) {
        return !symbols.Any(symbol => symbol == null || !(symbol is IFieldSymbol));
      }

      private bool ContainsMemberOfForeignObject(ExpressionSyntax expression) {
        return expression.DescendantNodesAndSelf()
          .WithCancellation(CancellationToken)
          .OfType<MemberAccessExpressionSyntax>()
          .Any(memberAccess => !(memberAccess.Expression is ThisExpressionSyntax));
      }

      private ISet<ISymbol?> GetAllIdentifierSymbolsInside(ExpressionSyntax expression) {
        // The symbol retrieval may return null.
        // This is probably obsolete when upgrading to a more recent roslyn version.
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        return expression.DescendantNodesAndSelf()
          .WithCancellation(CancellationToken)
          .OfType<IdentifierNameSyntax>()
          .Select(identifier => SemanticModel.GetSymbolInfo(identifier, CancellationToken).Symbol)
          .ToImmutableHashSet();
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
      }
    }
  }
}
