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
  /// Analyzer that analyzes sources for async methods without *Async suffix that have a counterpart with the *Async suffix.
  /// 
  /// <example>Illustrates a class with a method that uses the *Async suffix although the implementation is CPU-bound.
  /// <code>
  /// class Sample {
  ///   public Task DoWork() {
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
  public class AsyncMethodWithoutSuffixWithAsyncCounterpartAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S021";

    private const string Category = "Concurrency";

    private const string AsyncSuffix = "Async";

    private static readonly LocalizableString Title = "Async Naming Confusion";
    private static readonly LocalizableString MessageFormat = "The async method does not use the *Async suffix, although there is another method with an *Async suffix.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerBase<ClassDeclarationSyntax> {
      private readonly TaskAnalysis _taskAnalysis;

      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper(context)) {
        _taskAnalysis = new TaskAnalysis(context.SemanticModel, context.CancellationToken);
      }

      public override void Analyze() {
        foreach(var method in GetAsyncMethodsWithoutAsyncSuffixThatHaveCounterpartWithAsyncSuffix()){
          Context.ReportDiagnostic(Diagnostic.Create(Rule, method.GetSignatureLocation()));
        }
      }

      private IEnumerable<MethodDeclarationSyntax> GetAsyncMethodsWithoutAsyncSuffixThatHaveCounterpartWithAsyncSuffix() {
        var methods = GetAllMethods().ToArray();
        var methodsWithAsyncSuffix = methods.Where(IsMethodWithAsyncSuffix).ToArray();
        return methods.WithCancellation(CancellationToken)
          .Where(IsAsyncMethodWithoutAsyncSuffix)
          .Where(method => HasCounterpartWithAsyncSuffix(method, methodsWithAsyncSuffix));
      }

      private IEnumerable<MethodDeclarationSyntax> GetAllMethods() {
        return Root.Members.WithCancellation(CancellationToken).OfType<MethodDeclarationSyntax>();
      }

      private bool IsAsyncMethodWithoutAsyncSuffix(MethodDeclarationSyntax method) {
        return !IsMethodWithAsyncSuffix(method) && IsAsyncMethod(method);
      }

      private static bool IsMethodWithAsyncSuffix(MethodDeclarationSyntax method) {
        return method.Identifier.Text.EndsWith(AsyncSuffix);
      }

      private bool IsAsyncMethod(MethodDeclarationSyntax method) {
        return method.Modifiers.Any(SyntaxKind.AsyncKeyword) || ReturnsTaskObject(method);
      }

      private bool ReturnsTaskObject(MethodDeclarationSyntax method) {
        var returnType = SemanticModel.GetTypeInfo(method.ReturnType, CancellationToken).Type;
        return returnType != null && _taskAnalysis.IsTaskType(returnType);
      }

      private static bool HasCounterpartWithAsyncSuffix(MethodDeclarationSyntax method, IReadOnlyList<MethodDeclarationSyntax> methodsWithAsyncSuffix) {
        var methodNameWithAsyncSuffix = method.Identifier.Text + AsyncSuffix;
        return methodsWithAsyncSuffix
          .Select(method => method.Identifier.Text)
          .Any(methodNameWithAsyncSuffix.Equals);
      }
    }
  }
}
