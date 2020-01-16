using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace ParallelHelper.Extensions {
  /// <summary>
  /// Extension methods to work with type symbols.
  /// </summary>
  public static class TypeSymbolExtensions {
    /// <summary>
    /// Gets all the base types of the specified type including itself.
    /// </summary>
    /// <param name="type">The type to get the base types of.</param>
    /// <returns>All the base types and the type itself.</returns>
    public static IEnumerable<ITypeSymbol> GetAllBaseTypesAndSelf(this ITypeSymbol type) {
      ITypeSymbol? currentType = type;
      while(currentType != null) {
        yield return currentType;
        currentType = currentType.BaseType;
      }
    }

    /// <summary>
    /// Gets all the base types of the specified type.
    /// </summary>
    /// <param name="type">The type to get the base types of.</param>
    /// <returns>All the base types and the type.</returns>
    public static IEnumerable<ITypeSymbol> GetAllBaseTypes(this ITypeSymbol type) {
      if(type.BaseType == null) {
        return Enumerable.Empty<ITypeSymbol>();
      }
      return GetAllBaseTypesAndSelf(type.BaseType);
    }

    /// <summary>
    /// Gets all the members that are accessible -including the members of the base type- by the specified type.
    /// </summary>
    /// <param name="type">The type to get accessible members of of.</param>
    /// <returns>All the accessible members of the type and its base types.</returns>
    public static IEnumerable<ISymbol> GetAllAccessibleMembers(this ITypeSymbol type) {
      IEnumerable<ISymbol> members = type.GetMembers();
      if(type.BaseType != null) {
        members = members.Concat(GetAllNonPrivateMembers(type.BaseType));
      }
      return members;
    }

    /// <summary>
    /// Gets all the members that are not private -including the members of the base type- of the specified type.
    /// </summary>
    /// <param name="type">The type to get non-private members of.</param>
    /// <returns>All the non-private members of the type and its base types.</returns>
    public static IEnumerable<ISymbol> GetAllNonPrivateMembers(this ITypeSymbol type) {
      return GetAllMembers(type)
        .Where(member =>
          member.DeclaredAccessibility != Accessibility.NotApplicable
           && member.DeclaredAccessibility != Accessibility.Private
        );
    }

    /// <summary>
    /// Gets all the members that are public -including the members of the base type- of the specified type.
    /// </summary>
    /// <param name="type">The type to get public members of.</param>
    /// <returns>All the public members of the type and its base types.</returns>
    public static IEnumerable<ISymbol> GetAllPublicMembers(this ITypeSymbol type) {
      return GetAllMembers(type)
        .Where(member => member.DeclaredAccessibility == Accessibility.Public);
    }

    /// <summary>
    /// Gets all the members of the specified type.
    /// </summary>
    /// <param name="type">The type to get members of.</param>
    /// <returns>All the members of the type and its base types.</returns>
    public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol type) {
      return GetAllBaseTypesAndSelf(type).SelectMany(baseType => baseType.GetMembers());
    }
  }
}
