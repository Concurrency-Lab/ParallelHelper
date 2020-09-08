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
  /// Analyzer that analyzes sources for the use of nested locks to identify the chance of a deadlock.
  /// 
  /// <example>Illustrates a class with a method that receives an object of the same type and acquires nested locks.
  /// <code>
  /// class Sample {
  ///   private readonly object syncObject = new object();
  ///   
  ///   public void DoWork(Sample other) {
  ///     lock(syncObject) {
  ///       lock(other.syncObject) {
  ///         // ...
  ///       }
  ///     }
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class MonitorDeadlockAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_B001";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Possible Deadlock";
    private static readonly LocalizableString MessageFormat = "The nested locks may lead to a deadlock.";
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
        if(syncObject != null && CanDeadlock(lockStatement, syncObject)) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, lockStatement.GetLocation()));
        }
      }

      public bool CanDeadlock(LockStatementSyntax outerLockStatement, IFieldSymbol outerSyncObject) {
        foreach(var statement in GetImplicitelyNestedLockStatements(outerLockStatement)) {
          var syncObject = GetSyncObject(statement);
          if(syncObject != null && CanDeadlock(outerLockStatement, outerSyncObject, statement, syncObject)) {
            return true;
          }
        }
        return false;
      }

      public bool CanDeadlock(LockStatementSyntax outerLockStatement, IFieldSymbol outerSyncObject, LockStatementSyntax innerLockStatement, IFieldSymbol innerSyncObject) {
        return outerSyncObject.Equals(innerSyncObject) && UseDifferentInstances(outerLockStatement, innerLockStatement);
      }

      private IFieldSymbol? GetSyncObject(LockStatementSyntax lockStatement) {
        return SemanticModel.GetSymbolInfo(lockStatement.Expression, CancellationToken).Symbol as IFieldSymbol;
      }

      private IEnumerable<LockStatementSyntax> GetLockStatementsInside(SyntaxNode root) {
        return root.DescendantNodes().WithCancellation(CancellationToken).OfType<LockStatementSyntax>();
      }

      private IEnumerable<LockStatementSyntax> GetImplicitelyNestedLockStatements(SyntaxNode root) {
        // TODO follow the control flow through methods but prevent cycles.
        return GetLockStatementsInside(root);
      }

      private bool UseDifferentInstances(LockStatementSyntax outerLockStatement, LockStatementSyntax innerLockStatement) {
        var symbols = new [] {
          GetRootMemberSymbol(outerLockStatement.Expression),
          GetRootMemberSymbol(innerLockStatement.Expression)
        };
        return symbols.All(symbol => symbol != null) && symbols.Count(symbol => symbol is IParameterSymbol) == 1;
      }

      private ISymbol? GetRootMemberSymbol(ExpressionSyntax expression) {
        var node = expression;
        var completed = false;
        while(!completed) {
          CancellationToken.ThrowIfCancellationRequested();
          switch(node) {
          case MemberAccessExpressionSyntax memberAccess:
            node = memberAccess.Expression;
            break;
          case ParenthesizedExpressionSyntax parenthesized:
            node = parenthesized.Expression;
            break;
          default:
            completed = true;
            break;
          }
        }
        return SemanticModel.GetSymbolInfo(node, CancellationToken).Symbol;
      }
    }
  }
}
