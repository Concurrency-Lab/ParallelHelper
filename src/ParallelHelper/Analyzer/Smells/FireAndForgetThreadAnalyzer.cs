﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Immutable;

namespace ParallelHelper.Analyzer.Smells {
  /// <summary>
  /// Analyzer that analyzes sources for the use of fire-and-forget threads.
  /// 
  /// <example>Illustrates a class with a method that starts a thread without making use of the thread object.
  /// <code>
  /// class Sample {
  ///   public void DoWork() {
  ///     new Thread(() => /* ... */)).Start();
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class FireAndForgetThreadAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S004";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Fire-and-Forget Threads";
    private static readonly LocalizableString MessageFormat = "The use of threads in a fire-and-forget manner is discouraged.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    private static readonly ClassMemberDescriptor[] ThreadStartMethods = {
      new ClassMemberDescriptor("System.Threading.Thread", "Start")
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeExpressionStatement, SyntaxKind.ExpressionStatement);
    }

    private static void AnalyzeExpressionStatement(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerBase<ExpressionStatementSyntax> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper(context)) { }

      public override void Analyze() {
        if(IsFireAndForgetThread()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, Root.GetLocation()));
        }
      }

      private bool IsFireAndForgetThread() {
        return Root.Expression is InvocationExpressionSyntax invocation
          && IsAccessingInlineConstructedInstance(invocation)
          && SemanticModel.GetSymbolInfo(invocation, CancellationToken).Symbol is IMethodSymbol method
          && IsThreadStartMethod(method);
      }

      private bool IsThreadStartMethod(IMethodSymbol method) {
        return ThreadStartMethods.AnyContainsMember(SemanticModel, method);
      }

      private static bool IsAccessingInlineConstructedInstance(InvocationExpressionSyntax invocationExpression) {
        return invocationExpression.Expression is MemberAccessExpressionSyntax memberAccess
          && memberAccess.Expression is ObjectCreationExpressionSyntax;
      }
    }
  }
}
