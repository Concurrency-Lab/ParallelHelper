using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.Smells {
  /// <summary>
  /// Analyzer that analyzes sources for the use of side-effects inside PLINQ expressions.
  /// 
  /// <example>Illustrates a class with two methods that write to a shared variable inside a PLINQ expression.
  /// <code>
  /// using System.Linq;
  /// 
  /// class Sample {
  ///   public int[] DoWorkAsync(int[] accumulate) {
  ///     int sum = 0;
  ///     return accumulate.AsParallel().Select(i => {
  ///       sum += i;
  ///       return sum;
  ///     }).ToArray();
  ///   }
  ///   
  ///   public int[] DoWorkAsync2(int[] accumulate) {
  ///     int sum = 0;
  ///     return (from i in test.AsParallel() select i + ++sum).ToArray();
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class PlinqSideEffectAnalyzer : DiagnosticAnalyzer {
    // AsParallel returns a ParallelQuery typed result. Only queries of this type are executed in parallel.
    // TODO This analysis currently only supports side-effects upon local variables. Add support for member-fields.
    // TODO Mark the actual side-effect or the whole PLINQ expression?
    public const string DiagnosticId = "PH_S009";

    private const string Category = "Concurrency";

    private static readonly string[] ParallelQueryTypes = { "System.Linq.ParallelQuery", "System.Linq.ParallelQuery`1" };

    private static readonly LocalizableString Title = "PLINQ Side-Effects";
    private static readonly LocalizableString MessageFormat = "Having side-effects inside PLINQ expressions may lead to undesired results.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);


    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeExpressionStatement, SyntaxKind.QueryExpression, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeExpressionStatement(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : SyntaxNodeAnalyzerBase<ExpressionSyntax> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(context) { }

      public override void Analyze() {
        if(IsParallelQueryExpressionWithSideEffects() || IsParallelMethodInvocationWithSideEffects()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, Root.GetLocation()));
        }
      }

      private bool IsParallelQueryExpressionWithSideEffects() {
        return Root is QueryExpressionSyntax query
          && IsParallelQuery(query.FromClause.Expression)
          && GetExpressionsFromQueryBody(query.Body).Any(SemanticModel.HasSideEffects);
      }

      private bool IsParallelMethodInvocationWithSideEffects() {
        // If the result of an invocation of the type ParallelQuery, a PLINQ method was used.
        return Root is InvocationExpressionSyntax invocation
          && IsParallelQuery(invocation)
          && invocation.ArgumentList.Arguments.Any(a => SemanticModel.HasSideEffects(a.Expression));
      }

      private bool IsParallelQuery(ExpressionSyntax expression) {
        var typeSymbol = SemanticModel.GetTypeInfo(expression, CancellationToken).Type;
        return typeSymbol != null && ParallelQueryTypes.Any(type => SemanticModel.IsEqualType(typeSymbol, type));
      }

      private IEnumerable<ExpressionSyntax> GetExpressionsFromQueryBody(QueryBodySyntax body) {
        switch(body.SelectOrGroup) {
        case SelectClauseSyntax selectClause:
          yield return selectClause.Expression;
          break;
        case GroupClauseSyntax groupClause:
          yield return groupClause.GroupExpression;
          yield return groupClause.ByExpression;
          if(body.Continuation != null) {
            foreach(var sub in GetExpressionsFromQueryBody(body.Continuation.Body)) { 
              yield return sub;
            }
          }
          break;
        }
      }
    }
  }
}
