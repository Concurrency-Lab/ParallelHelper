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
  /// Analyzer that analyzes sources for constructors that start threads or tasks..
  /// 
  /// <example>Illustrates a class with a constructor that runs a task upon instantiation.
  /// <code>
  /// class Sample {
  ///   private readonly Task task;
  ///   public Sample() {
  ///     task = Task.Run(() => /* ... *));
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class ThreadStartInsideConstructorAnalyzer : DiagnosticAnalyzer {
    // TODO Check methods invoked by the constructor too.
    public const string DiagnosticId = "PH_S007";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Thread Start in Constructor";
    private static readonly LocalizableString MessageFormat = "Starting threads or tasks inside a constructor is discouraged.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    // TODO Timer (Special case): Can start upon instantiation.
    private static readonly StartDescriptor[] StartMethods = {
      new StartDescriptor("System.Threading.Tasks.Task", "Run"),
      new StartDescriptor("System.Threading.Tasks.TaskFactory", "StartNew"),
      new StartDescriptor("System.Threading.Thread", "Start")
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeExpressionStatement, SyntaxKind.ConstructorDeclaration);
    }

    private static void AnalyzeExpressionStatement(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : SyntaxNodeAnalyzerBase<ConstructorDeclarationSyntax> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(context) { }

      public override void Analyze() {
        foreach(var threadStart in GetThreadStartInvocations()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, threadStart.GetLocation()));
        }
      }

      private IEnumerable<InvocationExpressionSyntax> GetThreadStartInvocations() {
        return Root.DescendantNodes()
          .WithCancellation(CancellationToken)
          .OfType<InvocationExpressionSyntax>()
          .Where(IsThreadStart);
      }

      private bool IsThreadStart(InvocationExpressionSyntax invocation) {
        return SemanticModel.GetSymbolInfo(invocation, CancellationToken).Symbol is IMethodSymbol method
          && IsThreadStart(method);
      }

      private bool IsThreadStart(IMethodSymbol method) {
        return StartMethods.WithCancellation(CancellationToken)
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
