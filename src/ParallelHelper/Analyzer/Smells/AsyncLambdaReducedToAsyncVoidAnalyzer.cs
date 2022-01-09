using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Util;
using System.Collections.Immutable;

namespace ParallelHelper.Analyzer.Smells {
  /// <summary>
  /// Analyzer that analyzes sources for async lambda expressions that are reduced to async void.
  /// 
  /// <example>Illustrates a class that defines an async lambda expression which is reduced to async void.
  /// interface IWorkQueue {
  ///   // Method only accepts synchronous callbacks
  ///   void ScheduleWork(Action job);
  /// }
  /// 
  /// class FileDownloader {
  ///   private readonly WebClient webClient;
  ///   private readonly IWorkQueue queue;
  /// 
  ///   public void DownloadFile(Uri address, StreamWriter output) {
  ///     queue.ScheduleWork(
  ///       async () => {  // Will be inferred as async void
  ///       var body = await webClient.DownloadStringTaskAsync(address);
  ///         await output.WriteAsync(body);
  ///       }
  ///     );
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class AsyncLambdaReducedToAsyncVoidAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S034";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Async Lambda Reduced to Async Void";
    private static readonly LocalizableString MessageFormat = "The async lambda expression is reduced to async void; thus, it cannot be awaited and uncaught exceptions may crash the application.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.SimpleLambdaExpression);
    }

    private static void AnalyzeLambdaExpression(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerBase<LambdaExpressionSyntax> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper(context)) { }

      public override void Analyze() {
        if(IsAsyncVoidLambda()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, Root.GetLocation()));
        }
      }

      private bool IsAsyncVoidLambda() {
        return SemanticModel.GetSymbolInfo(Root, CancellationToken).Symbol is IMethodSymbol method
          && method.ReturnsVoid
          && method.IsAsync;
      }
    }
  }
}
