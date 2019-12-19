using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

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
        || node is AnonymousFunctionExpressionSyntax;
    }

    /// <summary>
    /// Checks if the provided method declaration or anonymous function (e.g. lambdas) has
    /// an async modifier.
    /// </summary>
    /// <param name="node">The node to check if it has an async modifier.</param>
    /// <returns><c>True</c> if an async modifier is present.</returns>
    public static bool IsMethodOrFunctionWithAsyncModifier(this SyntaxNode node) {
      if(node is BaseMethodDeclarationSyntax method) {
        return method.Modifiers.Any(SyntaxKind.AsyncKeyword);
      }
      if (node is AnonymousFunctionExpressionSyntax function) {
        return function.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);
      }
      return false;
    }
  }
}
