using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.Bugs {
  /// <summary>
  /// Analyzer that analyzes sources for the use of classes that only synchronize the write accesses to a collection.
  /// 
  /// <example>Illustrates a class with a method that only synchronizes the write-access to the collection.
  /// <code>
  /// class Sample {
  ///   private readonly object _syncObject = new object();
  ///   private readonly Dictionary&lt;int, string&gt; _dictionary = new Dictionary&lt;int, string&gt;();
  /// 
  ///   public void Add(int key, string value) {
  ///     lock(_syncObject) {
  ///       _dictionary.Add(key, value);
  ///     }
  ///   }
  ///   
  ///   public string Get(int key) {
  ///     return _dictionary[key];
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class UnsynchronizedCollectionAccessAnalyzer : DiagnosticAnalyzer {
    // TODO Support access through interfaces.
    public const string DiagnosticId = "PH_B003";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Unsynchronized Collection Access";
    private static readonly LocalizableString MessageFormat = "The access to the collection is not synchronized, although there are synchronized write-accesses.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    private static readonly CollectionTypeDescriptor[] collectionTypeDescriptors = {
      // TODO Properties
      new CollectionTypeDescriptor("System.Collections.Generic.Dictionary`2",
        new string[] {"ContainsKey", "ContainsValue", "TryGetValue"},
        new string[] {"Add", "Clear", "Remove", "TryAdd"}
      ),
      new CollectionTypeDescriptor("System.Collections.Generic.List`1",
        new string[] {
          "BinarySearch", "Contains", "ConvertAll", "CopyTo", "Exists", "Find", "FindAll", "FindIndex", "FindLast",
          "FindLastIndex", "ForEach", "GetRange", "IndexOf", "LastIndexOf", "ToArray", "TrueForAll"
        },
        new string[] {"Add", "AddRange", "Clear", "Remove", "RemoveAll", "RemoveAt", "RemoveRange", "Reverse", "Sort"}
      )
    };

    private static readonly SyntaxKind[] WritingUnaryOperators = {
      SyntaxKind.PlusPlusToken,
      SyntaxKind.MinusMinusToken
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSemanticModelAction(AnalyzeSemanticModel);
    }

    private static void AnalyzeSemanticModel(SemanticModelAnalysisContext context) {
      new Analyzer(context, GetAllFieldCollections(context)).Analyze();
    }

    private static IDictionary<IFieldSymbol, CollectionTypeDescriptor> GetAllFieldCollections(SemanticModelAnalysisContext context) {
      var cancellationToken = context.CancellationToken;
      var semanticModel = context.SemanticModel;
      var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);
      return root.DescendantNodesAndSelf()
        .WithCancellation(cancellationToken)
        .OfType<FieldDeclarationSyntax>()
        .WithCancellation(cancellationToken)
        .SelectMany(declaration => declaration.Declaration.Variables)
        .Select(field => (IFieldSymbol)semanticModel.GetDeclaredSymbol(field, cancellationToken))
        .Select(field => new { Field = field, Descriptor = GetCollectionTypeDescriptor(semanticModel, field) })
        .Where(entry => entry.Descriptor != null)
        .ToDictionary(e => e.Field, e => e.Descriptor);
    }

    private static CollectionTypeDescriptor? GetCollectionTypeDescriptor(SemanticModel semanticModel, IFieldSymbol field) {
      return collectionTypeDescriptors
        .Where(descriptor => semanticModel.IsEqualType(field.Type, descriptor.Type))
        .SingleOrDefault();
    }

    private class Analyzer : MonitorAwareSemanticModelAnalyzerWithSyntaxWalkerBase {
      private readonly IDictionary<IFieldSymbol, CollectionTypeDescriptor> _collectionTypeDescriptorPerField;
      private readonly IDictionary<IFieldSymbol, ISet<SyntaxNode>> _synchronizedWriteAccessesPerField;
      private readonly IDictionary<IFieldSymbol, ISet<SyntaxNode>> _unsynchronizedReadAccessesPerField;

      public Analyzer(SemanticModelAnalysisContext context,
          IDictionary<IFieldSymbol, CollectionTypeDescriptor> collectionTypeDescriptorPerField) : base(context) {
        _collectionTypeDescriptorPerField = collectionTypeDescriptorPerField;
        _synchronizedWriteAccessesPerField = CreateFieldCollection(collectionTypeDescriptorPerField.Keys);
        _unsynchronizedReadAccessesPerField = CreateFieldCollection(collectionTypeDescriptorPerField.Keys);
      }

      private static IDictionary<IFieldSymbol, ISet<SyntaxNode>> CreateFieldCollection(IEnumerable<IFieldSymbol> collectionTypedFields) {
        return collectionTypedFields.ToDictionary(field => field, field => (ISet<SyntaxNode>)new HashSet<SyntaxNode>());
      }

      public override void Analyze() {
        if(_collectionTypeDescriptorPerField.Count == 0) {
          return;
        }
        base.Analyze();
        foreach(var readAccess in GetAllUnsynchronizedReadAccessesToCollectionsWithSynchronizedWriteAccesses()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, readAccess.GetLocation()));
        }
      }

      private IEnumerable<SyntaxNode> GetAllUnsynchronizedReadAccessesToCollectionsWithSynchronizedWriteAccesses() {
        return _unsynchronizedReadAccessesPerField
          .Where(entry => _synchronizedWriteAccessesPerField[entry.Key].Count > 0)
          .SelectMany(entry => entry.Value);
      }

      public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node) {
      }

      public override void VisitMethodDeclaration(MethodDeclarationSyntax node) {
        if(node.Modifiers.Any(SyntaxKind.PublicKeyword)) {
          base.VisitMethodDeclaration(node);
        }
      }

      public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node) {
        if(node.Modifiers.Any(SyntaxKind.PublicKeyword)) {
          base.VisitPropertyDeclaration(node);
        }
      }

      public override void VisitIndexerDeclaration(IndexerDeclarationSyntax node) {
        if(node.Modifiers.Any(SyntaxKind.PublicKeyword)) {
          base.VisitIndexerDeclaration(node);
        }
      }

      public override void VisitInvocationExpression(InvocationExpressionSyntax node) {
        if(node.Expression is MemberAccessExpressionSyntax memberAccess
            && SemanticModel.GetSymbolInfo(memberAccess.Expression, CancellationToken).Symbol is IFieldSymbol field) {
          TrackPossibleCollectionFieldAccess(field, node);
        }
        base.VisitInvocationExpression(node);
      }

      public override void VisitElementAccessExpression(ElementAccessExpressionSyntax node) {
        if(SemanticModel.GetSymbolInfo(node.Expression, CancellationToken).Symbol is IFieldSymbol field
            && _collectionTypeDescriptorPerField.ContainsKey(field)) {
          if(IsInsideLock) {
            if(IsWriteAccess(node.Parent)) {
              _synchronizedWriteAccessesPerField[field].Add(node.Parent);
            }
          } else {
            if(IsReadAccess(node.Parent)) {
              _unsynchronizedReadAccessesPerField[field].Add(node);
            }
          }
        }
        base.VisitElementAccessExpression(node);
      }

      private bool IsWriteAccess(SyntaxNode expression) {
        return expression is AssignmentExpressionSyntax
          || (expression is PrefixUnaryExpressionSyntax prefix && IsWritingUnaryOperator(prefix.OperatorToken))
          || (expression is PostfixUnaryExpressionSyntax postfix && IsWritingUnaryOperator(postfix.OperatorToken));
      }

      private bool IsReadAccess(SyntaxNode expression) {
        return !(expression is AssignmentExpressionSyntax assignment 
          && (assignment.Right != expression || assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)));
      }

      private static bool IsWritingUnaryOperator(SyntaxToken op) {
        return WritingUnaryOperators.Any(w => op.IsKind(w));
      }

      private void TrackPossibleCollectionFieldAccess(IFieldSymbol field, InvocationExpressionSyntax invocation) {
        if(_collectionTypeDescriptorPerField.TryGetValue(field, out var descriptor)
            && SemanticModel.GetSymbolInfo(invocation, CancellationToken).Symbol is IMethodSymbol method) {
          if(IsInsideLock) {
            if(descriptor.IsWriteMethod(method)) {
              _synchronizedWriteAccessesPerField[field].Add(invocation);
            }
          } else {
            if(descriptor.IsReadMethod(method)) {
              _unsynchronizedReadAccessesPerField[field].Add(invocation);
            }
          }
        }
      }
    }

    private class CollectionTypeDescriptor {
      public string Type { get; }
      public ISet<string> ReadMethods { get; }
      public ISet<string> WriteMethods { get; }

      public CollectionTypeDescriptor(string type, IReadOnlyCollection<string> readMethods, IReadOnlyCollection<string> writeMethods) {
        Type = type;
        ReadMethods = readMethods.ToImmutableHashSet();
        WriteMethods = writeMethods.ToImmutableHashSet();
      }

      public bool IsReadMethod(IMethodSymbol method) {
        return ReadMethods.Contains(method.Name);
      }

      public bool IsWriteMethod(IMethodSymbol method) {
        return WriteMethods.Contains(method.Name);
      }
    }
  }
}
