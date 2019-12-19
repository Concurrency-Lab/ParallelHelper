using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ParallelHelper.Extensions;
using System.Threading;

namespace ParallelHelper.Util {
  /// <summary>
  /// Tool collection for analysing <see cref="System.Threading.Monitor"/> based patterns.
  /// </summary>
  public class MonitorAnalysis {
    private const string MonitorType = "System.Threading.Monitor";
    private const string WaitMethod = "Wait";
    private const string PulseMethod = "Pulse";
    private const string PulseAllMethod = "PulseAll";
    private const string TryEnterMethod = "TryEnter";
    private const string EnterMethod = "Enter";

    private readonly SemanticModel _semanticModel;
    private readonly CancellationToken _cancellationToken;

    public MonitorAnalysis(SemanticModel semanticModel, CancellationToken cancellationToken) {
      _semanticModel = semanticModel;
      _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Checks if the given method invocation invokes the <see cref="System.Threading.Monitor.Wait(object)"/> method.
    /// </summary>
    /// <param name="invocation">The invocation to check.</param>
    /// <returns><c>True</c> if the method invokes the monitor's wait method.</returns>
    public bool IsMonitorWait(InvocationExpressionSyntax invocation) {
      return IsMonitorMethodWithName(invocation, WaitMethod);
    }

    /// <summary>
    /// Checks if the given method invocation invokes the <see cref="System.Threading.Monitor.Pulse(object)"/> method.
    /// </summary>
    /// <param name="invocation">The invocation to check.</param>
    /// <returns><c>True</c> if the method invokes the monitor's pulse method.</returns>
    public bool IsMonitorPulse(InvocationExpressionSyntax invocation) {
      return IsMonitorMethodWithName(invocation, PulseMethod);
    }

    /// <summary>
    /// Checks if the given method invocation invokes the <see cref="System.Threading.Monitor.PulseAll(object)"/> method.
    /// </summary>
    /// <param name="invocation">The invocation to check.</param>
    /// <returns><c>True</c> if the method invokes the monitor's pulseall method.</returns>
    public bool IsMonitorPulseAll(InvocationExpressionSyntax invocation) {
      return IsMonitorMethodWithName(invocation, PulseAllMethod);
    }

    /// <summary>
    /// Checks if the given method invocation invokes the <see cref="System.Threading.Monitor.TryEnter(object)"/> method or
    /// one of its overloads.
    /// </summary>
    /// <param name="invocation">The invocation to check.</param>
    /// <returns><c>True</c> if the method invokes the monitor's TryEnter method.</returns>
    public bool IsMonitorTryEnter(InvocationExpressionSyntax invocation) {
      return IsMonitorMethodWithName(invocation, TryEnterMethod);
    }

    /// <summary>
    /// Checks if the given method invocation invokes the <see cref="System.Threading.Monitor.Enter(object)"/> method or
    /// one of its overloads.
    /// </summary>
    /// <param name="invocation">The invocation to check.</param>
    /// <returns><c>True</c> if the method invokes the monitor's Enter method.</returns>
    public bool IsMonitorEnter(InvocationExpressionSyntax invocation) {
      return IsMonitorMethodWithName(invocation, EnterMethod);
    }

    private bool IsMonitorMethodWithName(InvocationExpressionSyntax invocation, string methodName) {
      return _semanticModel.GetSymbolInfo(invocation, _cancellationToken).Symbol is IMethodSymbol method
        && method.Name.Equals(methodName) && IsMonitorMethod(method);
    }

    /// <summary>
    /// Checks if the given method is a method of the <see cref="System.Threading.Monitor"/> type.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <returns><c>True</c> if the method belongs to the monitor type.</returns>
    public bool IsMonitorMethod(IMethodSymbol method) {
      return _semanticModel.IsEqualType(method.ContainingType, MonitorType);
    }

    /// <summary>
    /// Tries to extract the synchronization object from the given invocation of a monitor method.
    /// </summary>
    /// <param name="invocation">The invocation to extract the synchronization object from.</param>
    /// <param name="syncObject">The extracted synchronization object.</param>
    /// <returns><c>True</c> if the synchronization object could be suffesfully extracted.</returns>
    public bool TryGetSyncObjectFromMonitorMethod(InvocationExpressionSyntax invocation, out ISymbol? syncObject) {
      var arguments = invocation.ArgumentList.Arguments;
      if(arguments.Count == 0) {
        syncObject = null;
        return false;
      }
      syncObject = _semanticModel.GetSymbolInfo(arguments[0].Expression, _cancellationToken).Symbol;
      return syncObject != null;
    }
  }
}
