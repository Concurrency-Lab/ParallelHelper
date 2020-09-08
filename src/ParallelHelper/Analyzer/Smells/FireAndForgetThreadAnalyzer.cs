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
  /// Analyzer that analyzes sources for the use of fire-and-forget threads / tasks.
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
  public class FireAndForgetThreadAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S004";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Fire-and-forget Threads";
    private static readonly LocalizableString MessageFormat = "The use of threads or tasks in a fire-and-forget manner is discouraged.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    private static readonly StartDescriptor[] ThreadStartMethods = {
      new StartDescriptor("System.Threading.Tasks.Task", "Run", true),
      new StartDescriptor("System.Threading.Tasks.TaskFactory", "StartNew", true),
      new StartDescriptor("System.Threading.Thread", "Start")
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
      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper<ExpressionStatementSyntax>(context)) { }

      public override void Analyze() {
        if(Root.Expression is InvocationExpressionSyntax invocation && IsFireAndForgetThread(invocation)) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, Root.GetLocation()));
        }
      }

      private bool IsFireAndForgetThread(InvocationExpressionSyntax invocationExpression) {
        if(SemanticModel.GetSymbolInfo(invocationExpression, CancellationToken).Symbol is IMethodSymbol method) {
          var descriptor = GetStartDescriptor(method);
          return descriptor != null && (descriptor.IsFactory || IsAccessingInlineConstructedInstance(invocationExpression));
        }
        return false;
      }

      private StartDescriptor? GetStartDescriptor(IMethodSymbol method) {
        return ThreadStartMethods
          .WithCancellation(CancellationToken)
          .Where(descriptor => SemanticModel.IsEqualType(method.ContainingType, descriptor.Type))
          .FirstOrDefault(descriptor => method.Name.Equals(descriptor.Method));
      }

      private static bool IsAccessingInlineConstructedInstance(InvocationExpressionSyntax invocationExpression) {
        return (invocationExpression.Expression as MemberAccessExpressionSyntax)?.Expression is ObjectCreationExpressionSyntax;
      }
    }

    private class StartDescriptor {
      public string Type { get; }
      public string Method { get; }
      public bool IsFactory { get; }

      public StartDescriptor(string type, string method) : this(type, method, false) { }

      public StartDescriptor(string type, string method, bool isFactory) {
        Type = type;
        Method = method;
        IsFactory = isFactory;
      }
    }
  }
}
