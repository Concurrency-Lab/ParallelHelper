using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.Bugs {
  /// <summary>
  /// Analyzer that analyzes sources for the use of concurrent collections such as
  /// <see cref="System.Collections.Concurrent.ConcurrentQueue{T}"/> that query the state
  /// inside a condition of a branch statement and execute an action in the statement.
  /// 
  /// <example>Illustrates the non-atomic increment on a <c>volatile</c> field.
  /// <code>
  /// class Sample {
  ///   private readonly ConcurrentQueue&lt;string&gt; entries = new ConcurrentQueue&lt;string&gt;();
  ///   
  ///   public void DequeueAll() {
  ///     while(!entries.IsEmpty) {
  ///       entries.TryDequeue(out var _);
  ///     }
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class StateQueryFollowedByActionInConcurrentCollectionAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_B007";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Non-Atomic Access to Concurrent Collection";
    private static readonly LocalizableString MessageFormat = "The collection access to '{0}' appears to be a race-condition since it depends on a previous state by the query in the enclosing if-statement.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    private static readonly CollectionDescriptor[] CollectionDescriptors = {
      new CollectionDescriptor(
        "System.Collections.Concurrent.BlockingCollection`1",
        new string[] { "Count" },
        new string[] { "Add", "Take", "TryAdd", "TryTake" }
      ),
      new CollectionDescriptor(
        "System.Collections.Concurrent.ConcurrentBag`1",
        new string[] { "Count", "IsEmpty" },
        new string[] { "Add", "TryPeek", "TryTake" }
      ),
      new CollectionDescriptor(
        "System.Collections.Concurrent.ConcurrentDictionary`2",
        new string[] { "ContainsKey", "Count", "IsEmpty" },
        new string[] { "AddOrUpdate", "GetOrAdd", "TryAdd", "TryGetValue", "TryRemove", "TryUpdate" },
        true
      ),
      new CollectionDescriptor(
        "System.Collections.Concurrent.ConcurrentQueue`1",
        new string[] { "Count", "IsEmpty" },
        new string[] { "Enqueue", "TryDequeue", "TryPeek" }
      ),
      new CollectionDescriptor(
        "System.Collections.Concurrent.ConcurrentStack`1",
        new string[] { "Count", "IsEmpty" },
        new string[] { "Push", "PushRange", "TryPeek", "TryPop", "TryPopRange" }
      ),
    };

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSemanticModelAction(AnalyzeSemanticModel);
    }

    private static void AnalyzeSemanticModel(SemanticModelAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerBase<SyntaxNode> {
      public Analyzer(SemanticModelAnalysisContext context) : base(new SemanticModelAnalysisContextWrapper(context)) {
      }

      public override void Analyze() {
        foreach(var (condition, body) in GetAllBranchStatements()) {
          AnalyzeBranchStatement(condition, body);
        }
      }

      private void AnalyzeBranchStatement(ExpressionSyntax condition, StatementSyntax statement) {
        foreach(var queriedField in GetAllStateQueriedConcurrentCollectionFields(condition)) {
          foreach(var actionOnQueriedField in GetAllActionsOnFieldInside(queriedField, statement)) {
            Context.ReportDiagnostic(Diagnostic.Create(Rule, actionOnQueriedField.GetLocation(), queriedField.Name));
          }
        }
      }

      private IEnumerable<(ExpressionSyntax Condition, StatementSyntax Statement)> GetAllBranchStatements() {
        var ifStatements = Root.DescendantNodesAndSelf()
          .WithCancellation(CancellationToken)
          .OfType<IfStatementSyntax>()
          .Select(statement => (statement.Condition, statement.Statement));
        var whileStatements = Root.DescendantNodesAndSelf()
          .WithCancellation(CancellationToken)
          .OfType<WhileStatementSyntax>()
          .Select(statement => (statement.Condition, statement.Statement));
        var forStatements = Root.DescendantNodesAndSelf()
          .WithCancellation(CancellationToken)
          .OfType<ForStatementSyntax>()
          .Select(statement => (statement.Condition, statement.Statement));
        return ifStatements
          .Concat(whileStatements)
          .Concat(forStatements)
          .Where(entry => entry.Condition != null && entry.Statement != null);
      }

      private ISet<IFieldSymbol> GetAllStateQueriedConcurrentCollectionFields(SyntaxNode node) {
        return node.DescendantNodesAndSelf()
          .WithCancellation(CancellationToken)
          .OfType<MemberAccessExpressionSyntax>()
          .Where(IsStateQueryOnConcurrentCollection)
          .Select(memberAccess => SemanticModel.GetSymbolInfo(memberAccess.Expression, CancellationToken).Symbol)
          .OfType<IFieldSymbol>()
          .ToImmutableHashSet();
      }
      
      private bool IsStateQueryOnConcurrentCollection(MemberAccessExpressionSyntax memberAccess) {
        // TODO Check that member methods are actually invoked?
        var symbol = SemanticModel.GetSymbolInfo(memberAccess, CancellationToken).Symbol;
        return symbol != null && CollectionDescriptors
          .WithCancellation(CancellationToken)
          .Where(descriptor => symbol.ContainingType != null && SemanticModel.IsEqualType(symbol.ContainingType, descriptor.Type))
          .Any(descriptor => descriptor.QueryMembers.Contains(symbol.Name));
      }

      private IEnumerable<SyntaxNode> GetAllActionsOnFieldInside(IFieldSymbol field, SyntaxNode node) {
        return GetAllActionInvocationsOnFieldInside(field, node)
          .Cast<SyntaxNode>()
          .Concat(GetAllIndexerAccessesOnField(field, node));
      }

      private IEnumerable<InvocationExpressionSyntax> GetAllActionInvocationsOnFieldInside(IFieldSymbol field, SyntaxNode node) {
        return node.DescendantNodesAndSelf()
          .WithCancellation(CancellationToken)
          .OfType<InvocationExpressionSyntax>()
          .Where(invocation => IsMemberInvocationOfField(invocation, field))
          .Where(IsActionOnConcurrentCollection);
      }

      private IEnumerable<ElementAccessExpressionSyntax> GetAllIndexerAccessesOnField(IFieldSymbol field, SyntaxNode node) {
        return node.DescendantNodesAndSelf()
          .WithCancellation(CancellationToken)
          .OfType<ElementAccessExpressionSyntax>()
          .Where(IsElementAccessOnConcurrentCollectionWithIndexer)
          .Where(elementAccess => field.Equals(SemanticModel.GetSymbolInfo(elementAccess.Expression, CancellationToken).Symbol, SymbolEqualityComparer.Default));
      }

      private bool IsMemberInvocationOfField(InvocationExpressionSyntax invocation, IFieldSymbol field) {
        return invocation.Expression is MemberAccessExpressionSyntax memberAccess
          && field.Equals(SemanticModel.GetSymbolInfo(memberAccess.Expression, CancellationToken).Symbol, SymbolEqualityComparer.Default);
      }

      private bool IsActionOnConcurrentCollection(InvocationExpressionSyntax invocation) {
        return SemanticModel.GetSymbolInfo(invocation, CancellationToken).Symbol is IMethodSymbol method
          && CollectionDescriptors
          .WithCancellation(CancellationToken)
          .Where(descriptor => SemanticModel.IsEqualType(method.ContainingType, descriptor.Type))
          .Any(descriptor => descriptor.ActionMembers.Contains(method.Name));
      }

      private bool IsElementAccessOnConcurrentCollectionWithIndexer(ElementAccessExpressionSyntax elementAccess) {
        var symbol = SemanticModel.GetSymbolInfo(elementAccess, CancellationToken).Symbol;
        return symbol != null
          && CollectionDescriptors
          .WithCancellation(CancellationToken)
          .Where(descriptor => SemanticModel.IsEqualType(symbol.ContainingType, descriptor.Type))
          .Any(descriptor => descriptor.HasIndexer);
      }
    }

    private class CollectionDescriptor {
      public string Type { get; }
      public ISet<string> QueryMembers { get; }
      public ISet<string> ActionMembers { get; }
      public bool HasIndexer { get; }

      public CollectionDescriptor(string type, IReadOnlyList<string> queryMembers,
          IReadOnlyList<string> actionMembers, bool hasIndexer = false) {
        Type = type;
        QueryMembers = queryMembers.ToImmutableHashSet();
        ActionMembers = actionMembers.ToImmutableHashSet();
        HasIndexer = hasIndexer;
      }
    }
  }
}
