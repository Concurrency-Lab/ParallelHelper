using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Util;
using System.Collections.Generic;

namespace ParallelHelper.Analyzer {
  /// <summary>
  /// Base analyzer for implementations that work with the semantic model analysis context and a syntax walker. This
  /// implementation is aware of the use of lock statements and loops that are candidates for conditional loops of an
  /// invocation of the <see cref="System.Threading.Monitor.Wait(object)"/> invocation.
  /// </summary>
  internal abstract class MonitorAwareSemanticModelAnalyzerWithSyntaxWalkerBase : SemanticModelAnalyzerWithSyntaxWalkerBase {
    /// <summary>
    /// Gets the analysis instance for monitor based analysis.
    /// </summary>
    protected MonitorAnalysis MonitorAnalysis { get; }

    /// <summary>
    /// Gets whether the currently visitted node is inside a lock statement or not.
    /// </summary>
    protected bool IsInsideLock => LockDepth > 0;

    /// <summary>
    /// Gets whether the currently visitted node is inside a loop that is enclosed by a lock statement or not.
    /// </summary>
    protected bool IsInsideLoopEnclosedByLock => LoopsEnclosedByLock.Count > 0;

    /// <summary>
    /// Gets the depth of the lock-statements when visiting the current node.
    /// </summary>
    protected int LockDepth => EnclosingLocks.Count;

    /// <summary>
    /// Gets the enclosing lock-statement. The top of the stack represents the most-inner statement.
    /// </summary>
    protected Stack<LockStatementSyntax> EnclosingLocks { get; } = new Stack<LockStatementSyntax>();

    /// <summary>
    /// Gets the enclosing loop statements that have occured inside a lock statement. The top
    /// of the stack represents the most-inner loop statement.
    /// </summary>
    protected Stack<WhileStatementSyntax> LoopsEnclosedByLock { get; } = new Stack<WhileStatementSyntax>();

    /// <summary>
    /// Initializes the semantic model analyzer with a syntax walker base and its monitor awareness.
    /// </summary>
    /// <param name="context">The semantic model analysis context to use during the analysis.</param>
    protected MonitorAwareSemanticModelAnalyzerWithSyntaxWalkerBase(SemanticModelAnalysisContext context) : base(context) {
      MonitorAnalysis = new MonitorAnalysis(SemanticModel, CancellationToken);
    }

    public override void VisitLockStatement(LockStatementSyntax node) {
      EnclosingLocks.Push(node);
      base.VisitLockStatement(node);
      EnclosingLocks.Pop();
    }

    public override void VisitWhileStatement(WhileStatementSyntax node) {
      if(IsInsideLock) {
        LoopsEnclosedByLock.Push(node);
        base.VisitWhileStatement(node);
        LoopsEnclosedByLock.Pop();
      } else {
        base.VisitWhileStatement(node);
      }
    }
  }
}
