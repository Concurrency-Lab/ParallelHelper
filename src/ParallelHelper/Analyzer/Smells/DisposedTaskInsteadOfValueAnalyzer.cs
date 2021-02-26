using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Immutable;

namespace ParallelHelper.Analyzer.Smells {
  /// <summary>
  /// Analyzer that analyzes sources for async methods without *Async suffix that have a counterpart with the *Async suffix.
  /// 
  /// <example>Illustrates a class with a method that uses the *Async suffix although the implementation is CPU-bound.
  /// <code>
  /// class Sample {
  ///   public Task DoWork() {
  ///     return Task.CompletedTask;
  ///   }
  ///   
  ///   public Task DoWorkAsync() {
  ///     return Task.CompletedTask;
  ///   }
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
        if(DisposedTaskWithDisposableValue()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, Root.Expression.GetLocation()));
        }
      }

      private bool DisposedTaskWithDisposableValue() {
        if(Root.Expression == null) {
          return false;
        }
        var type = SemanticModel.GetTypeInfo(Root.Expression, CancellationToken).Type as INamedTypeSymbol;
        return type != null
          && type.TypeArguments.Length == 1
          && _taskAnalysis.IsTaskType(type)
          && IsDisposable(type.TypeArguments[0]);
      }

      private bool IsDisposable(ITypeSymbol type) {
        return SemanticModel.IsEqualType(type, DisposableType);
      }
    }
  }
}
