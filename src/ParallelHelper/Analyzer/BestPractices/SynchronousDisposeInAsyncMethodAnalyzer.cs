using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.BestPractices {
  /// <summary>
  /// Analyzer that analyzes sources for the use of a synchronous dispose in an asynchronous method.
  /// 
  /// <example>Illustrates a class that uses a conventional dispose with the using statement instead of an asynchronous.
  /// <code>
  /// class Sample {
  ///   public async Task&lt;string&gt; ReadFileAsync(string filePath) {
  ///     using(var input = File.Read(filePath)) {
  ///       // ...
  ///     }
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class SynchronousDisposeInAsyncMethodAnalyzer : DiagnosticAnalyzer {
    // TODO Using declaration.
    // TODO Search for DisposeAsync implementation?
    public const string DiagnosticId = "PH_P009";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Synchronous Dispose in Async Method";
    private static readonly LocalizableString MessageFormat = "The resource is disposed synchronously, although it can be disposed asynchronously be preceding the using-statement with 'await'.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    private const string AsyncDisposableType = "System.IAsyncDisposable";
    
    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.MethodDeclaration, SyntaxKind.AnonymousMethodExpression,
        SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : SyntaxNodeAnalyzerBase<SyntaxNode> {
      private bool IsAsyncMethod => Node is MethodDeclarationSyntax method
        && method.Modifiers.Any(SyntaxKind.AsyncKeyword);
      private bool IsAsyncAnonymousFunction => Node is AnonymousFunctionExpressionSyntax function
        && function.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);

      public Analyzer(SyntaxNodeAnalysisContext context) : base(context) { }

      public override void Analyze() {
        if (!IsAsyncMethod && !IsAsyncAnonymousFunction) {
          return;
        }
        foreach(var usingStatement in GetUsingStatementsThatCanBeAsynchronous()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, GetReportLocation(usingStatement)));
        }
      }

      private Location GetReportLocation(UsingStatementSyntax statement) {
        var start = statement.GetLocation().SourceSpan.Start;
        var end = statement.CloseParenToken.GetLocation().SourceSpan.End;
        return Location.Create(statement.SyntaxTree, TextSpan.FromBounds(start, end));
      }

      private IEnumerable<UsingStatementSyntax> GetUsingStatementsThatCanBeAsynchronous() {
        var asyncDisposableTypes = SemanticModel.GetTypesByName(AsyncDisposableType).ToArray();
        return Node.DescendantNodesInSameActivationFrame()
          .WithCancellation(CancellationToken)
          .OfType<UsingStatementSyntax>()
          .Where(statement => !statement.AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword))
          .Where(statement => AreAllDisposedVariablesAssignableToAnyTargetType(statement, asyncDisposableTypes));
      }

      private bool AreAllDisposedVariablesAssignableToAnyTargetType(UsingStatementSyntax usingStatement, IEnumerable<ITypeSymbol> targetTypes) {
        var disposedValues = GetTypesOfDisposedValues(usingStatement).ToArray();
        return disposedValues.Length > 0 
          && disposedValues.WithCancellation(CancellationToken)
            .All(variable => IsAssignableToAnyType(variable, targetTypes));
      }

      private IEnumerable<ITypeSymbol> GetTypesOfDisposedValues(UsingStatementSyntax usingStatement) {
        if(usingStatement.Expression != null) {
          return new[] { SemanticModel.GetTypeInfo(usingStatement.Expression, CancellationToken).Type };
        }
        if(usingStatement.Declaration != null) {
          return usingStatement.Declaration.Variables
            .WithCancellation(CancellationToken)
            .Select(variable => ((ILocalSymbol)SemanticModel.GetDeclaredSymbol(variable, CancellationToken)).Type);
        }
        return Enumerable.Empty<ITypeSymbol>();
      }

      private bool IsAssignableToAnyType(ITypeSymbol? type, IEnumerable<ITypeSymbol> targetTypes) {
        return type != null
          && targetTypes.WithCancellation(CancellationToken)
            .Any(targetType => SemanticModel.Compilation.ClassifyConversion(type, targetType).IsImplicit);
      }
    }
  }
}
