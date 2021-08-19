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
  public class DiscouragedEntityFrameworkMethodsAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_P013";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Discouraged EntityFramework Method";
    private static readonly LocalizableString MessageFormat = "The use of the EntityFramework method '{0}' is discouraged.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    private static readonly MethodDescriptor[] DiscouragedMethods = {
      new MethodDescriptor("Microsoft.EntityFrameworkCore.DbContext", new string[] { "AddAsync", "AddRangeAsync" }),
      new MethodDescriptor("Microsoft.EntityFrameworkCore.DbSet`1", new string[] { "AddAsync", "AddRangeAsync" })
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
