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
  /// Analyzer that analyzes the source for classes that incorporate only a partial monitor synchornization, i.e. blocks
  /// that are missing the lock-statement when accessing a single field multiple times.
  /// 
  /// <example>A class with a method that locks prior field access and another that doesn't.
  /// <code>
  /// class Sample {
  ///   private readonly object syncObject = new object();
  ///   private int count = 0;
  /// 
  ///   public bool TryTake(int amount) {
  ///     if(amount &lt;= count) {
  ///       count -= amount;
  ///     }
  ///     return false;
  ///   }
  /// 
  ///   public void Put(int amount) {
  ///     lock(syncObject) {
  ///       count += amount;
  ///     }
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class MissingMonitorLockOnSingleFieldAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_B010";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Missing Monitor Synchronization";
    private static readonly LocalizableString MessageFormat = "The access to the field '{0}' is probably missing an enclosing lock-statement for synchronization.";
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
      new Analyzer(context, GetAllFields(context)).Analyze();
    }

    private static ISet<IFieldSymbol> GetAllFields(SemanticModelAnalysisContext context) {
      var semanticModel = context.SemanticModel;
      var cancellationToken = context.CancellationToken;
      return semanticModel.SyntaxTree.GetRoot(cancellationToken)
        .DescendantNodesAndSelf()
        .WithCancellation(cancellationToken)
        .OfType<FieldDeclarationSyntax>()
        .SelectMany(declaration => declaration.Declaration.Variables)
        .Select(variable => (IFieldSymbol)semanticModel.GetDeclaredSymbol(variable, cancellationToken))
        .IsNotNull()
        .ToImmutableHashSet();
    }

    private class Analyzer : FieldAccessAwareSemanticModelAnalyzerWithSyntaxWalkerBase {
      public Analyzer(SemanticModelAnalysisContext context, ISet<IFieldSymbol> declaredFields) : base(context, declaredFields) { }

      public override void Analyze() {
        base.Analyze();
        foreach(var (field, location) in GetAllFieldAccessesToReport()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, location, field.Name));
        }
      }

      private IEnumerable<(IFieldSymbol Field, Location Location)> GetAllFieldAccessesToReport() {
        return GetAllUnsynchronizedAccessesToFieldsWithMultipleAccessesInsideSingleScopesOutsideLockToFieldsWrittenInsideLock()
          .Concat(GetAllUnsynchronizedAccessesToFieldsWithMultipleAccessesInsideSingleLockToFieldsWrittenOutsideLock())
          .Select(access => (access.Field, Location : access.Access.GetLocation()))
          .Distinct();
      }

      private IEnumerable<FieldAccess> GetAllUnsynchronizedAccessesToFieldsWithMultipleAccessesInsideSingleScopesOutsideLockToFieldsWrittenInsideLock() {
        var accesses = GetAllFieldAccessesWithMultipleAccessesInsideSameScopeAndOutsideLock();
        return GetAllFieldsWrittenInsideLock()
          .Where(accesses.ContainsKey)
          .SelectMany(field => accesses[field]);
      }

      private IEnumerable<FieldAccess> GetAllUnsynchronizedAccessesToFieldsWithMultipleAccessesInsideSingleLockToFieldsWrittenOutsideLock() {
        var accesses = GetAllWriteAccessesOutsideLock();
        return GetAllFieldsWithMultipleAccessesToSingleFieldInsideSingleLock()
          .Where(accesses.ContainsKey)
          .SelectMany(field => accesses[field]);
      }

      private IDictionary<IFieldSymbol, ISet<FieldAccess>> GetAllFieldAccessesWithMultipleAccessesInsideSameScopeAndOutsideLock() {
        return FieldAccesses
          .WithCancellation(CancellationToken)
          .Where(access => !access.IsInsideLock)
          .GroupBy(access => (access.Field, access.EnclosingScope))
          .Where(accessPerFieldAndScope => accessPerFieldAndScope.Count() >= 2)
          .SelectMany(accessPerFieldAndScope => accessPerFieldAndScope)
          .GroupBy(access => access.Field)
          .ToImmutableDictionary(
            accessPerField => accessPerField.Key,
            accessPerField => (ISet<FieldAccess>)accessPerField.ToImmutableHashSet()
          );
      }

      private IDictionary<IFieldSymbol, ISet<FieldAccess>> GetAllWriteAccessesOutsideLock() {
        return FieldAccesses
          .WithCancellation(CancellationToken)
          .Where(access => !access.IsInsideLock && access.IsWriting)
          .GroupBy(access => access.Field)
          .ToImmutableDictionary(
            accessPerField => accessPerField.Key,
            accessPerField => (ISet<FieldAccess>)accessPerField.ToImmutableHashSet()
          );
      }

      private ISet<IFieldSymbol> GetAllFieldsWrittenInsideLock() {
        return FieldAccesses
          .WithCancellation(CancellationToken)
          .Where(access => access.IsWriting && access.IsInsideLock)
          .Select(access => access.Field)
          .ToImmutableHashSet();
      }

      private ISet<IFieldSymbol> GetAllFieldsWithMultipleAccessesToSingleFieldInsideSingleLock() {
        return FieldAccesses
          .WithCancellation(CancellationToken)
          .Where(access => access.IsInsideLock)
          .GroupBy(access => (access.Field, access.EnclosingLock))
          .Where(accessesPerField => accessesPerField.Count() >= 2)
          .SelectMany(accessesPerField => accessesPerField)
          .Select(access => access.Field)
          .ToImmutableHashSet();
      }
    }
  }
}
