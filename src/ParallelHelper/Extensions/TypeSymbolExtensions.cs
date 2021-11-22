using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
      return GetAllMembers(type).Where(IsNonPrivateMember);
    }

    private static bool IsNonPrivateMember(ISymbol member) {
      return member.DeclaredAccessibility != Accessibility.NotApplicable
        && member.DeclaredAccessibility != Accessibility.Private;
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

    /// <summary>
    /// Checks if the given type is a base type of the given type.
    /// </summary>
    /// <param name="baseType">The base type to check.</param>
    /// <param name="subType">The type to check if it's a sub type of the given base type.</param>
    /// <param name="cancellationToken">A token to stop the check before completion.</param>
    /// <returns><c>True</c> if the given type is a sub-type of the given base type.</returns>
    /// <remarks>This check does not include interfaces.</remarks>
    public static bool IsBaseTypeOf(this ITypeSymbol baseType, ITypeSymbol subType, CancellationToken cancellationToken) {
      return subType.GetAllBaseTypesAndSelf()
        .WithCancellation(cancellationToken)
        .Any(type => baseType.Equals(type, SymbolEqualityComparer.Default));
    }
  }
}
