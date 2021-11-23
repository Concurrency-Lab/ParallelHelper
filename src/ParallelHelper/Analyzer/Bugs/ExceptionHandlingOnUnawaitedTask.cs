using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ParallelHelper.Analyzer.Bugs {
  /// <summary>
  /// Analyzer that analyzes sources that return tasks enclosed by try-statements without awating them.
  /// 
  /// <example>Illustrates a class with a method that directly returns a task whose exceptions should be handled by the enclosing try-statement.
  /// <code>
  /// class Sample {
  ///   public Task DoWorkAsync() 
  ///     try {
  ///       return DoWorkInternalAsync();
  ///     } catch(Exception e) {
  ///       // error
  ///     }
  ///   }
  ///   
  ///   private async Task DoWorkInternalAsync() {
  ///     // do work that may fail
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class ExceptionHandlingOnUnawaitedTask : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_B016";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Exception handling on unawaited Task";
    private static readonly LocalizableString MessageFormat = "The task is not awaited; thus, the surrounding exception handling may be ineffective.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSemanticModelAction(AnalyzeExpressionStatement);
    }

    private static void AnalyzeExpressionStatement(SemanticModelAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerWithSyntaxWalkerBase<SyntaxNode> {
      private readonly TaskAnalysis _taskAnalysis;

      private Stack<int> _tryStatementsPerActivationFrame = new Stack<int>();
      private int _enclosingTryStatementsOfCurrentActivationFrame;

      private bool IsEnclosedByTryStatement => _enclosingTryStatementsOfCurrentActivationFrame > 0;

      public Analyzer(SemanticModelAnalysisContext context) : base(new SemanticModelAnalysisContextWrapper(context)) {
        _taskAnalysis = new TaskAnalysis(context.SemanticModel, context.CancellationToken);
      }

      public override void Visit(SyntaxNode node) {
        if(!node.IsNewActivationFrame()) {
          base.Visit(node);
          return;
        }
        _tryStatementsPerActivationFrame.Push(_enclosingTryStatementsOfCurrentActivationFrame);
        _enclosingTryStatementsOfCurrentActivationFrame = 0;
        base.Visit(node);
        _enclosingTryStatementsOfCurrentActivationFrame = _tryStatementsPerActivationFrame.Pop();
      }

      public override void VisitTryStatement(TryStatementSyntax node) {
        ++_enclosingTryStatementsOfCurrentActivationFrame;
        node.Block.Accept(this);
        --_enclosingTryStatementsOfCurrentActivationFrame;
        foreach(var catchClause in node.Catches) {
          catchClause.Accept(this);
        }
        node.Finally?.Accept(this);
      }

      public override void VisitReturnStatement(ReturnStatementSyntax node) {
        if(node.Expression == null || !IsEnclosedByTryStatement) {
          base.VisitReturnStatement(node);
          return;
        }
        if(IsInvocationOfTaskReturningMethod(node)) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, node.Expression.GetLocation()));
        }
      }

      private bool IsInvocationOfTaskReturningMethod(ReturnStatementSyntax node) {
        return node.Expression is InvocationExpressionSyntax
          && _taskAnalysis.IsTaskTyped(node.Expression);
      }
    }
  }
}
