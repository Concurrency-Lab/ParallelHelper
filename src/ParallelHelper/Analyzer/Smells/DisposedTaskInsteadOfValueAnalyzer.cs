using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.Smells {
  /// <summary>
  /// Analyzer that analyzes sources for using statements that dispose the task instead of the encapsulated value.
  /// 
  /// <example>Illustrates a class with a method that disposes a task instead of awaiting it and disposing its value.
  /// <code>
  /// class Sample {
  ///   private async Task&lt;IDisposable&gt; CreateAsync() {
  ///     return new SomeDisposable();
  ///   }
  ///   
  ///   public void DoIt() {
  ///     using(CreateAsync()) {
  ///     }
  ///   }
  /// }
  /// 
  /// class SomeDisposable : IDisposable {
  ///   public void Dispose() {}
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class DisposedTaskInsteadOfValueAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S033";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Disposed Task instead of Value";
    private static readonly LocalizableString MessageFormat = "The using statement disposes the task instead of the encapsulated disposable value.";
    private static readonly LocalizableString Description = "";

    private const string DisposableType = "System.IDisposable";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.UsingStatement);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerBase<UsingStatementSyntax> {
      private readonly TaskAnalysis _taskAnalysis;

      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper(context)) {
        _taskAnalysis = new TaskAnalysis(context.SemanticModel, context.CancellationToken);
      }

      public override void Analyze() {
        if(Root.Expression != null) {
          AnalyzeUsingExpression(Root.Expression);
        }
      }

      public void AnalyzeUsingExpression(ExpressionSyntax expression) {
        if(IsTaskWithDisposableValue(expression)) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, expression.GetLocation()));
        }
      }

      private bool IsTaskWithDisposableValue(ExpressionSyntax expression) {
        var type = SemanticModel.GetTypeInfo(expression, CancellationToken).Type as INamedTypeSymbol;
        return type != null
          && type.TypeArguments.Length == 1
          && _taskAnalysis.IsTaskType(type)
          && IsDisposable(type.TypeArguments[0]);
      }

      private bool IsDisposable(ITypeSymbol type) {
        return IsDisposableType(type)
          || type.AllInterfaces.WithCancellation(CancellationToken).Any(IsDisposableType);
      }

      private bool IsDisposableType(ITypeSymbol type) {
        return SemanticModel.IsEqualType(type, DisposableType);
      }
    }
  }
}
