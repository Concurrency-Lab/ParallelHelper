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
  /// Analyzer that analyzes sources for blocking invocations of async methods in constructors.
  /// 
  /// <example>Illustrates a class with a method that invokes an async method within a constructor and blocks for the result.
  /// <code>
  /// class Sample {
  ///   private readonly string content;
  /// 
  ///   public Sample(string filePath) {
  ///     content = File.ReadAllTextAsync(filePath).Result;
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class BlockingAsyncMethodInvocationInConstructorAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S035";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Blocking Async Method Invocation in Constructor";
    private static readonly LocalizableString MessageFormat = "The blocking invocation of async methods within the constructor is discouraged since they cannot be awaited. Use a factory method instead.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeConstructor, SyntaxKind.ConstructorDeclaration);
    }

    private static void AnalyzeConstructor(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerBase<ConstructorDeclarationSyntax> {
      private readonly TaskAnalysis _taskAnalysis;

      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper(context)) {
        _taskAnalysis = new TaskAnalysis(context.SemanticModel, context.CancellationToken);
      }

      public override void Analyze() {
        foreach(var blockingAccess in GetBlockingTaskAccesses()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, blockingAccess.GetLocation()));
        }
      }

      private IEnumerable<SyntaxNode> GetBlockingTaskAccesses() {
        return Root.DescendantNodesInSameActivationFrame()
          .WithCancellation(CancellationToken)
          .Where(IsBlockingTaskAccess);
      }

      private bool IsBlockingTaskAccess(SyntaxNode node) {
        return node switch {
          InvocationExpressionSyntax invocation => _taskAnalysis.IsBlockingMethodInvocation(invocation),
          MemberAccessExpressionSyntax memberAccess => _taskAnalysis.IsBlockingPropertyAccess(memberAccess),
          _ => false
        };
      }
    }
  }
}
