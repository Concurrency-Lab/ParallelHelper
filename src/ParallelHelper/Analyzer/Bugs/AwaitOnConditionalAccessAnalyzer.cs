using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Util;
using System.Collections.Immutable;

namespace ParallelHelper.Analyzer.Bugs {
  /// <summary>
  /// Analyzer that analyzes sources for accidentially awaiting null when using null-conditionals in conjunction with await.
  /// 
  /// <example>Illustrates a class with a method that awaits a task resulting from a conditional access.
  /// <code>
  /// class Sample {
  ///   private readonly object syncObject = new object();
  ///   
  ///   public async DoWork() {
  ///     var nested = new Nested();
  ///     await nested.Other?.Task;
  ///   }
  ///   
  ///   private class Nested {
  ///     public Nested Other { get; }
  ///     public Task Task { get; }
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class AwaitOnConditionalAccessAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_B014";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Await On Conditional Access";
    private static readonly LocalizableString MessageFormat = "The await expression awaits an object that may be null due its conditional access.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeAwaitExpression, SyntaxKind.AwaitExpression);
    }

    private static void AnalyzeAwaitExpression(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerBase<AwaitExpressionSyntax> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper(context)) { }

      public override void Analyze() {
        if(AwaitsConditionalAccess()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, Root.GetLocation()));
        }
      }

      private bool AwaitsConditionalAccess() {
        return ContainsConditionalAccess(Root.Expression);
      }

      private bool ContainsConditionalAccess(ExpressionSyntax expression) {
        return expression switch {
          ConditionalAccessExpressionSyntax _ => true,
          ParenthesizedExpressionSyntax parenthesized => ContainsConditionalAccess(parenthesized.Expression),
          MemberAccessExpressionSyntax memberAccess => ContainsConditionalAccess(memberAccess.Expression),
          CastExpressionSyntax cast => ContainsConditionalAccess(cast.Expression),
          BinaryExpressionSyntax binary => binary.IsKind(SyntaxKind.AsExpression),
          _ => false
        };
      }
    }
  }
}
