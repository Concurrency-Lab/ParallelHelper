using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ParallelHelper.Extensions {
  /// <summary>
  /// Extension methods to work with method symbols.
  /// </summary>
  public static class MethodSymbolExtensions {
    /// <summary>
    /// Returns all overloads, including the method itself, of the specified method.
    /// </summary>
    /// <param name="method">The method to reslove the overloads of.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation early.</param>
    /// <returns>All identified method overloads.</returns>
    public static IEnumerable<IMethodSymbol> GetAllOverloads(this IMethodSymbol method, CancellationToken cancellationToken) {
      return method.ContainingType.GetMembers()
        .WithCancellation(cancellationToken)
        .OfType<IMethodSymbol>()
        .Where(member => member.IsStatic == method.IsStatic)
        .Where(member => member.Name.Equals(method.Name));
    }

    /// <summary>
    /// Gets whether the given method is an implementation of an interface method or not.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation early.</param>
    /// <returns><c>True</c> if the specified method is an implementation of a method defined by an interface.</returns>
    public static bool IsInterfaceImplementation(this IMethodSymbol method, CancellationToken cancellationToken) {
      return method.ContainingType.AllInterfaces
        .SelectMany(i => i.GetMembers())
        .WithCancellation(cancellationToken)
        .OfType<IMethodSymbol>()
        .Any(m => method.HasSameSignatureAs(m, cancellationToken));
    }

    private static bool HasSameSignatureAs(this IMethodSymbol method, IMethodSymbol other, CancellationToken cancellationToken) {
      if(method.Parameters.Length != other.Parameters.Length || method.Name != other.Name) {
        return false;
      }
      return method.Parameters
        .WithCancellation(cancellationToken)
        .Zip(other.Parameters, (a, b) => a.Type.Equals(b.Type))
        .All(parametersAreEqual => parametersAreEqual);
    }
  }
}
