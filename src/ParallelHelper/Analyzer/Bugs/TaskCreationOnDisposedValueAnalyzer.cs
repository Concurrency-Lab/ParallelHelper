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
  /// Analyzer that analyzes sources returned tasks that are created on a value that will be disposed.
  /// 
  /// <example>Illustrates a class with a method that uses an entity framework database context to execute
  /// a query asynchronously.
  /// <code>
  /// class Sample {
  ///   public Task AddAsync(Entity entity) {
  ///     using(var context = new DatabaseContext()) {
  ///       return context.AddAsync(entity);
  ///     }
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class TaskCreationOnDisposedValueAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_B011";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Returning a Task based on a Disposed Value";
    private static readonly LocalizableString MessageFormat = "The enclosing using-statement disposes the object before the execution of the returned task. Await the asynchronous method instead.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    private static readonly string[] TaskTypes = {
      "System.Threading.Tasks.Task",
      "System.Threading.Tasks.Task`1"
    };

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeStatement, SyntaxKind.UsingStatement, SyntaxKind.LocalDeclarationStatement);
    }

    private static void AnalyzeStatement(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerBase<StatementSyntax> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper<StatementSyntax>(context)) { }

      public override void Analyze() {
        foreach(var returnStatement in GetReturnStatementsReturningTasksDependingOnVariableFromUsingStatement()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, returnStatement.GetLocation()));
        }
      }

      private IEnumerable<ReturnStatementSyntax> GetReturnStatementsReturningTasksDependingOnVariableFromUsingStatement() {
        if (Root is UsingStatementSyntax usingStatement) {
          var disposedVariables = GetDisposedVariablesFromUsingStatement(usingStatement).ToImmutableHashSet();
          return GetReturnStatementsReturningTasksDependingOnVariable(Root, disposedVariables);
        } else if(Root is LocalDeclarationStatementSyntax localDeclaration) {
          var disposedVariables = GetDisposedVariablesFromLocalDeclaration(localDeclaration).ToImmutableHashSet();
          return GetReturnStatementsReturningTasksDependingOnVariable(Root.Parent, disposedVariables);
        }
        return Enumerable.Empty<ReturnStatementSyntax>();
      }

      private IEnumerable<ISymbol> GetDisposedVariablesFromUsingStatement(UsingStatementSyntax usingStatement) {
        return GetDisposedVariablesFromUsingStatementDeclaration(usingStatement)
          .Concat(GetDisposedVariablesFromUsingStatementExpression(usingStatement));
      }

      private IEnumerable<ISymbol> GetDisposedVariablesFromUsingStatementDeclaration(UsingStatementSyntax usingStatement) {
        return GetVariablesFromVariableDeclaration(usingStatement.Declaration);
      }

      private IEnumerable<ISymbol> GetDisposedVariablesFromLocalDeclaration(LocalDeclarationStatementSyntax localDeclaration) {
        if (!localDeclaration.UsingKeyword.IsKind(SyntaxKind.UsingKeyword)) {
          return Enumerable.Empty<ISymbol>();
        }
        return GetVariablesFromVariableDeclaration(localDeclaration.Declaration);
      }

      private IEnumerable<ISymbol> GetVariablesFromVariableDeclaration(VariableDeclarationSyntax? declaration) {
        if (declaration == null) {
          return Enumerable.Empty<ISymbol>();
        }
        return declaration.Variables
          .WithCancellation(CancellationToken)
          .Select(declarator => SemanticModel.GetDeclaredSymbol(declarator, CancellationToken));
      }

      private IEnumerable<ISymbol> GetDisposedVariablesFromUsingStatementExpression(UsingStatementSyntax usingStatement) {
        var expression = usingStatement.Expression;
        if(expression is AssignmentExpressionSyntax assignment) {
          expression = assignment.Left;
        }
        if (expression == null) {
          return Enumerable.Empty<ISymbol>();
        }
        var symbol = SemanticModel.GetSymbolInfo(expression, CancellationToken).Symbol;
        return symbol == null ? Enumerable.Empty<ISymbol>() : new ISymbol[] { symbol };
      }

      private IEnumerable<ReturnStatementSyntax> GetReturnStatementsReturningTasksDependingOnVariable(SyntaxNode root, ISet<ISymbol> disposedVariables) {
        return root.DescendantNodes()
          .WithCancellation(CancellationToken)
          .OfType<ReturnStatementSyntax>()
          .Where(returnStatement => returnStatement.Expression != null)
          .Where(returnStatement => IsTaskTyped(returnStatement.Expression))
          .Where(returnStatement => IsExpressionUsingMemberAccessOnAnyVariable(returnStatement.Expression, disposedVariables));
      }

      private bool IsTaskTyped(ExpressionSyntax expression) {
        var type = SemanticModel.GetTypeInfo(expression, CancellationToken).Type;
        return type != null && TaskTypes.Any(typeName => SemanticModel.IsEqualType(type, typeName));
      }

      private bool IsExpressionUsingMemberAccessOnAnyVariable(ExpressionSyntax expression, ISet<ISymbol> disposedVariables) {
        ExpressionSyntax? currentExpression = expression;
        while(currentExpression != null) {
          switch (currentExpression) {
          case IdentifierNameSyntax identifier:
            return IsIdentifierReferencingAnyVariable(identifier, disposedVariables);
          case InvocationExpressionSyntax invocation:
            currentExpression = invocation.Expression;
            break;
          case MemberAccessExpressionSyntax memberAccess:
            currentExpression = memberAccess.Expression;
            break;
          default:
            return false;
          }
        }
        return false;
      }
      
      private bool IsIdentifierReferencingAnyVariable(IdentifierNameSyntax identifier, ISet<ISymbol> disposedVariables) {
        var symbol = SemanticModel.GetSymbolInfo(identifier, CancellationToken).Symbol;
        return symbol != null && disposedVariables.Contains(symbol);
      }
    }
  }
}
