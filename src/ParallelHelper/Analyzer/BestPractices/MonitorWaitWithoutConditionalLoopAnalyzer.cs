using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Util;
using System.Collections.Immutable;

namespace ParallelHelper.Analyzer.BestPractices {
  /// <summary>
  /// Analyzer that analyzes sources for <see cref="System.Threading.Monitor.Wait(object)"/> invocations without an
  /// enclosing conditional loop.
  /// 
  /// <example>The invocation of <c>Monitor.Wait</c> without being enclosed by a conditional loop.
  /// <code>
  /// class Sample {
  ///   private readonly object syncObject = new object();
  /// 
  ///   public void DoWork() {
  ///     lock(syncObject) {
  ///       Monitor.Wait(syncObject);
  ///     }
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class MonitorWaitWithoutConditionalLoopAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_P002";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Monitor.Wait without Conditional Loop";
    private static readonly LocalizableString MessageFormat = "It is advisable to enclose a Monitor.Wait invocation with a conditional loop.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSemanticModelAction(AnalyzeSemanticModel);
    }

    private static void AnalyzeSemanticModel(SemanticModelAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : MonitorAwareAnalyzerWithSyntaxWalkerBase<SyntaxNode> {
      public Analyzer(SemanticModelAnalysisContext context) : base(new SemanticModelAnalysisContextWrapper(context)) { }

      public override void VisitInvocationExpression(InvocationExpressionSyntax node) {
        if(IsInsideLock && !IsInsideLoopEnclosedByLock && MonitorAnalysis.IsMonitorWait(node)) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation()));
        }
        base.VisitInvocationExpression(node);
      }
    }
  }
}
