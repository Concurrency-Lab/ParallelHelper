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
      context.RegisterSyntaxNodeAction(AnalyzeTryStatement, SyntaxKind.TryStatement);
    }

    private static void AnalyzeTryStatement(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerBase<TryStatementSyntax> {
      private readonly TaskAnalysis _taskAnalysis;

      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper(context)) {
        _taskAnalysis = new TaskAnalysis(context.SemanticModel, context.CancellationToken);
      }

      public override void Analyze() {
        foreach(var invocation in GetAllImmediatelyReturnedTaskTypedInvocations()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
        }
      }

      private IEnumerable<InvocationExpressionSyntax> GetAllImmediatelyReturnedTaskTypedInvocations() {
        return Root.Block.DescendantNodesInSameActivationFrame(node => !(node is TryStatementSyntax))
          .WithCancellation(CancellationToken)
          .OfType<ReturnStatementSyntax>()
          .Select(returnStatement => returnStatement.Expression)
          .OfType<InvocationExpressionSyntax>()
          .Where(_taskAnalysis.IsTaskTyped);
      }
    }
  }
}
