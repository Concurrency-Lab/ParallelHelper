using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ParallelHelper.Extensions {
  /// <summary>
  /// Extension methods for accessing the semantic model.
  /// </summary>
  public static class SemanticModelExtensions {
    /// <summary>
    /// Checks if the given type symbol has the given metadata name.
    /// </summary>
    /// <param name="semanticModel">The semantic model to use to apply the comparison.</param>
    /// <param name="type">The type to check.</param>
    /// <param name="metadataName">The metadata name of the type.</param>
    /// <returns><c>True</c> if the type has the given metadata name.</returns>
    public static bool IsEqualType(this SemanticModel semanticModel, ITypeSymbol type, string metadataName) {
      return semanticModel.GetTypesByName(metadataName).Any(type.IsEqualType);
    }

    /// <summary>
    /// Checks if the given type symbol has the given type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="other">The type to check against.</param>
    /// <returns><c>True</c> if the types are equal.</returns>
    public static bool IsEqualType(this ITypeSymbol type, ITypeSymbol other) {
      // OriginalDefinition is used here for the use with generic types.
      // For example:
      // - The defined type Task<TResult> is not equal to the type Task<string>.
      // - However, the original definition of Task<string> is Task<TResult>.
      // TODO: Maybe adapt the method's name.
      return type.OriginalDefinition.Equals(other);
    }

    /// <summary>
    /// Gets all types that match a given name.
    /// </summary>
    /// <param name="semanticModel">The semantic model to use when retrieving the type.</param>
    /// <param name="metadataName">The name of the type.</param>
    /// <returns>The type symbols that match the given name.</returns>
    public static IEnumerable<ITypeSymbol> GetTypesByName(this SemanticModel semanticModel, string metadataName) {
      // The implementation of semanticModel.Compilation.GetTypeByMetadataName(metadataName) is broken.
      // It returns null if there are multiple types of the same name, which happens for System.Threading.Thread.
      // - https://github.com/dotnet/roslyn/pull/32280
      // - https://github.com/dotnet/roslyn/issues/3864
      //return semanticModel.Compilation.GetTypeByMetadataName(metadataName);
      var compilation = semanticModel.Compilation;
      return compilation.References
        .Select(compilation.GetAssemblyOrModuleSymbol)
        .OfType<IAssemblySymbol>()
        .Select(assembly => assembly.GetTypeByMetadataName(metadataName))
        .IsNotNull();
    }

    /// <summary>
    /// Checks if the given expression has side-effects (i.e. writes to variables outside of it).
    /// </summary>
    /// <param name="semanticModel">The semantic model backing the expression.</param>
    /// <param name="expression">The expression to check for side-effects.</param>
    /// <returns><c>True</c> if the given expression contains side-effects.</returns>
    public static bool HasSideEffects(this SemanticModel semanticModel, ExpressionSyntax expression) {
      // TODO currently only supports side-effects when accessing local variables (not member fields).
      var dataFlow = semanticModel.AnalyzeDataFlow(expression);
      return dataFlow.WrittenInside
        .Except(dataFlow.VariablesDeclared)
        .Intersect(dataFlow.Captured)
        .Any();
    }

    /// <summary>
    /// Tries to retrieve the method symbol from the specified syntax node representing a method declaration or anonymous function declaration.
    /// </summary>
    /// <param name="semanticModel">The semantic model to query.</param>
    /// <param name="node">The node to get the method symbol of.</param>
    /// <param name="methodSymbol">The resolved method symbol, or <c>null</c> if it couldn't be resolved.</param>
    /// <param name="cancellationToken">The cancellation token to use.</param>
    /// <returns><c>True</c> if the method symbol could be resolved.</returns>
    public static bool TryGetMethodSymbolFromMethodOrFunctionDeclaration(
        this SemanticModel semanticModel,
        SyntaxNode node,
        out IMethodSymbol? methodSymbol,
        CancellationToken cancellationToken
    ) {
      methodSymbol = node switch {
        MethodDeclarationSyntax method => semanticModel.GetDeclaredSymbol(method, cancellationToken),
        LocalFunctionStatementSyntax function => (IMethodSymbol)semanticModel.GetDeclaredSymbol(function, cancellationToken),
        AnonymousFunctionExpressionSyntax function => (IMethodSymbol)semanticModel.GetSymbolInfo(function, cancellationToken).Symbol,
        _ => throw new ArgumentException($"{node} is not a method or function declaration")
      };
      return methodSymbol != null;
    }
  }
}
