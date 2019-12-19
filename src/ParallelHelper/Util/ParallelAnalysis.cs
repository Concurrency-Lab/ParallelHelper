using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ParallelHelper.Extensions;
using System.Linq;
using System.Threading;

namespace ParallelHelper.Util {
  /// <summary>
  /// Tool collection for analysing <see cref="System.Threading.Tasks.Parallel"/> based patterns.
  /// </summary>
  public class ParallelAnalysis {
    private const string ParallelType = "System.Threading.Tasks.Parallel";
    private const string ForMethod = "For";
    private const string ForEachMethod = "ForEach";

    private readonly SemanticModel _semanticModel;
    private readonly CancellationToken _cancellationToken;

    private static readonly ParallelMethodDescriptor[] ParallelMethods = {
      new ParallelMethodDescriptor(ForMethod, 2), new ParallelMethodDescriptor(ForEachMethod, 1)
    };

    public ParallelAnalysis(SemanticModel semanticModel, CancellationToken cancellationToken) {
      _semanticModel = semanticModel;
      _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Tries to get the delgate provided to the <see cref="System.Threading.Tasks.Parallel.For(int, int, System.Action{int})"/> or
    /// <see cref="System.Threading.Tasks.Parallel.ForEach{TSource}(System.Collections.Generic.IEnumerable{TSource}, System.Action{TSource})"/>
    /// methods or one of its overloads, if the invocation targets one of these methods.
    /// </summary>
    /// <param name="invocation">The potential Parallel.For or Parallel.ForEach invocation to get the delegate of.</param>
    /// <param name="expression">The expression representing the delegate when it could be resolved, otherwise <c>null</c>.</param>
    /// <returns><c>True</c> if the delegate could be resolved.</returns>
    public bool TryGetParallelForOrForEachDelegate(InvocationExpressionSyntax invocation, out ExpressionSyntax? expression) {
      var methodSymbol = _semanticModel.GetSymbolInfo(invocation, _cancellationToken).Symbol as IMethodSymbol;
      if(methodSymbol == null || !_semanticModel.IsEqualType(methodSymbol.ContainingType, ParallelType)) {
        expression = null;
        return false;
      }
      var arguments = invocation.ArgumentList.Arguments;
      expression = ParallelMethods
        .Where(method => arguments.Count > method.DelegateArgumentPosition)
        .Where(method => method.MethodName.Equals(methodSymbol.Name))
        .Select(method => arguments[method.DelegateArgumentPosition].Expression)
        .FirstOrDefault();
      return expression != null;
    }

    /// <summary>
    /// Checks if the given method invocation invokes the <see cref="System.Threading.Tasks.Parallel.For(int, int, System.Action{int})"/> 
    /// method or one of its overloads.
    /// </summary>
    /// <param name="invocation">The invocation to check.</param>
    /// <returns><c>True</c> if the method invokes the Parallel.For method.</returns>
    public bool IsParallelFor(InvocationExpressionSyntax invocation) {
      return IsParallelMethodWithName(invocation, ForMethod);
    }

    /// <summary>
    /// Checks if the given method invocation invokes the <see cref="System.Threading.Tasks.Parallel.ForEach{TSource}(System.Collections.Generic.IEnumerable{TSource}, System.Action{TSource})"/>
    /// method or one of its overloads.
    /// </summary>
    /// <param name="invocation">The invocation to check.</param>
    /// <returns><c>True</c> if the method invokes the Parallel.ForEach method.</returns>
    public bool IsParallelForEach(InvocationExpressionSyntax invocation) {
      return IsParallelMethodWithName(invocation, ForEachMethod);
    }

    private bool IsParallelMethodWithName(InvocationExpressionSyntax invocation, string methodName) {
      return _semanticModel.GetSymbolInfo(invocation, _cancellationToken).Symbol is IMethodSymbol method
        && method.Name.Equals(methodName) && IsParallelMethod(method);
    }

    /// <summary>
    /// Checks if the given method is a method of the <see cref="System.Threading.Tasks.Parallel"/> type.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <returns><c>True</c> if the method belongs to the monitor type.</returns>
    public bool IsParallelMethod(IMethodSymbol method) {
      return _semanticModel.IsEqualType(method.ContainingType, ParallelType);
    }

    private class ParallelMethodDescriptor {
      public string MethodName { get; }
      public int DelegateArgumentPosition { get; }

      public ParallelMethodDescriptor(string methodName, int delegateArgumentPosition) {
        MethodName = methodName;
        DelegateArgumentPosition = delegateArgumentPosition;
      }
    }
  }
}
