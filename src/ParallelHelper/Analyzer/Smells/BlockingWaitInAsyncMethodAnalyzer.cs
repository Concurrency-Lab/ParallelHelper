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
  /// Analyzer that analyzes sources for (potentially) async methods that blockingly wait for tasks.
  /// 
  /// <example>Illustrates a class with an asynchronous method that blockingly waits for the tasks completion.
  /// <code>
  /// class Sample {
  ///   public async Task DoWorkTwiceAsync() {
  ///     DoWorkAsync().Wait();
  ///     DoWorkAsync().Wait();
  ///     return Task.CompletedTask;
  ///   }
  ///   
  ///   public Task DoWorkAsync() {
  ///     return Task.CompletedTask;
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class BlockingWaitInAsyncMethodAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S026";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Blocking Wait in Async Method";
    private static readonly LocalizableString MessageFormat = "The method appears to be asynchronous, but it synchronously waits for the completion of a task. Use 'await' instead.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    private static readonly string[] TaskTypes = {
      "System.Threading.Tasks.Task",
      "System.Threading.Tasks.Task`1",
    };

    private const string WaitMethod = "Wait";
    private const string ResultProperty = "Result";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : SyntaxNodeAnalyzerBase<MethodDeclarationSyntax> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(context) { }

      public override void Analyze() {
        if(!IsAsyncMethod()) {
          return;
        }
        foreach(var blockingTaskUsage in GetBlockingTaskUsages()){
          Context.ReportDiagnostic(Diagnostic.Create(Rule, blockingTaskUsage.GetLocation()));
        }
      }

      private bool IsAsyncMethod() {
        return Node.Modifiers.Any(SyntaxKind.AsyncKeyword)
          || IsTaskTyped(Node.ReturnType);
      }

      private bool IsTaskType(ITypeSymbol type) {
          return TaskTypes.WithCancellation(CancellationToken)
            .Any(typeName => SemanticModel.IsEqualType(type, typeName));
      }

      private IEnumerable<ExpressionSyntax> GetBlockingTaskUsages() {
        return GetTaskWaitInvocations()
          .Cast<ExpressionSyntax>()
          .Concat(GetTaskResultAccesses());
      }

      private IEnumerable<InvocationExpressionSyntax> GetTaskWaitInvocations() {
        return Node.DescendantNodesInSameActivationFrame()
          .WithCancellation(CancellationToken)
          .OfType<InvocationExpressionSyntax>()
          .Where(invocation => IsTaskMemberAccess(invocation.Expression, WaitMethod));
      }

      private IEnumerable<MemberAccessExpressionSyntax> GetTaskResultAccesses() {
        return Node.DescendantNodesInSameActivationFrame()
          .WithCancellation(CancellationToken)
          .OfType<MemberAccessExpressionSyntax>()
          .Where(memberAccess => IsTaskMemberAccess(memberAccess, ResultProperty));
      }

      private bool IsTaskMemberAccess(ExpressionSyntax expression, string memberName) {
        return expression is MemberAccessExpressionSyntax memberAccess
          && memberAccess.Name.Identifier.Text.Equals(memberName)
          && IsTaskTyped(memberAccess.Expression);
      }

      private bool IsTaskTyped(ExpressionSyntax expression) {
        var returnType = SemanticModel.GetTypeInfo(expression, CancellationToken).Type;
        return returnType != null && IsTaskType(returnType);
      }
    }
  }
}
