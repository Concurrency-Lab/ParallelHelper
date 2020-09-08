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
  /// The task start factory <see cref="System.Threading.Tasks.TaskFactory.StartNew(System.Action)"/> does not support async delegates
  /// and requries manual unwrapping / double use of await..
  /// 
  /// <example>Illustrates that starts a task with an async delegate.
  /// <code>
  /// class Sample {
  ///   private object syncObject = 1;
  ///   
  ///   public Task DoWork() {
  ///     return Task.Factory.StartNew(DoItAsync);
  ///   }
  ///   
  ///   public async Task DoItAsync() {
  ///     // ...
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class TaskFactoryStartNewWithAsyncDelegateAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S014";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Task.Factory.StartNew with async delegate";
    private static readonly LocalizableString MessageFormat = "The method Task.Factory.StartNew does not support async delegates and requires manual unwrapping of the inner tasks. Prefer the use Task.Run instead.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    private const string TaskFactoryType = "System.Threading.Tasks.TaskFactory";
    private const string StartNewMethod = "StartNew";

    private static readonly string[] TaskTypes = {
      "System.Threading.Tasks.Task",
      "System.Threading.Tasks.Task`1"
    };

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeExpressionStatement, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeExpressionStatement(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerBase<InvocationExpressionSyntax> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper<InvocationExpressionSyntax>(context)) { }

      public override void Analyze() {
        if(!IsTaskFactoryStartNew() || !IsInvokingAsyncDelegate()) {
          return;
        }
        Context.ReportDiagnostic(Diagnostic.Create(Rule, Root.GetLocation()));
      }

      private bool IsTaskFactoryStartNew() {
        return SemanticModel.GetSymbolInfo(Root, CancellationToken).Symbol is IMethodSymbol method
          && method.Name.Equals(StartNewMethod) && SemanticModel.IsEqualType(method.ContainingType, TaskFactoryType);
      }

      private bool IsInvokingAsyncDelegate() {
        var arguments = Root.ArgumentList.Arguments;
        if(arguments.Count == 0) {
          return false;
        }
        return SemanticModel.GetSymbolInfo(arguments[0].Expression, CancellationToken).Symbol is IMethodSymbol method
          && (method.IsAsync || IsTaskType(method.ReturnType));
      }

      private bool IsTaskType(ITypeSymbol? type) {
        return type != null
          && TaskTypes.Any(taskType => SemanticModel.IsEqualType(type, taskType));
      }
    }
  }
}
