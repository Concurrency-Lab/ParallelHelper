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
  /// Analyzer that analyzes sources for methods that receive a <see cref="System.Threading.CancellationToken"/> but do not
  /// pass it to all invocations where possible.
  /// 
  /// <example>Illustrates a class with a method that receives a cancellation token but does not pass it further.
  /// <code>
  /// using System.Threading;
  /// 
  /// class Sample {
  ///   public async Task DoWorkAsync1(CancellationToken cancellationToken = default) {
  ///     await DoWorkAsync2();
  ///   }
  ///   
  ///   public Task DoWorkAsync2(CancellationToken cancellationToken = default) {
  ///     return Task.CompletedTask;
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class PassCancellationTokenWherePossibleAnalyzer : DiagnosticAnalyzer {
    // TODO Probably obsolete due to the analyzer UnusedCancellationTokenFromEnclosingScopeAnalyzer.
    public const string DiagnosticId = "PH_P004";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "CancellationToken not Passed Through";
    private static readonly LocalizableString MessageFormat = "The current method receives a cancellation token but does not pass it to this invocation.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    private const string CancellationTokenType = "System.Threading.CancellationToken";

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : SyntaxNodeAnalyzerBase<MethodDeclarationSyntax> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(context) { }

      public override void Analyze() {
        if(ReceivesCancellationToken()) {
          foreach (var invocation in GetAllInvocationsWithMissingCancellationToken()) {
            Context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
          }
        }
      }

      private bool ReceivesCancellationToken() {
        return Node.ParameterList.Parameters
          .WithCancellation(CancellationToken)
          .Select(parameter => SemanticModel.GetDeclaredSymbol(parameter, CancellationToken))
          .Select(parameter => parameter.Type)
          .IsNotNull()
          .Any(type => SemanticModel.IsEqualType(type, CancellationTokenType));
      }

      private IEnumerable<InvocationExpressionSyntax> GetAllInvocationsWithMissingCancellationToken() {
        return Node.DescendantNodes()
          .WithCancellation(CancellationToken)
          .OfType<InvocationExpressionSyntax>()
          .Where(InvocationMissesCancellationToken);
      }

      private bool InvocationMissesCancellationToken(InvocationExpressionSyntax invocation) {
        return SemanticModel.GetSymbolInfo(invocation, CancellationToken).Symbol is IMethodSymbol method
          && !InvocationUsesCancellationToken(invocation)
          && (MethodAcceptsCancellationToken(method) || MethodHasOverloadThatAcceptsCancellationToken(method));
      }

      private bool InvocationUsesCancellationToken(InvocationExpressionSyntax invocation) {
        return invocation.ArgumentList.Arguments
          .WithCancellation(CancellationToken)
          .Select(argument => SemanticModel.GetTypeInfo(argument.Expression, CancellationToken).Type)
          .IsNotNull()
          .Where(type => SemanticModel.IsEqualType(type, CancellationTokenType))
          .Any();
      }

      private bool MethodHasOverloadThatAcceptsCancellationToken(IMethodSymbol method) {
        return method.GetAllOverloads(CancellationToken).Any(MethodAcceptsCancellationToken);
      }

      private bool MethodAcceptsCancellationToken(IMethodSymbol method) {
        return method.Parameters
          .WithCancellation(CancellationToken)
          .Where(parameter => SemanticModel.IsEqualType(parameter.Type, CancellationTokenType))
          .Any();
      }
    }
  }
}
