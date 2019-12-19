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
  /// Analyzer that analyzes sources for the use of non-atomic read/write operations on <c>volatile</c> fields.
  /// 
  /// <example>Illustrates the non-atomic increment on a <c>volatile</c> field.
  /// <code>
  /// class Sample {
  ///   private volatile int count;
  ///   
  ///   public void Increment() {
  ///     count++;
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class NonAtomicReadWriteOnVolatileAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_B006";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Non-Atomic Read/Write on Volatile Field";
    private static readonly LocalizableString MessageFormat = "The use us non-atomic read/write operations leads to race-conditions when accessed from multiple threads.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSemanticModelAction(AnalyzeSemanticModel);
    }

    private static void AnalyzeSemanticModel(SemanticModelAnalysisContext context) {
      new Analyzer(context, GetAllVolatileFields(context)).Analyze();
    }

    private static ISet<IFieldSymbol> GetAllVolatileFields(SemanticModelAnalysisContext context) {
      var cancellationToken = context.CancellationToken;
      var semanticModel = context.SemanticModel;
      return semanticModel.SyntaxTree.GetRoot(cancellationToken)
        .DescendantNodesAndSelf()
        .WithCancellation(cancellationToken)
        .OfType<FieldDeclarationSyntax>()
        .Where(declaration => declaration.Modifiers.Any(SyntaxKind.VolatileKeyword))
        .SelectMany(declaration => declaration.Declaration.Variables)
        .Select(variable => (IFieldSymbol)semanticModel.GetDeclaredSymbol(variable, cancellationToken))
        .IsNotNull()
        .ToImmutableHashSet();
    }

    private class Analyzer : MonitorAwareSemanticModelAnalyzerWithSyntaxWalkerBase {
      private readonly ISet<IFieldSymbol> _volatileFields;

      public Analyzer(SemanticModelAnalysisContext context, ISet<IFieldSymbol> volatileFields) : base(context) {
        _volatileFields = volatileFields;
      }

      public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node) {
        // Assume that there are no threads created inside a constructor, thus any read/write operation
        // will not run concurrently.
      }

      public override void VisitMethodDeclaration(MethodDeclarationSyntax node) {
        if(node.Modifiers.Any(SyntaxKind.PublicKeyword)) {
          // Only analyze public members. Private members (and possibly others) might
          // be invoked through other methods which incorporate synchronization mechanisms.
          base.VisitMethodDeclaration(node);
        }
      }

      public override void VisitLockStatement(LockStatementSyntax node) {
        // Anything that is inside a synchronized block is most-likely safe.
      }

      public override void VisitAssignmentExpression(AssignmentExpressionSyntax node) {
        if(IsReadWriteAssignment(node)) {
          AnalyzeReadWriteAccess(node.Left, node.GetLocation());
        }
        base.VisitAssignmentExpression(node);
      }

      private bool IsReadWriteAssignment(AssignmentExpressionSyntax assignment) {
        return !assignment.IsKind(SyntaxKind.SimpleAssignmentExpression);
      }

      public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node) {
        if(IsReadWriteUnaryOperator(node.OperatorToken)) {
          AnalyzeReadWriteAccess(node.Operand, node.GetLocation());
        }
        base.VisitPostfixUnaryExpression(node);
      }

      public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node) {
        if(IsReadWriteUnaryOperator(node.OperatorToken)) {
          AnalyzeReadWriteAccess(node.Operand, node.GetLocation());
        }
        base.VisitPrefixUnaryExpression(node);
      }

      private bool IsReadWriteUnaryOperator(SyntaxToken token) {
        return token.IsKind(SyntaxKind.PlusPlusToken) || token.IsKind(SyntaxKind.MinusMinusToken);
      }

      private void AnalyzeReadWriteAccess(ExpressionSyntax accessedVariable, Location location) {
        if(IsVolatileField(accessedVariable)) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, location));
        }
      }

      private bool IsVolatileField(ExpressionSyntax expression) {
        return SemanticModel.GetSymbolInfo(expression, CancellationToken).Symbol is IFieldSymbol field
          && _volatileFields.Contains(field);
      }
    }
  }
}
