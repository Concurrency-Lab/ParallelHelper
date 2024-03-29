﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Immutable;

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

    private static readonly ClassMemberDescriptor[] TaskStartMethods = {
      new ClassMemberDescriptor("System.Threading.Tasks.Task", "Run"),
      new ClassMemberDescriptor("System.Threading.Tasks.TaskFactory", "StartNew")
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

    private class Analyzer : InternalAnalyzerBase<MethodDeclarationSyntax> {
      private readonly TaskAnalysis _taskAnalysis;

      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper(context)) {
        _taskAnalysis = new TaskAnalysis(context.SemanticModel, context.CancellationToken);
      }

      private bool IsReportOverridesEnabled => Context.Options.GetConfig(Rule, "overrides", "ignore") == "report";

      public override void Analyze() {
        if(IsAsyncMethodWithTaskImplementation() && (IsReportOverridesEnabled || !IsInterfaceImplementationOrOverride())) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, Root.GetSignatureLocation()));
        }
      }

      private bool IsAsyncMethodWithTaskImplementation() {
        return HasMethodBody() && IsMethodWithAsyncSuffix() && ReturnsTaskObject() && ReturnsCpuBoundTask();
      }

      private bool HasMethodBody() {
        return Root.Body != null || Root.ExpressionBody != null;
      }

      private bool IsMethodWithAsyncSuffix() {
        return Root.Identifier.Text.EndsWith(AsyncSuffix);
      }

      private bool IsInterfaceImplementationOrOverride() {
        var method = SemanticModel.GetDeclaredSymbol(Root, CancellationToken);
        return method.IsOverride || method.IsInterfaceImplementation(CancellationToken);
      }

      private bool ReturnsTaskObject() {
        var returnType = SemanticModel.GetTypeInfo(Root.ReturnType, CancellationToken).Type;
        return returnType != null && _taskAnalysis.IsTaskType(returnType);
      }

      private bool ReturnsCpuBoundTask() {
        return GetMethodsReturnValue() is InvocationExpressionSyntax invocation && IsTaskStart(invocation);
      }

      private ExpressionSyntax? GetMethodsReturnValue() {
        if(Root.ExpressionBody != null) {
          return Root.ExpressionBody.Expression;
        }

        // TODO: Decide if really only one statement is allowed or any return statement must not return a manually created task.
        var statements = Root.Body.Statements;
        if(statements.Count != 1) {
          return null;
        }
        return (statements[0] as ReturnStatementSyntax)?.Expression;
      }

      private bool IsTaskStart(InvocationExpressionSyntax invocation) {
        return TaskStartMethods.AnyContainsInvokedMethod(SemanticModel, invocation, CancellationToken);
      }
    }
  }
}
