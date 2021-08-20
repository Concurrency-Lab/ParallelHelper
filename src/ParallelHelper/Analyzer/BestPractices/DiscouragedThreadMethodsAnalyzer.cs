using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Util;
using System.Collections.Immutable;

namespace ParallelHelper.Analyzer.BestPractices {
  /// <summary>
  /// Analyzer that analyzes sources for the use of discouraged methods.
  /// 
  /// <example>Illustrates a class that uses the discouraged method <see cref="System.Threading.Thread.Abort"/>.
  /// <code>
  /// class Sample {
  ///   private readonly Thread workerThread;
  ///   
  ///   public void Stop() {
  ///     workerThread.Abort();
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class DiscouragedThreadMethodsAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_P003";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Discouraged Thread Method";
    private static readonly LocalizableString MessageFormat = "The use of the method '{0}' is discouraged.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    private static readonly ClassMemberDescriptor[] DiscouragedMethods = {
      new ClassMemberDescriptor("System.Threading.Thread", "Abort", "Suspend", "Resume")
    };

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context) {
      new InvocationReportingAnalyzer(context, Rule, DiscouragedMethods).Analyze();
    }
  }
}
