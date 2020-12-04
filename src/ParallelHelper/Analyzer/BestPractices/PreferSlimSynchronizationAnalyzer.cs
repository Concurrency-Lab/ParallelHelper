using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.BestPractices {
  /// <summary>
  /// Analyzer that analyzes sources for cases where a method returns a task built with continuations instead
  /// of using the async/await pattern.
  /// 
  /// <example>Illustrates a class with an asynchronous methods that unecessarily makes use of continuations.
  /// <code>
  /// class Sample {
  ///   public Task DoWorkTwiceAsync() {
  ///     return DoWorkAsync().ContinueWith(DoWorkAsync);
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
  public class PreferSlimSynchronizationAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_P012";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Prefer Slim Synchronization";
    private static readonly LocalizableString MessageFormat = "The used synchronization type '{0}' uses OS-level primitives. The use of the lightweight '{1}' might be more suitable.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    private static readonly SynchronizationPrimitive[] SynchronizationPrimitives = {
      new SynchronizationPrimitive("System.Threading.Semaphore", "System.Threading.SemaphoreSlim"),
      new SynchronizationPrimitive("System.Threading.Mutex", "System.Threading.SemaphoreSlim"),
      new SynchronizationPrimitive("System.Threading.ReaderWriterLock", "System.Threading.ReaderWriterLockSlim"),
      new SynchronizationPrimitive("System.Threading.ManualResetEvent", "System.Threading.ManualResetEventSlim")
    };

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeCandidate, SyntaxKind.ObjectCreationExpression);
    }

    private static void AnalyzeCandidate(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerBase<ObjectCreationExpressionSyntax> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper(context)) { }

      public override void Analyze() {
        var method = (IMethodSymbol)SemanticModel.GetSymbolInfo(Root, CancellationToken).Symbol;
        if(method == null) {
          return;
        }
        var primitive = GetUsedSynchronizationPrimitive(method);
        if(primitive != null && !IsConstructingNamedPrimitive(method)) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, Root.GetLocation(), primitive.FatType, primitive.SlimType));
        }
      }

      private SynchronizationPrimitive? GetUsedSynchronizationPrimitive(IMethodSymbol method) {
        return SynchronizationPrimitives.WithCancellation(CancellationToken)
          .SingleOrDefault(primitive => IsSynchronizationPrimitive(method, primitive));
      }

      private bool IsSynchronizationPrimitive(IMethodSymbol method, SynchronizationPrimitive primitive) {
        return SemanticModel.IsEqualType(method.ContainingType, primitive.FatType);
      }

      private bool IsConstructingNamedPrimitive(IMethodSymbol method) {
        return method.Parameters.WithCancellation(CancellationToken)
          .Select(parameter => parameter.Type)
          .Any(type => type.SpecialType == SpecialType.System_String);
      }
    }

    private class SynchronizationPrimitive {
      public string FatType { get; }
      public string SlimType { get; }

      public SynchronizationPrimitive(string fatType, string slimType) {
        FatType = fatType;
        SlimType = slimType;
      }
    }
  }
}
