using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.BestPractices {
  /// <summary>
  /// Analyzer that analyzes sources for the use of discouraged methods.
  /// 
  /// <example>Illustrates a class that uses the discouraged method <see cref="System.Threading.Thread.Abort"/>.
  /// <code>
  /// class Sample {
  ///   private object syncObject = new object();
  ///   
  ///   public void DoWork() {
  ///     lock(syncObject) {
  ///     }
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class DiscouragedMethodsAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_P003";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Discouraged Method";
    private static readonly LocalizableString MessageFormat = "The use of the method '{0}' is discouraged.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    private static readonly MethodDescriptor[] DiscouragedMethods = {
      new MethodDescriptor("System.Threading.Thread", new string[] { "Abort", "Suspend", "Resume" })
    };

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerBase<InvocationExpressionSyntax> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper(context)) { }

      public override void Analyze() {
        if(SemanticModel.GetSymbolInfo(Root, CancellationToken).Symbol is IMethodSymbol method && IsDiscouragedMethod(method)) {
          var methodName = $"{method.ContainingType.Name}.{method.Name}";
          Context.ReportDiagnostic(Diagnostic.Create(Rule, Root.GetLocation(), methodName));
        }
      }

      private bool IsDiscouragedMethod(IMethodSymbol method) {
        return DiscouragedMethods.Any(d => IsAnyMethodOf(method, d));
      }

      private bool IsAnyMethodOf(IMethodSymbol method, MethodDescriptor descriptor) {
        return descriptor.Methods.Any(m => method.Name.Equals(m))
          && SemanticModel.IsEqualType(method.ContainingType, descriptor.Type);
      }
    }
  }
}
