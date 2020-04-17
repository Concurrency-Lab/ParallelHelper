using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ParallelHelper.Extensions {
  /// <summary>
  /// Analsis methods allowing to query for certain collection properties.
  /// </summary>
  public class CollectionAnalysis {
    private static readonly string[] CollectionBaseTypes = {
      "System.Collections.ICollection",
      "System.Collections.Generic.ICollection`1",
    };

    private static readonly string[] ImmutableCollectionTypes = {
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

    private readonly SemanticModel _semanticModel;
    private readonly CancellationToken _cancellationToken;

    /// <summary>
    /// Creates a new instance to apply collection based analyses.
    /// </summary>
    /// <param name="semanticModel">The semantic model to use when searching for collections.</param>
    /// <param name="cancellationToken">The cancellation token to use when cancelling the search operations.</param>
    public CollectionAnalysis(SemanticModel semanticModel, CancellationToken cancellationToken) {
      _semanticModel = semanticModel;
      _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets all fields declared at the specified class with the type of a potentially mutable collection (i.e. no instance of an
    /// immutable collection). Potentially in the sense of, that it may hold an instance to a mutable collection (i.e. by defining
    /// an interface rather a concrete type).
    /// </summary>
    /// <param name="classDeclaration">The class declaration to resolve the mutable collection fields of.</param>
    /// <returns>All fields declaring a mutable collection.</returns>
    public IEnumerable<IFieldSymbol> GetPotentiallyMutableCollectionFields(ClassDeclarationSyntax classDeclaration) {
      return classDeclaration.Members.WithCancellation(_cancellationToken)
        .OfType<FieldDeclarationSyntax>()
        .SelectMany(declaration => declaration.Declaration.Variables)
        .Select(field => (IFieldSymbol)_semanticModel.GetDeclaredSymbol(field, _cancellationToken))
        .Where(field => IsPotentiallyMutableCollection(field.Type));
    }

    /// <summary>
    /// Gets whether the specified type is potentially a mutable collection (i.e. not an immutable collection) or not.
    /// Potentially in the sense of, that it may hold an instance to a mutable collection (i.e. by defining an interface
    /// rather a concrete type).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><c>True</c> if the specified type is a mutable collection.</returns>
    public bool IsPotentiallyMutableCollection(ITypeSymbol type) {
      return !IsImmutableCollection(type)
        && IsAssignableToAnyOf(type, CollectionBaseTypes);
    }

    /// <summary>
    /// Gets whether the specified type represents an immutable collection.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><c>True</c> if the given type is an immutable collection.</returns>
    public bool IsImmutableCollection(ITypeSymbol type) {
      return IsAssignableToAnyOf(type, ImmutableCollectionTypes);
    }

    private bool IsAssignableToAnyOf(ITypeSymbol type, string[] targetTypes) {
      return targetTypes
        .SelectMany(targetType => _semanticModel.GetTypesByName(targetType))
        .Any(targetType => type.AllInterfaces.Concat(new[] { type }).Any(type => type.IsEqualType(targetType)));
    }
  }
}
