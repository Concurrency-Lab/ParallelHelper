using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ParallelHelper.Analyzer {
  /// <summary>
  /// Contains information about a field access.
  /// </summary>
  public class FieldAccess {
    /// <summary>
    /// Gets the symbol of the accessed field.
    /// </summary>
    public IFieldSymbol Field { get; }

    /// <summary>
    /// Gets the node accessing the field.
    /// </summary>
    public SyntaxNode Access { get; }

    /// <summary>
    /// Gets the enclosing scope of the field access (i.e. a method or a lambda expression).
    /// </summary>
    public SyntaxNode EnclosingScope { get; }

    /// <summary>
    /// Gets the enclosing lock or <c>null</c> if there isn't any.
    /// </summary>
    public LockStatementSyntax? EnclosingLock { get; }

    /// <summary>
    /// Gets the value indicating whether the access is writing or reading the field.
    /// </summary>
    public bool IsWriting { get; }

    /// <summary>
    /// Gets the value indicating whether the access is enclosed by a lock statement or not.
    /// </summary>
    public bool IsInsideLock => EnclosingLock != null;

    public FieldAccess(IFieldSymbol field, SyntaxNode access, SyntaxNode enclosingScope,
        LockStatementSyntax? enclosingLock, bool writing) {
      Field = field;
      Access = access;
      EnclosingScope = enclosingScope;
      EnclosingLock = enclosingLock;
      IsWriting = writing;
    }

    public override string ToString() {
      return $"[Field={Field}, Expression={Access}, IsWriting={IsWriting}, IsInsideLock={IsInsideLock}]";
    }
  }
}
