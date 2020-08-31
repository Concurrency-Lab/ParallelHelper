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
  /// Analyzer that analyzes sources for cases where a method returns <see cref="IEnumerable{T}"/> inside a
  /// <see cref="System.Threading.Tasks.Task{TResult}"/> where returning an IAsyncEnumerable would be suitable.
  /// 
  /// <example>Illustrates a class that populates a <see cref="List{T}"/> and returns it inside a <see cref="System.Threading.Tasks.Task{TResult}"/>.
  /// <code>
  /// class Sample {
  ///   public async Task&lt;IEnumerable&lt;string&gt;&gt; Delayed() {
  ///     var result = new List&lt;string&gt;();
  ///     for(int i = 0; i &lt; 10; i++) {
  ///       await Task.Delay(100);
  ///       result.Add($"#{i}");
  ///     }
  ///     return result;
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class ReplaceWithAsyncGeneratorAnalyzer : DiagnosticAnalyzer {
    // TODO other collection
    public const string DiagnosticId = "PH_P011";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Replace With Async Stream";
    private static readonly LocalizableString MessageFormat = "The current implementation makes the consumer await the whole result, although it appears that it can be replaced with an async stream.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    private const string TaskType = "System.Threading.Tasks.Task`1";
    private const string EnumerableType = "System.Collections.Generic.IEnumerable`1";
    private const string AsyncEnumerableType = "System.Collections.Generic.IAsyncEnumerable`1";
    private const string CollectionType = "System.Collections.Generic.ICollection`1";
    private const string AddMethod = "Add";

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

      private bool IsAsync => Node.Modifiers.Any(SyntaxKind.AsyncKeyword);

      public override void Analyze() {
        if (!IsAsync || !IsAsyncEnumerableTypeAvailable() || !ReturnsEnumerableInTask()) {
          return;
        }
        var collectionVariables = GetReturnedCollectionVariables().ToImmutableHashSet();
        if(ContainsLoopThatAsynchronouslyPopulatesAnyCollection(collectionVariables)) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, Node.GetSignatureLocation()));
        }
      }

      private bool IsAsyncEnumerableTypeAvailable() {
        return SemanticModel.GetTypesByName(AsyncEnumerableType).Any();
      }

      private bool ReturnsEnumerableInTask() {
        return Node.ReturnType is GenericNameSyntax genericType
          && IsEnumerableInTask(genericType);
      }

      private bool IsEnumerableInTask(GenericNameSyntax type) {
        if (type.TypeArgumentList.Arguments.Count != 1) {
          return false;
        }
        var returnType = SemanticModel.GetTypeInfo(type, CancellationToken).Type;
        var genericType = SemanticModel.GetTypeInfo(type.TypeArgumentList.Arguments[0], CancellationToken).Type;
        return SemanticModel.IsEqualType(returnType, TaskType)
          && SemanticModel.IsEqualType(genericType, EnumerableType);
      }

      private IEnumerable<ILocalSymbol> GetReturnedCollectionVariables() {
        return Node.DescendantNodesInSameActivationFrame()
          .WithCancellation(CancellationToken)
          .OfType<ReturnStatementSyntax>()
          .Select(statement => statement.Expression)
          .IsNotNull()
          .Select(expression => SemanticModel.GetSymbolInfo(expression, CancellationToken).Symbol)
          .OfType<ILocalSymbol>()
          .Where(IsCollectionTyped);
      }

      private bool IsCollectionTyped(ILocalSymbol symbol) {
        return symbol.Type.GetAllBaseTypesAndSelf()
          .WithCancellation(CancellationToken)
          .Any(ImplementsCollectionInterface);
      }

      private bool ImplementsCollectionInterface(ITypeSymbol type) {
        return SemanticModel.IsEqualType(type, CollectionType)
            || type.AllInterfaces.WithCancellation(CancellationToken).Any(interfaceType => SemanticModel.IsEqualType(interfaceType, CollectionType));
      }

      private bool ContainsLoopThatAsynchronouslyPopulatesAnyCollection(ISet<ILocalSymbol> collections) {
        return GetAsyncPopulationsOfAnyCollection(collections).Any();
      }

      private IEnumerable<SyntaxNode> GetAsyncPopulationsOfAnyCollection(ISet<ILocalSymbol> collections) {
        return GetLoopBodies()
          .Where(ContainsAwaitExpression)
          .Where(body => ContainsPopulationOfAnyCollection(body, collections));
      }

      private IEnumerable<SyntaxNode> GetLoopBodies() {
        return Node.DescendantNodesInSameActivationFrame()
          .WithCancellation(CancellationToken)
          .Select(TryGetLoopBody)
          .IsNotNull();
      }

      private StatementSyntax? TryGetLoopBody(SyntaxNode node) {
        return node switch
        {
          ForStatementSyntax loop => loop.Statement,
          WhileStatementSyntax loop => loop.Statement,
          DoStatementSyntax loop => loop.Statement,
          ForEachStatementSyntax loop => loop.Statement,
          _ => null
        };
      }

      private bool ContainsAwaitExpression(SyntaxNode node) {
        return node.DescendantNodesInSameActivationFrame()
          .WithCancellation(CancellationToken)
          .OfType<AwaitExpressionSyntax>()
          .Any();
      }

      private bool ContainsPopulationOfAnyCollection(SyntaxNode node, ISet<ILocalSymbol> collections) {
        return node.DescendantNodesInSameActivationFrame()
          .WithCancellation(CancellationToken)
          .OfType<InvocationExpressionSyntax>()
          .Any(invocation => IsPopulationOfAnyCollection(invocation, collections));
      }

      private bool IsPopulationOfAnyCollection(InvocationExpressionSyntax invocation, ISet<ILocalSymbol> collections) {
        var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
        if(memberAccess == null || memberAccess.Name.Identifier.Text != AddMethod) {
          return false;
        }
        return SemanticModel.GetSymbolInfo(memberAccess.Expression, CancellationToken).Symbol is ILocalSymbol variable
          && collections.Contains(variable);
      }
    }
  }
}
