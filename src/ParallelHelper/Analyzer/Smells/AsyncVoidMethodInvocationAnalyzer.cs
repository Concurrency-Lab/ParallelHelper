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
  /// Analyzer that analyzes sources for invocations of async void methods.
  /// 
  /// <example>Illustrates a class with a method that invokes an async method with return type void.
  /// <code>
  /// class Sample {
  ///   public void DoIt() {
  ///     DoItAsync();
  ///   }
  ///   
  ///   public async void DoItAsync() {
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class AsyncVoidMethodInvocationAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S030";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Async Void Method Invocation";
    private static readonly LocalizableString MessageFormat = "The invoked method is async with return type void. Replace the method with an implementation that can be awaited.";
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

    private class Analyzer : SyntaxNodeAnalyzerBase<InvocationExpressionSyntax> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(context) { }

      public override void Analyze() {
        if(IsAsyncVoidMethod()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, Root.GetLocation()));
        }
      }

      private bool IsAsyncVoidMethod() {
        return SemanticModel.GetSymbolInfo(Root, CancellationToken).Symbol is IMethodSymbol method
          && method.ReturnsVoid
          && method.IsAsync;
      }
    }
  }
}
