using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace ParallelHelper.Analyzer {
  /// <summary>
  /// Base analyzer for implementations that work with the semantic model analysis context and a syntax walker. This
  /// implementation is aware of the use of lock statements and loops that are candidates for conditional loops of an
  /// invocation of the <see cref="System.Threading.Monitor.Wait(object)"/> invocation. Moreover, it collects any
  /// access to a field.
  /// </summary>
  /// <typeparam name="TRootNode">The syntax type of the root node of the applied analysis.</typeparam>
  public abstract class FieldAccessAwareAnalyzerWithSyntaxWalkerBase<TRootNode> : MonitorAwareAnalyzerWithSyntaxWalkerBase<TRootNode>
      where TRootNode : SyntaxNode {
    // TODO Detect cyclic access to the same field, i.e. through loops?
    // TODO For the read-only accesses: Is it better to always treat ExpressionSyntax as read-access
    //      and exclude write-only parents instead of manually listing every read-access?

    /// <summary>
    /// Gets the fields whose access is tracked.
    /// </summary>
    public ISet<IFieldSymbol> FieldsToTrack { get; }

    /// <summary>
    /// Gets the collected field accesses.
    /// </summary>
    public ISet<FieldAccess> FieldAccesses { get; } = new HashSet<FieldAccess>();

    /// <summary>
    /// Gets the currently enclosing scopes.
    /// </summary>
    protected Stack<SyntaxNode> EnclosingScopes { get; } = new Stack<SyntaxNode>();

    /// <summary>
    /// Gets the value indicating whether the current node is enclosed by a scope or not.
    /// </summary>
    protected bool HasEnclosingScope => EnclosingScopes.Count != 0;

    /// <summary>
    /// Gets the enclosing lock-statement or <c>null</c> if there isn't any.
    /// </summary>
    protected LockStatementSyntax? EnclosingLockOrNull => EnclosingLocks.Count != 0 ? EnclosingLocks.Peek() : null;

    /// <summary>
    /// Initializes the semantic model analyzer with a syntax walker base and its monitor awareness.
    /// </summary>
    /// <param name="context">The analysis context to use during the analysis.</param>
    /// <param name="fieldsToTrack">The fields whose accesses should be tracked.</param>
    protected FieldAccessAwareAnalyzerWithSyntaxWalkerBase(IAnalysisContextWrapper context, ISet<IFieldSymbol> fieldsToTrack)
        : base(context) {
      FieldsToTrack = fieldsToTrack;
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node) {
      if (node.Modifiers.Any(SyntaxKind.PublicKeyword)) {
        EnclosingScopes.Push(node);
        base.VisitMethodDeclaration(node);
        EnclosingScopes.Pop();
      }
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node) {
      // The code of a constructor is usually not executed concurrently to the instance members.
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node) {
      if (node.Modifiers.Any(SyntaxKind.PublicKeyword)) {
        base.VisitPropertyDeclaration(node);
      }
    }

    public override void VisitAccessorDeclaration(AccessorDeclarationSyntax node) {
      if (!HasRestrictedAccessibility(node)) {
        EnclosingScopes.Push(node);
        base.VisitAccessorDeclaration(node);
        EnclosingScopes.Pop();
      }
    }

    private bool HasRestrictedAccessibility(AccessorDeclarationSyntax accessor) {
      return accessor.Modifiers.Any();
    }

    public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) {
      // TODO Maybe unneceessary since it inherits from SimpleLambdaExpressionSyntax.
      // It is necessary to identify where this code is actually executed and which accessibility it has.
    }

    public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) {
      // It is necessary to identify where this code is actually executed and which accessibility it has.
    }

    public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node) {
      // It is necessary to identify where this code is actually executed and which accessibility it has.
    }

    public override void VisitArgument(ArgumentSyntax node) {
      if (node.RefOrOutKeyword.IsKind(SyntaxKind.RefKeyword)) {
        TrackReadAccessToPotentialField(node.Expression, node);
        TrackWriteAccessToPotentialField(node.Expression, node);
      } else if (node.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword)) {
        TrackWriteAccessToPotentialField(node.Expression, node);
      } else {
        TrackReadAccessToPotentialField(node.Expression, node);
      }
      base.VisitArgument(node);
    }

    public override void VisitAssignmentExpression(AssignmentExpressionSyntax node) {
      TrackReadAccessToPotentialField(node.Right, node);
      TrackWriteAccessToPotentialField(node.Left, node);
      if (!node.IsKind(SyntaxKind.SimpleAssignmentExpression)) {
        TrackReadAccessToPotentialField(node.Left, node);
      }
      base.VisitAssignmentExpression(node);
    }

    public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node) {
      TrackReadAccessToPotentialField(node.Operand, node.Operand);
      if (IsReadWriteUnaryOperator(node.OperatorToken)) {
        TrackWriteAccessToPotentialField(node.Operand, node);
      }
      base.VisitPrefixUnaryExpression(node);
    }

    public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node) {
      TrackReadAccessToPotentialField(node.Operand, node.Operand);
      if (IsReadWriteUnaryOperator(node.OperatorToken)) {
        TrackWriteAccessToPotentialField(node.Operand, node);
      }
      base.VisitPostfixUnaryExpression(node);
    }

    private bool IsReadWriteUnaryOperator(SyntaxToken token) {
      return token.IsKind(SyntaxKind.PlusPlusToken) || token.IsKind(SyntaxKind.MinusMinusToken);
    }

    public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node) {
      TrackReadAccessToPotentialField(node.Expression, node.Expression);
      base.VisitMemberAccessExpression(node);
    }

    public override void VisitBinaryExpression(BinaryExpressionSyntax node) {
      TrackReadAccessToPotentialField(node.Left, node.Left);
      TrackReadAccessToPotentialField(node.Right, node.Right);
      base.VisitBinaryExpression(node);
    }

    public override void VisitReturnStatement(ReturnStatementSyntax node) {
      TrackReadAccessToPotentialField(node.Expression, node.Expression);
      base.VisitReturnStatement(node);
    }

    public override void VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node) {
      TrackReadAccessToPotentialField(node.Expression, node.Expression);
      base.VisitConditionalAccessExpression(node);
    }

    public override void VisitConditionalExpression(ConditionalExpressionSyntax node) {
      TrackReadAccessToPotentialField(node.Condition, node.Condition);
      TrackReadAccessToPotentialField(node.WhenTrue, node.WhenTrue);
      TrackReadAccessToPotentialField(node.WhenFalse, node.WhenFalse);
      base.VisitConditionalExpression(node);
    }

    public override void VisitInterpolation(InterpolationSyntax node) {
      TrackReadAccessToPotentialField(node.Expression, node.Expression);
      base.VisitInterpolation(node);
    }

    public override void VisitWhileStatement(WhileStatementSyntax node) {
      TrackReadAccessToPotentialField(node.Condition, node.Condition);
      base.VisitWhileStatement(node);
    }

    public override void VisitIfStatement(IfStatementSyntax node) {
      TrackReadAccessToPotentialField(node.Condition, node.Condition);
      base.VisitIfStatement(node);
    }

    public override void VisitDoStatement(DoStatementSyntax node) {
      TrackReadAccessToPotentialField(node.Condition, node.Condition);
      base.VisitDoStatement(node);
    }

    public override void VisitEqualsValueClause(EqualsValueClauseSyntax node) {
      TrackReadAccessToPotentialField(node.Value, node.Value);
      base.VisitEqualsValueClause(node);
    }

    public override void VisitSwitchStatement(SwitchStatementSyntax node) {
      TrackReadAccessToPotentialField(node.Expression, node.Expression);
      base.VisitSwitchStatement(node);
    }

    public override void VisitWhenClause(WhenClauseSyntax node) {
      TrackReadAccessToPotentialField(node.Condition, node.Condition);
      base.VisitWhenClause(node);
    }

    public override void VisitCastExpression(CastExpressionSyntax node) {
      TrackReadAccessToPotentialField(node.Expression, node.Expression);
      base.VisitCastExpression(node);
    }

    public override void VisitAnonymousObjectMemberDeclarator(AnonymousObjectMemberDeclaratorSyntax node) {
      TrackReadAccessToPotentialField(node.Expression, node.Expression);
      base.VisitAnonymousObjectMemberDeclarator(node);
    }

    private void TrackReadAccessToPotentialField(ExpressionSyntax fieldExpression, SyntaxNode readingNode) {
      TrackAccessToPotentialField(fieldExpression, readingNode, false);
    }

    private void TrackWriteAccessToPotentialField(ExpressionSyntax fieldExpression, SyntaxNode writingNode) {
      TrackAccessToPotentialField(fieldExpression, writingNode, true);
    }

    // Disabled since the parameter could be necessary later.
#pragma warning disable IDE0060 // Remove unused parameter
    private void TrackAccessToPotentialField(ExpressionSyntax? fieldExpression, SyntaxNode accessingNode, bool writing) {
#pragma warning restore IDE0060 // Remove unused parameter
      if(fieldExpression == null) {
        return;
      }
      if (HasEnclosingScope && SemanticModel.GetSymbolInfo(fieldExpression, CancellationToken).Symbol is IFieldSymbol field && FieldsToTrack.Contains(field)) {
        // TODO Report the accessing node or the accessed field in the diagnostics?
        //      The former makes it difficult to avoid duplicate reports.
        FieldAccesses.Add(new FieldAccess(field, fieldExpression, EnclosingScopes.Peek(), EnclosingLockOrNull, writing));
      }
    }
  }
}
