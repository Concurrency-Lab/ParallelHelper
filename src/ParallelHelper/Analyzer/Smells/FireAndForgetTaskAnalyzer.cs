using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Immutable;

namespace ParallelHelper.Analyzer.Smells {
  /// <summary>
  /// Analyzer that analyzes sources for the use of fire-and-forget tasks.
  /// 
  /// <example>Illustrates a class with a method that starts a task without making use of the returned task object.
  /// <code>
  /// class Sample {
  ///   public void DoWork() {
  ///     Task.Run(() => /* ... */));
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class FireAndForgetTaskAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S033";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Fire-and-Forget Tasks";
    private static readonly LocalizableString MessageFormat = "The use of tasks in a fire-and-forget manner is discouraged.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    private static readonly ClassMemberDescriptor[] TaskStartMethods = {
      new ClassMemberDescriptor("System.Threading.Tasks.Task", "Run"),
      new ClassMemberDescriptor("System.Threading.Tasks.TaskFactory", "StartNew")
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeExpressionStatement, SyntaxKind.ExpressionStatement);
    }

    private static void AnalyzeExpressionStatement(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerBase<ExpressionStatementSyntax> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper(context)) { }

      public override void Analyze() {
        if(IsFireAndForgetTask()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, Root.GetLocation()));
        }
      }

      private bool IsFireAndForgetTask() {
        return Root.Expression is InvocationExpressionSyntax invocation
          && SemanticModel.GetSymbolInfo(invocation, CancellationToken).Symbol is IMethodSymbol method
          && IsTaskStartMethod(method);
      }

      private bool IsTaskStartMethod(IMethodSymbol method) {
        return TaskStartMethods.AnyContainsMember(SemanticModel, method);
      }
    }
  }
}
