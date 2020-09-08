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
  /// Analyzer that analyzes sources for the use of <see cref="System.Threading.ThreadLocal{T}"/> instances inside asynchronous methods.
  /// 
  /// <example>Illustrates a class with an asynchronous method that reads and writes a thread local value.
  /// <code>
  /// class Sample {
  ///   private readonly ThreadLocal&lt;int&gt; count = new ThreadLocal&lt;int&gt;();
  /// 
  ///   public async Task&lt;int&gt; GetAndIncrementAsync() {
  ///     count.Value++;
  ///     await Task.Delay(100);
  ///     return count.Value;
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class ThreadLocalInAsynchronousMethodAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S029";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "ThreadLocal in Async Method";
    private static readonly LocalizableString MessageFormat = "The use of ThreadLocal inside asynchronous methods is discouraged. Consider the use of AsyncLocal instead.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    private const string ThreadLocalType = "System.Threading.ThreadLocal`1";
    private const string ValueProperty = "Value";

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(
        AnalyzeMethodDeclaration,
        SyntaxKind.MethodDeclaration,
        SyntaxKind.SimpleLambdaExpression,
        SyntaxKind.ParenthesizedLambdaExpression,
        SyntaxKind.AnonymousMethodExpression,
        SyntaxKind.LocalFunctionStatement
      );
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerBase<SyntaxNode> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper(context)) { }

      public override void Analyze() {
        if(!Root.IsMethodOrFunctionWithAsyncModifier()) {
          return;
        }
        foreach(var memberAccess in GetThreadLocalValueMemberAccesses()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, memberAccess.GetLocation()));
        }
      }

      private IEnumerable<MemberAccessExpressionSyntax> GetThreadLocalValueMemberAccesses() {
        return Root.DescendantNodesInSameActivationFrame()
          .WithCancellation(CancellationToken)
          .OfType<MemberAccessExpressionSyntax>()
          .Where(IsThreadLocalValueProperty);
      }

      private bool IsThreadLocalValueProperty(MemberAccessExpressionSyntax memberAccess) {
        return SemanticModel.GetSymbolInfo(memberAccess, CancellationToken).Symbol is IPropertySymbol property
          && ValueProperty.Equals(property.Name)
          && SemanticModel.IsEqualType(property.ContainingType, ThreadLocalType);
      }
    }
  }
}
