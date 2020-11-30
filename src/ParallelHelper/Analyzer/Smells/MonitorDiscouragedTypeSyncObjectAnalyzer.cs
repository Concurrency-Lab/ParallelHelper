using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.Smells {
  /// <summary>
  /// Analyzer that analyzes sources for the use locks that access fields that reference discouraged types such as integers or strings.
  /// 
  /// <example>Illustrates a class with a method that uses a member field for the synchronization whose value is a boxed type.
  /// <code>
  /// class Sample {
  ///   private object syncObject = 1;
  ///   
  ///   public void DoWork() {
  ///     lock(syncObject) {
  ///     }
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class MonitorDiscouragedTypeSyncObjectAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S002";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Monitor Discouraced Sync Object";
    private static readonly LocalizableString MessageFormat = "The synchronization object's potential type '{0}' is discouraged for monitor synchronization.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    private static readonly SpecialType[] DiscouragedTypes = {
      SpecialType.System_Byte,
      SpecialType.System_SByte,

      SpecialType.System_Char,
      SpecialType.System_String,

      SpecialType.System_UInt16,
      SpecialType.System_UInt32,
      SpecialType.System_UInt64,

      SpecialType.System_Int16,
      SpecialType.System_Int32,
      SpecialType.System_Int64,

      SpecialType.System_Decimal,
      SpecialType.System_Single,
      SpecialType.System_Double,

      SpecialType.System_Boolean
    };

    private static readonly string[] DiscouragedTypeNames = Array.Empty<string>();

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSemanticModelAction(AnalyzeSemanticModel);
    }

    private static void AnalyzeSemanticModel(SemanticModelAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerBase<SyntaxNode> {
      public Analyzer(SemanticModelAnalysisContext context) : base(new SemanticModelAnalysisContextWrapper(context)) { }

      public override void Analyze() {
        foreach(var statement in GetLockStatementsInside(Root)) {
          AnalyzeLockStatement(statement);
        }
      }

      private void AnalyzeLockStatement(LockStatementSyntax lockStatement) {
        var syncObject = lockStatement.Expression;
        var discouragedType = TryGetFirstDiscouragedType(syncObject);
        if(discouragedType != null) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, syncObject.GetLocation(), discouragedType.Name));
        }
      }

      private ITypeSymbol? TryGetFirstDiscouragedType(SyntaxNode node) {
        return GetPossibleTypes(node).FirstOrDefault(IsDiscouragedType);
      }

      private IEnumerable<ITypeSymbol> GetPossibleTypes(SyntaxNode node) {
        var expressionType = GetExpressionTypeAsEnumerable(node);
        var symbol = SemanticModel.GetSymbolInfo(node, CancellationToken).Symbol;
        if(symbol == null) {
          return expressionType;
        }
        return expressionType.Concat(GetInitializerTypes(symbol)).Concat(GetAssignedTypes(symbol));
      }

      private IEnumerable<ITypeSymbol> GetExpressionTypeAsEnumerable(SyntaxNode node) {
        var expressionType = SemanticModel.GetTypeInfo(node, CancellationToken).Type;
        if(expressionType != null) {
          yield return expressionType;
        }
      }

      private IEnumerable<ITypeSymbol> GetAssignedTypes(ISymbol symbol) {
        return Root.DescendantNodesAndSelf()
          .WithCancellation(CancellationToken)
          .OfType<AssignmentExpressionSyntax>()
          .Where(assignment => symbol.Equals(SemanticModel.GetSymbolInfo(assignment.Left, CancellationToken).Symbol, SymbolEqualityComparer.Default))
          .Select(assignment => SemanticModel.GetTypeInfo(assignment.Right, CancellationToken).Type)
          .IsNotNull();
      }

      private IEnumerable<ITypeSymbol> GetInitializerTypes(ISymbol symbol) {
        return GetAllDeclaratorsInsideTheCurrentSyntaxTree(symbol)
          .WithCancellation(CancellationToken)
          .Select(declarator => declarator.Initializer?.Value)
          .IsNotNull()
          .Select(expression => SemanticModel.GetTypeInfo(expression, CancellationToken).Type)
          .IsNotNull();
      }

      private IEnumerable<VariableDeclaratorSyntax> GetAllDeclaratorsInsideTheCurrentSyntaxTree(ISymbol symbol) {
        return SemanticModel.GetResolvableDeclaringSyntaxes(symbol, CancellationToken)
          .SelectMany(declaration => declaration.DescendantNodesAndSelf().OfType<VariableDeclaratorSyntax>());
      }

      private bool IsDiscouragedType(ITypeSymbol syncObjectType) {
        return IsDiscouragedSpecialType(syncObjectType) 
          || DiscouragedTypeNames.WithCancellation(CancellationToken).Any(type => SemanticModel.IsEqualType(syncObjectType, type));
      }

      private bool IsDiscouragedSpecialType(ITypeSymbol syncObjectType) {
        return DiscouragedTypes.WithCancellation(CancellationToken).Any(type => syncObjectType.SpecialType == type);
      }

      private IEnumerable<LockStatementSyntax> GetLockStatementsInside(SyntaxNode root) {
        return root.DescendantNodes()
          .WithCancellation(CancellationToken)
          .OfType<LockStatementSyntax>();
      }
    }
  }
}
