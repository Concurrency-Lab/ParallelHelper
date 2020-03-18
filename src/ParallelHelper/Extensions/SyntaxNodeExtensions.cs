using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ParallelHelper.Extensions {
  /// <summary>
  /// Extension methods to work with syntax nodes.
  /// </summary>
  public static class SyntaxNodeExtensions {
    /// <summary>
    /// Returns all descendant nodes of the same scope. For example, if the provided node is a method,
    /// it will not return nodes of a lambda expression that is declared inside this method.
    /// </summary>
    /// <param name="node">The node to get the descendant nodes of.</param>
    /// <returns>The descendant nodes of the provided node.</returns>
    public static IEnumerable<SyntaxNode> DescendantNodesInSameActivationFrame(this SyntaxNode node) {
      return node.DescendantNodes(descendant => node == descendant || !IsNewActivationFrame(descendant));
    }

    private static bool IsNewActivationFrame(SyntaxNode node) {
      return node is BaseMethodDeclarationSyntax
        || node is AnonymousFunctionExpressionSyntax
        || node is LocalFunctionStatementSyntax;
    }

    /// <summary>
    /// Checks if the provided method declaration or anonymous function (e.g. lambdas) has
    /// an async modifier.
    /// </summary>
    /// <param name="node">The node to check if it has an async modifier.</param>
    /// <returns><c>True</c> if an async modifier is present.</returns>
    public static bool IsMethodOrFunctionWithAsyncModifier(this SyntaxNode node) {
      return node switch {
        BaseMethodDeclarationSyntax method => method.Modifiers.Any(SyntaxKind.AsyncKeyword),
        AnonymousFunctionExpressionSyntax function => function.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword),
        LocalFunctionStatementSyntax function => function.Modifiers.Any(SyntaxKind.AsyncKeyword),
        _ => false
      };
    }

    /// <summary>
    /// Gets all expressions that were written (excluding the declarations) inside the given syntax node.
    /// </summary>
    /// <param name="node">The node to get the nested write accesses of.</param>
    /// <param name="cancellationToken">A cancellation token to stop the search prematurely.</param>
    /// <returns>All expressions that are written to inside the given node.</returns>
    public static IEnumerable<SyntaxNode> GetAllWrittenExpressions(this SyntaxNode node, CancellationToken cancellationToken) {
      return node.DescendantNodes()
        .WithCancellation(cancellationToken)
        .Select(TryGetWrittenExpression)
        .IsNotNull();
    }

    private static ExpressionSyntax? TryGetWrittenExpression(SyntaxNode expression) {
      return expression switch {
        AssignmentExpressionSyntax assignment => assignment.Left,
        PostfixUnaryExpressionSyntax postfix => postfix.Operand,
        PrefixUnaryExpressionSyntax prefix => prefix.Operand,
        ArgumentSyntax argument when IsArgumentWithSideEffect(argument) => argument.Expression,
        _ => null,
      };
    }

    private static bool IsArgumentWithSideEffect(ArgumentSyntax argument) {
      return argument.RefKindKeyword.IsKind(SyntaxKind.RefKeyword)
        || argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword);
    }
  }
}
