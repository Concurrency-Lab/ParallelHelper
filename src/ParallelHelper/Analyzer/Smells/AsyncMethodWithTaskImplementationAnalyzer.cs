using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.Smells {
  /// <summary>
  /// Analyzer that analyzes sources for the use of the *Async suffix for methods that are implemented with tasks - so-called fake-async methods.
  /// 
  /// <example>Illustrates a class with a method that uses the *Async suffix although the implementation is CPU-bound.
  /// <code>
  /// class Sample {
  ///   public Task DoWorkAsync() {
  ///     return Task.Run(() => /* ... */));
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class AsyncMethodWithTaskImplementationAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S005";

    private const string Category = "Concurrency";

    private const string AsyncSuffix = "Async";

    private static readonly LocalizableString Title = "Fake-Async Methods";
    private static readonly LocalizableString MessageFormat = "The use of the *Async suffix is discouraged for methods that are task-based implementations.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    private static readonly StartDescriptor[] TaskStartMethods = {
      new StartDescriptor("System.Threading.Tasks.Task", "Run"),
      new StartDescriptor("System.Threading.Tasks.TaskFactory", "StartNew")
    };

    private static readonly string[] TaskTypes = {
      "System.Threading.Tasks.Task",
      "System.Threading.Tasks.Task`1",
    };

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
        if(!HasMethodBody() || !IsMethodWithAsyncSuffix() || !ReturnsTaskObject() || !ReturnsCpuBoundTask()) {
          return;
        }
        Context.ReportDiagnostic(Diagnostic.Create(Rule, Node.GetSignatureLocation()));
      }

      private bool HasMethodBody() {
        return Node.Body != null || Node.ExpressionBody != null;
      }

      private bool IsMethodWithAsyncSuffix() {
        return Node.Identifier.Text.EndsWith(AsyncSuffix);
      }

      private bool ReturnsTaskObject() {
        var returnType = SemanticModel.GetTypeInfo(Node.ReturnType, CancellationToken).Type;
        return returnType != null
          && TaskTypes
            .WithCancellation(CancellationToken)
            .Any(taskType => SemanticModel.IsEqualType(returnType, taskType));
      }

      private bool ReturnsCpuBoundTask() {
        return GetMethodsReturnValue() is InvocationExpressionSyntax invocation && IsTaskStart(invocation);
      }

      private ExpressionSyntax? GetMethodsReturnValue() {
        if(Node.ExpressionBody != null) {
          return Node.ExpressionBody.Expression;
        }

        // TODO: Decide if really only one statement is allowed or any return statement must not return a manually created task.
        var statements = Node.Body.Statements;
        if(statements.Count != 1) {
          return null;
        }
        return (statements[0] as ReturnStatementSyntax)?.Expression;
      }

      private bool IsTaskStart(InvocationExpressionSyntax invocationExpression) {
        return SemanticModel.GetSymbolInfo(invocationExpression, CancellationToken).Symbol is IMethodSymbol method
          && TaskStartMethods
            .WithCancellation(CancellationToken)
            .Where(descriptor => SemanticModel.IsEqualType(method.ContainingType, descriptor.Type))
            .Any(descriptor => method.Name.Equals(descriptor.Method));
      }
    }

    private class StartDescriptor {
      public string Type { get; }
      public string Method { get; }

      public StartDescriptor(string type, string method) {
        Type = type;
        Method = method;
      }
    }
  }
}
