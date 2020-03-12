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
  /// Analyzer that analyzes sources for accidential leaks of collections inside monitor locks.
  /// 
  /// <example>Illustrates a class with a method synchronizes method accesses but returns the plain reference to the collection.
  /// <code>
  /// class Sample {
  ///   private readonly object syncObject = new object();
  ///   private readonly ISet&lt;string&gt; entries = new HashSet&lt;string&gt;();
  /// 
  ///   public ISet&lt;string&gt; GetAll() {
  ///     lock(syncObject) {
  ///       return entries;
  ///     }
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class LeakedOutboundCollectionAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S027";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Leaked Outbound Collection";
    private static readonly LocalizableString MessageFormat = "Returning a reference to the collection '{0}' allows unsynchronized accesses to it; return a copy of it instead.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    private static readonly string[] CollectionBaseTypes = {
      "System.Collections.ICollection",
      "System.Collections.Generic.ICollection`1",
    };

    private static readonly string[] SafeCollectionBaseTypes = {
      "System.Collections.Immutable.IImmutableDictionary",
      "System.Collections.Immutable.IImmutableDictionary`2",
      "System.Collections.Immutable.IImmutableList",
      "System.Collections.Immutable.IImmutableList`1",
      "System.Collections.Immutable.IImmutableSet",
      "System.Collections.Immutable.IImmutableSet`1",
      "System.Collections.Immutable.IImmutableStack",
      "System.Collections.Immutable.IImmutableStack`1",
      "System.Collections.Immutable.IImmutableQueue",
      "System.Collections.Immutable.IImmutableQueue`1"
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSemanticModelAction(AnalyzeSemanticModel);
    }

    private static void AnalyzeSemanticModel(SemanticModelAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : MonitorAwareSemanticModelAnalyzerWithSyntaxWalkerBase {
      private ISet<IFieldSymbol>? _unsafeCollectionFields;

      public Analyzer(SemanticModelAnalysisContext context) : base(context) { }

      public override void Analyze() {
        foreach(var classDeclaration in GetClassDeclarations()) {
          AnalyzeClassDeclaration(classDeclaration);
        }
      }

      private void AnalyzeClassDeclaration(ClassDeclarationSyntax classDeclaration) {
        _unsafeCollectionFields = GetUnsafeCollectionFields(classDeclaration).ToImmutableHashSet();
        if(_unsafeCollectionFields.Count == 0) {
          return;
        }
        foreach(var member in classDeclaration.Members) {
          Visit(member);
        }
      }

      public override void VisitMethodDeclaration(MethodDeclarationSyntax node) {
        if(node.Modifiers.Any(SyntaxKind.PublicKeyword)) {
          base.VisitMethodDeclaration(node);
        }
      }

      public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node) {
        if (node.Modifiers.Any(SyntaxKind.PublicKeyword)) {
          base.VisitPropertyDeclaration(node);
        }
      }

      public override void VisitAccessorDeclaration(AccessorDeclarationSyntax node) {
        // Since it is required that the enclosing property declaration is public,
        // only more restrictive modifiers may be used here.
        if(!node.Modifiers.Any()) {
          base.VisitAccessorDeclaration(node);
        }
      }

      public override void VisitReturnStatement(ReturnStatementSyntax node) {
        var field = TryGetFieldSymbol(node.Expression);
        if (field != null && IsInsideLock && _unsafeCollectionFields!.Contains(field)) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, node.Expression.GetLocation(), field.Name));
        }
      }

      private IFieldSymbol? TryGetFieldSymbol(ExpressionSyntax? expression) {
        if(expression == null) {
          return null;
        }
        return SemanticModel.GetSymbolInfo(expression, CancellationToken).Symbol as IFieldSymbol;
      }

      private IEnumerable<ClassDeclarationSyntax> GetClassDeclarations() {
        return Root.DescendantNodesAndSelf()
          .WithCancellation(CancellationToken)
          .OfType<ClassDeclarationSyntax>();
      }

      private IEnumerable<IFieldSymbol> GetUnsafeCollectionFields(ClassDeclarationSyntax classDeclaration) {
        return classDeclaration.Members.WithCancellation(CancellationToken)
          .OfType<FieldDeclarationSyntax>()
          .SelectMany(declaration => declaration.Declaration.Variables)
          .Select(field => (IFieldSymbol)SemanticModel.GetDeclaredSymbol(field, CancellationToken))
          .Where(field => IsUnsafeCollection(field.Type));
      }

      private bool IsUnsafeCollection(ITypeSymbol type) {
        return !IsAssignableToAnyOf(type, SafeCollectionBaseTypes)
          && IsAssignableToAnyOf(type, CollectionBaseTypes);
      }

      private bool IsAssignableToAnyOf(ITypeSymbol type, string[] targetTypes) {
        return targetTypes
          .SelectMany(targetType => SemanticModel.GetTypesByName(targetType))
          .Any(targetType => type.AllInterfaces.Any(type => type.IsEqualType(targetType)));
      }

      public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) {
        // Lambdas start a new activation frame.
      }

      public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) {
        // Lambdas start a new activation frame.
      }

      public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node) {
        // Anonymous methods start a new activation frame.
      }

      public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node) {
        // Local functions start a new activation frame.
      }

      public override void VisitClassDeclaration(ClassDeclarationSyntax node) {
        // Each class is analyzed individually.
      }
    }
  }
}
