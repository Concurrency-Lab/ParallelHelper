using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ParallelHelper.Analyzer;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ParallelHelper.Extensions {
  /// <summary>
  /// Extension methods for <see cref="ClassMemberDescriptor"/>.
  /// </summary>
  public static class ClassMemberDescriptorExtensions {
    /// <summary>
    /// Checks if the given invocation invokes a member represented by any of the given descriptors.
    /// </summary>
    /// <param name="descriptors">The descriptors to query.</param>
    /// <param name="semanticModel">The semantic model to use to check the type compatibility.</param>
    /// <param name="invocation">The invocation to check if intovkes a member.</param>
    /// <returns><c>true</c> if the given method is a member.</returns>
    public static bool AnyContainsInvokedMethod(
      this IEnumerable<ClassMemberDescriptor> descriptors,
      SemanticModel semanticModel,
      InvocationExpressionSyntax invocation,
      CancellationToken cancellationToken
    ) {
      return semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol method
        && descriptors.AnyContainsMember(semanticModel, method);
    }

    /// <summary>
    /// Checks if the given symbol is a member (field, method, or property) represented by any of the given descriptors.
    /// </summary>
    /// <param name="semanticModel">The semantic model to use to check the type compatibility.</param>
    /// <param name="descriptors">The descriptors to query.</param>
    /// <param name="symbol">The symbol to check if its a member.</param>
    /// <returns><c>true</c> if the given method is a member.</returns>
    public static bool AnyContainsMember(
      this IEnumerable<ClassMemberDescriptor> descriptors,
      SemanticModel semanticModel,
      ISymbol symbol
    ) {
      return descriptors.FirstContainingMember(semanticModel, symbol) != null;
    }

    /// <summary>
    /// Tries to find the first descriptor containing the specified member.
    /// </summary>
    /// <param name="descriptors">The descriptors to find the member in.</param>
    /// <param name="semanticModel">The semantic model to use to check the type compatibility.</param>
    /// <param name="symbol">The symbol to find the containing descriptor of.</param>
    /// <returns>The descriptor containing the given symbol or <c>null</c> if no descriptor contains the given member symbol.</returns>
    public static TDescriptor? FirstContainingMember<TDescriptor>(
      this IEnumerable<TDescriptor> descriptors,
      SemanticModel semanticModel,
      ISymbol symbol
    ) where TDescriptor : ClassMemberDescriptor {
      if(!IsMemberSymbol(symbol)) {
        return null;
      }
      return descriptors.FirstOrDefault(descriptor => descriptor.ContainsMember(semanticModel, symbol));
    }

    private static bool ContainsMember(this ClassMemberDescriptor descriptor, SemanticModel semanticModel, ISymbol symbol) {
      return semanticModel.IsEqualType(symbol.ContainingType, descriptor.Type)
        && descriptor.Members.Contains(symbol.Name);
    }

    private static bool IsMemberSymbol(ISymbol symbol) {
      return symbol switch {
        IPropertySymbol _ => true,
        IMethodSymbol _ => true,
        IFieldSymbol _ => true,
        _ => false
      };
    }
  }
}
