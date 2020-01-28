using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.Smells {
  /// <summary>
  /// Analyzer that analyzes sources for async methods that include blocking lock-statements.
  /// 
  /// <example>Illustrates a class with an async method incorporates a lock-statement.
  /// <code>
  /// using System.Threading;
  /// 
  /// class Sample {
  ///   private readonly object syncObject = new object();
  ///   
  ///   public async Task DoWorkAsync() {
  ///     lock(syncObject) {
  ///     }
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class MonitorLockInAsyncMethodAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S023";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Monitor Lock in Async Method";
    private static readonly LocalizableString MessageFormat = "Using the blocking synchronization mechanics of a monitor inside an async method is discouraged; use SemaphoreSlim instead.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(
        AnalyzeMethodOrAnonymousFunction, 
        SyntaxKind.MethodDeclaration,
        SyntaxKind.SimpleLambdaExpression,
        SyntaxKind.ParenthesizedLambdaExpression,
        SyntaxKind.AnonymousMethodExpression,
        SyntaxKind.LocalFunctionStatement
      );
    }

    private static void AnalyzeMethodOrAnonymousFunction(SyntaxNodeAnalysisContext context) {
      if(context.Node.IsMethodOrFunctionWithAsyncModifier()) {
        new Analyzer(context).Analyze();
      }
    }

    private class Analyzer : SyntaxNodeAnalyzerBase<SyntaxNode> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(context) { }

      public override void Analyze() {
        foreach(var lockStatement in GetLockStatementsInSameMethodScope()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, lockStatement.GetLocation()));
        }
      }

      private IEnumerable<LockStatementSyntax> GetLockStatementsInSameMethodScope() {
        return Node.DescendantNodesInSameActivationFrame()
          .WithCancellation(CancellationToken)
          .OfType<LockStatementSyntax>();
      }
    }
  }
}
