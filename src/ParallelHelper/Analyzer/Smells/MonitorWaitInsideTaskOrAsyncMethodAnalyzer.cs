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
  /// Analyzer that analyzes sources for async methods or task bodies that make use of <see cref="System.Threading.Monitor.Wait(object)"/>.
  /// 
  /// <example>Illustrates a class with a method that uses the *Async suffix although the implementation is CPU-bound.
  /// <code>
  /// class Sample {
  ///   private readonly object syncObject = new object();
  /// 
  ///   public Task StartWork() {
  ///     return Task.Run(() => {
  ///       lock(syncObject) {
  ///         Monitor.Wait(syncObject);
  ///       }
  ///     });
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class MonitorWaitInsideTaskOrAsyncMethodAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S031";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Monitor.Wait Inside Async Method or Task";
    private static readonly LocalizableString MessageFormat = "Monitor.Wait indicates that the enclosing task depends on others, which should be avoided.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    private static readonly string[] TaskTypes = {
      "System.Threading.Tasks.Task",
      "System.Threading.Tasks.Task`1",
    };

    private const string RunMethod = "Run";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSemanticModelAction(AnalyzeSemanticModel);
    }

    private static void AnalyzeSemanticModel(SemanticModelAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerBase<SyntaxNode> {
      private readonly MonitorAnalysis _monitorAnalysis;

      public Analyzer(SemanticModelAnalysisContext context) : base(new SemanticModelAnalysisContextWrapper(context)) {
        _monitorAnalysis = new MonitorAnalysis(context.SemanticModel, context.CancellationToken);
      }

      public override void Analyze() {
        foreach(var invocation in GetAllMonitorWaitInvocationsInsideTaskActivationFrames()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
        }
      }

      private IEnumerable<InvocationExpressionSyntax> GetAllMonitorWaitInvocationsInsideTaskActivationFrames() {
        return GetAllUniqueTaskBodies()
          .SelectMany(body => body.DescendantNodesInSameActivationFrame())
          .WithCancellation(CancellationToken)
          .OfType<InvocationExpressionSyntax>()
          .Where(_monitorAnalysis.IsMonitorWait);
      }

      private IEnumerable<SyntaxNode> GetAllUniqueTaskBodies() {
        return GetAllMethodsAndFunctionsWithAsyncModifier()
          .Concat(GetAllTaskDelegateBodies())
          .Distinct();
      }

      private IEnumerable<SyntaxNode> GetAllMethodsAndFunctionsWithAsyncModifier() {
        return Root.DescendantNodesAndSelf()
          .WithCancellation(CancellationToken)
          .Where(node => node.IsMethodOrFunctionWithAsyncModifier());
      }

      private IEnumerable<SyntaxNode> GetAllTaskDelegateBodies() {
        return Root.DescendantNodesAndSelf()
          .WithCancellation(CancellationToken)
          .OfType<InvocationExpressionSyntax>()
          .Where(invocation => invocation.ArgumentList.Arguments.Count == 1)
          .Where(IsTaskRunInvocation)
          .Select(invocation => invocation.ArgumentList.Arguments[0].Expression)
          .SelectMany(GetReferencedSyntaxNodes);
      }

      private bool IsTaskRunInvocation(InvocationExpressionSyntax invocation) {
        return SemanticModel.GetSymbolInfo(invocation, CancellationToken).Symbol is IMethodSymbol method
          && method.Name == RunMethod
          && TaskTypes.Any(type => SemanticModel.IsEqualType(method.ContainingType, type));
      }

      private IEnumerable<SyntaxNode> GetReferencedSyntaxNodes(ExpressionSyntax expression) {
        if(expression is AnonymousFunctionExpressionSyntax function) {
          return new[] { function };
        }
        var method = SemanticModel.GetSymbolInfo(expression, CancellationToken).Symbol as IMethodSymbol;
        if(method == null) {
          return Enumerable.Empty<SyntaxNode>();
        }
        return SemanticModel.GetResolvableDeclaringSyntaxes(method, CancellationToken);
      }
    }
  }
}
