using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.BestPractices {
  /// <summary>
  /// Analyzer that analyzes sources for the use locks that access fields that are not readonly.
  /// 
  /// <example>Illustrates a class with a method that uses a member field for the synchronization which is not marked readonly.
  /// <code>
  /// class Sample {
  ///   private object syncObject = new object();
  ///   
  ///   public void DoWork() {
  ///     lock(syncObject) {
  ///     }
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class MonitorReadonlySyncObjectAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_P001";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Writeable SyncObject";
    private static readonly LocalizableString MessageFormat = "The field '{0}' that references the monitor's synchronization object is not readonly.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info,
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
      public Analyzer(SemanticModelAnalysisContext context) : base(new SemanticModelAnalysisContextWrapper(context)) { }

      public override void Analyze() {
        foreach(var statement in GetLockStatementsInside(Root)) {
          AnalyzeLockStatement(statement);
        }
      }

      private void AnalyzeLockStatement(LockStatementSyntax lockStatement) {
        var syncObject = GetSyncObject(lockStatement);
        if(syncObject != null && !syncObject.IsReadOnly) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, lockStatement.Expression.GetLocation(), syncObject.Name));
        }
      }

      private IFieldSymbol? GetSyncObject(LockStatementSyntax lockStatement) {
        return SemanticModel.GetSymbolInfo(lockStatement.Expression, CancellationToken).Symbol as IFieldSymbol;
      }

      private IEnumerable<LockStatementSyntax> GetLockStatementsInside(SyntaxNode root) {
        return root.DescendantNodes().WithCancellation(CancellationToken).OfType<LockStatementSyntax>();
      }
    }
  }
}
