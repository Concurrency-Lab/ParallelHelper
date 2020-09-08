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
  /// Analyzer that analyzes sources for the use lock-statements inside Parallel.For or Paralle.ForEach statements.
  /// 
  /// <example>Illustrates a class that applies synchronization using a monitor lock inside a Parallel.For invocation.
  /// <code>
  /// using System.Linq;
  /// using System.Threading.Tasks;
  /// 
  /// class Sample {
  ///   public int[] DoWorkAsync(int[] accumulate) {
  ///     var syncObject = new object();
  ///     int sum = 0;
  ///     Parallel.For(0, 10, i => {
  ///       lock(syncObject) {
  ///         sum += i
  ///       }
  ///     });
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class ParallelForWithMonitorLockAnalyzer : DiagnosticAnalyzer {
    // TODO Mark the whole Parallel.For(Each) or only the lock-statement?
    public const string DiagnosticId = "PH_S022";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Parallel.For with Monitor Synchronization";
    private static readonly LocalizableString MessageFormat = "The application of synchronization inside the loop body of a Parallel.For introduces unnecessary overhead. Apply the synchronization within the local finally body.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeExpressionStatement, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeExpressionStatement(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerBase<InvocationExpressionSyntax> {
      private readonly ParallelAnalysis _parallelAnalysis;

      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper<InvocationExpressionSyntax>(context)) {
        _parallelAnalysis = new ParallelAnalysis(SemanticModel, CancellationToken);
      }

      public override void Analyze() {
        if(IsParallelMethodWithLockStatement()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, Root.GetLocation()));
        }
      }

      private bool IsParallelMethodWithLockStatement() {
        return _parallelAnalysis.TryGetParallelForOrForEachDelegate(Root, out var expression)
          && ContainsLockStatements(expression!);
      }

      private bool ContainsLockStatements(ExpressionSyntax expression) {
        return expression.DescendantNodesAndSelf()
          .WithCancellation(CancellationToken)
          .OfType<LockStatementSyntax>()
          .Any();
      }
    }
  }
}
