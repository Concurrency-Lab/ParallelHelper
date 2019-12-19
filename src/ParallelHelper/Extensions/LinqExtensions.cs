using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ParallelHelper.Extensions {
  /// <summary>
  /// Extension methos for LINQ expressions.
  /// </summary>
  public static class LinqExtensions {
    /// <summary>
    /// Cancels the iteration over the elements as soon as a cancellation is requested.
    /// </summary>
    /// <typeparam name="TElement">The type of elements to iterate over.</typeparam>
    /// <param name="elements">The elements to iterate over.</param>
    /// <param name="token">The token to check for cancellation.</param>
    /// <returns>The elements to iterate over.</returns>
    public static IEnumerable<TElement> WithCancellation<TElement>(this IEnumerable<TElement> elements, CancellationToken token) {
      foreach(var element in elements) {
        token.ThrowIfCancellationRequested();
        yield return element;
      }
    }

    /// <summary>
    /// Filters all null-elements from the provided enumerable based on nullable-elements.
    /// </summary>
    /// <typeparam name="TElement">The type of elements to filter.</typeparam>
    /// <param name="elements">The elements to filter.</param>
    /// <returns>The elements without any <c>null</c>.</returns>
    public static IEnumerable<TElement> IsNotNull<TElement>(this IEnumerable<TElement?> elements) where TElement : class {
      return elements.Where(e => e != null).Cast<TElement>();
    }
  }
}
