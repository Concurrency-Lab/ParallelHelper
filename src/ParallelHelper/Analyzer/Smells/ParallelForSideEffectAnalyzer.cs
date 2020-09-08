using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Immutable;

namespace ParallelHelper.Analyzer.Smells {
  /// <summary>
  /// Analyzer that analyzes sources for the use of side-effects inside Parallel.For or Paralle.ForEach statements.
  /// 
  /// <example>Illustrates a class with two methods that write to a shared variable inside a Parallel.For and a Paralle.ForEach statement.
  /// <code>
  /// using System.Linq;
  /// using System.Threading.Tasks;
  /// 
  /// class Sample {
  ///   public int[] DoWorkAsync(int[] accumulate) {
  ///     int sum = 0;
  ///     Parallel.For(0, 10, i => sum += i);
  ///   }
  ///   
  ///   public int[] DoWorkAsync2(int[] accumulate) {
  ///     int sum = 0;
  ///     Parallel.ForEach(accumulate, i => sum += i);
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class ParallelForSideEffectAnalyzer : DiagnosticAnalyzer {
    // TODO This analysis currently only supports side-effects upon local variables. Add support for member-fields.
    // TODO Mark the actual side-effect or the whole Parallel.For statement?
    public const string DiagnosticId = "PH_S010";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Parallel.For Side-Effects";
    private static readonly LocalizableString MessageFormat = "Having side-effects inside Paralle.For statements may lead to undesired results.";
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

    private class Analyzer : SyntaxNodeAnalyzerBase<InvocationExpressionSyntax> {
      private readonly ParallelAnalysis _parallelAnalysis;

      public Analyzer(SyntaxNodeAnalysisContext context) : base(context) {
        _parallelAnalysis = new ParallelAnalysis(SemanticModel, CancellationToken);
      }

      public override void Analyze() {
        if(IsParallelMethodWithSideEffects()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, Root.GetLocation()));
        }
      }

      private bool IsParallelMethodWithSideEffects() {
        return _parallelAnalysis.TryGetParallelForOrForEachDelegate(Root, out var expression)
          && SemanticModel.HasSideEffects(expression!);
      }
    }
  }
}
