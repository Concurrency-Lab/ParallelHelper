using Microsoft.CodeAnalysis;

namespace ParallelHelper.Extensions {
  /// <summary>
  /// Extension methods for the use with symbols of the semantic model.
  /// </summary>
  public static class SymbolExtensions {
    /// <summary>
    /// Checks if the passed symbol is a variable, i.e., any of the following types: <see cref="ILocalSymbol"/>, <see cref="IFieldSymbol"/>, or <see cref="IParameterSymbol"/>.
    /// <see cref="IPropertySymbol"/> is not included since it could have some invocation logic associated.
    /// </summary>
    /// <param name="symbol">The symbol to check</param>
    /// <returns><c>true</c> if the passed symbol represents a variable.</returns>
    public static bool IsVariable(this ISymbol? symbol) {
      return symbol is IFieldSymbol || symbol is ILocalSymbol || symbol is IParameterSymbol;
    }
  }
}
