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
  /// Analyzer that analyzes sources for the use of lock statements that change the synchronization object.
  /// 
  /// <example>Illustrates a class with a lock statement changing the shared object which is also used as a synchronization object.
  /// <code>
  /// class Sample {
  ///   private object shared = new shared();
  ///   
  ///   public void DoWork() {
  ///     lock(shared) {
  ///       // ...
  ///       shared = new object();
  ///     }
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class MonitorSyncObjectChangeAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S003";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "SyncObject Change";
    private static readonly LocalizableString MessageFormat = "Changing the synchronization object '{0}' inside the monitor lock is discouraged.";
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
      public Analyzer(SemanticModelAnalysisContext context) : base(new SemanticModelAnalysisContextWrapper(context)) { }

      public override void Analyze() {
        foreach(var statement in GetLockStatementsInside(Root)) {
          AnalyzeLockStatement(statement);
        }
      }

      private void AnalyzeLockStatement(LockStatementSyntax lockStatement) {
        var syncObject = GetSyncObject(lockStatement);
        if(syncObject == null) {
          return;
        }

        var assignment = TryGetFirstAssignmentToInside(syncObject, lockStatement);
        if(assignment != null) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, assignment.GetLocation(), syncObject.Name));
        }
      }

      private IFieldSymbol? GetSyncObject(LockStatementSyntax lockStatement) {
        return SemanticModel.GetSymbolInfo(lockStatement.Expression, CancellationToken).Symbol as IFieldSymbol;
      }

      private IEnumerable<LockStatementSyntax> GetLockStatementsInside(SyntaxNode root) {
        return root.DescendantNodes().WithCancellation(CancellationToken).OfType<LockStatementSyntax>();
      }

      private AssignmentExpressionSyntax? TryGetFirstAssignmentToInside(IFieldSymbol syncObject, SyntaxNode root) {
        // TODO follow the control flow through methods but prevent cycles.
        return root.DescendantNodes()
          .WithCancellation(CancellationToken)
          .OfType<AssignmentExpressionSyntax>()
          .Where(assignment => syncObject.Equals(SemanticModel.GetSymbolInfo(assignment.Left, CancellationToken).Symbol))
          .FirstOrDefault();
      }
    }
  }
}
