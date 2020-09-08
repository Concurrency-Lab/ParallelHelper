using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Util;
using System.Collections.Immutable;

namespace ParallelHelper.Analyzer.BestPractices {
  /// <summary>
  /// Analyzer that analyzes sources for the use of <see cref="System.Threading.Monitor.Enter(object)"/>.
  /// 
  /// <example>Illustrates the use of Monitor.Enter where the use of the lock-statement would be less error-prone.
  /// <code>
  /// class Sample {
  ///   private object syncObject = new object();
  ///   
  ///   public void DoWork() {
  ///     Monitor.Enter(syncObject);
  ///     // ...
  ///     Monitor.Exit(syncObject);
  ///   }
  ///   
  ///   public async Task DoWorkAsync() {
  ///   
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class MonitorLockInsteadOfEnterAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_P006";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Discouraged Monitor Method";
    private static readonly LocalizableString MessageFormat = "Favor the use of the lock-statement instead of the use of Monitor.Enter when no timeouts are needed.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerBase<InvocationExpressionSyntax> {
      private readonly MonitorAnalysis _monitorAnalysis;

      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper<InvocationExpressionSyntax>(context)) {
        _monitorAnalysis = new MonitorAnalysis(SemanticModel, CancellationToken);
      }

      public override void Analyze() {
        if(_monitorAnalysis.IsMonitorEnter(Root) && !IsAcceptableEnterOverload()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, Root.GetLocation()));
        }
      }

      private bool IsAcceptableEnterOverload() {
        var arguments = Root.ArgumentList.Arguments;
        return arguments.Count >= 2 && IsLockTakenArgument(arguments[1]);
      }

      private bool IsLockTakenArgument(ArgumentSyntax argument) {
        return argument.RefKindKeyword.IsKind(SyntaxKind.RefKeyword)
          && IsBooleanTyped(argument.Expression);
      }

      private bool IsBooleanTyped(ExpressionSyntax expression) {
        return SemanticModel.GetTypeInfo(expression, CancellationToken).Type?.SpecialType == SpecialType.System_Boolean;
      }
    }
  }
}
