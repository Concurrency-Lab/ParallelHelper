﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ParallelHelper.Extensions;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace ParallelHelper.Util {
  /// <summary>
  /// Tool collection for analysing <see cref="System.Threading.Tasks.Task"/> based patterns.
  /// </summary>
  public class TaskAnalysis {
    private const string TaskTypeWithoutValue = "System.Threading.Tasks.Task";
    private static readonly string[] TaskTypes = {
      TaskTypeWithoutValue,
      "System.Threading.Tasks.Task`1"
    };

    private static readonly string[] ValueTaskTypes = {
      "System.Threading.Tasks.ValueTask",
      "System.Threading.Tasks.ValueTask`1"
    };

    private static readonly ISet<string> BlockingProperties = ImmutableHashSet.Create(
      "Result"
    );

    private static readonly ISet<string> BlockingMethods = ImmutableHashSet.Create(
      "Wait"
    );


    private static readonly ISet<string> ContinuationMethods = ImmutableHashSet.Create(
      "ContinueWith"
    );

    private const string FromResultMethod = "FromResult";
    private const string CompletedTaskProperty = "CompletedTask";

    private readonly SemanticModel _semanticModel;
    private readonly CancellationToken _cancellationToken;

    public TaskAnalysis(SemanticModel semanticModel, CancellationToken cancellationToken) {
      _semanticModel = semanticModel;
      _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Checks if the expression is of a task type.
    /// </summary>
    /// <param name="expression">The expression to check.</param>
    /// <returns><c>true</c> if the given expression is of a task type, <c>false</c> otherwise.</returns>
    public bool IsTaskTyped(ExpressionSyntax expression) {
      var type = _semanticModel.GetTypeInfo(expression, _cancellationToken).Type;
      return type != null && IsTaskType(type);
    }

    /// <summary>
    /// Checks if the given type represents a task type.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns><c>true</c> if the given type is a task type, <c>false</c> otherwise.</returns>
    public bool IsTaskType(ITypeSymbol type) {
      return TaskTypes.Any(typeName => _semanticModel.IsEqualType(type, typeName));
    }

    /// <summary>
    /// Checks if the given type represents the task type <see cref="System.Threading.Tasks.Task"/>.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns><c>true</c> if the given type is a task type without a value, <c>false</c> otherwise.</returns>
    public bool IsTaskTypeWithoutResult(ITypeSymbol type) {
      return _semanticModel.IsEqualType(type, TaskTypeWithoutValue);
    }

    /// <summary>
    /// Checks if the expression is of a value task type.
    /// </summary>
    /// <param name="expression">The expression to check.</param>
    /// <returns><c>true</c> if the given expression is of a value task type, <c>false</c> otherwise.</returns>
    public bool IsValueTaskTyped(ExpressionSyntax expression) {
      var type = _semanticModel.GetTypeInfo(expression, _cancellationToken).Type;
      return type != null && IsValueTaskType(type);
    }

    /// <summary>
    /// Checks if the given type represents a value task type.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns><c>true</c> if the given type is a task type, <c>false</c> otherwise.</returns>
    public bool IsValueTaskType(ITypeSymbol type) {
      return ValueTaskTypes.Any(typeName => _semanticModel.IsEqualType(type, typeName));
    }

    /// <summary>
    /// Checks if the given member access is accessing a task property that is blocking.
    /// </summary>
    /// <param name="memberAccess">The member access to check.</param>
    /// <returns><c>true</c> if it's a blocking property access, <c>false</c> otherwise.</returns>
    public bool IsBlockingPropertyAccess(MemberAccessExpressionSyntax memberAccess) {
      return _semanticModel.GetSymbolInfo(memberAccess, _cancellationToken).Symbol is IPropertySymbol property
        && IsAnyTaskOrValueTaskMember(property, BlockingProperties);
    }

    /// <summary>
    /// Checks if the given method invocation is accessing a task method that is blocking.
    /// </summary>
    /// <param name="invocation">The method invocation to check.</param>
    /// <returns><c>true</c> if it's a blocking method invocation, <c>false</c> otherwise.</returns>
    public bool IsBlockingMethodInvocation(InvocationExpressionSyntax invocation) {
      return IsAnyTaskOrValueTaskMethodInvocation(invocation, BlockingMethods);
    }

    /// <summary>
    /// Checks if the given method invocation is accessing a task method that is blocking.
    /// </summary>
    /// <param name="invocation">The method invocation to check.</param>
    /// <returns><c>true</c> if it's a blocking method invocation, <c>false</c> otherwise.</returns>
    public bool IsContinuationMethodInvocation(InvocationExpressionSyntax invocation) {
      return IsAnyTaskOrValueTaskMethodInvocation(invocation, ContinuationMethods);
    }

    /// <summary>
    /// Checks if the given method invocation is accessing <see cref="System.Threading.Tasks.Task.FromResult{TResult}(TResult)"/>.
    /// </summary>
    /// <param name="invocation">The method invocation to check.</param>
    /// <returns><c>true</c> if it's the invocation of FromResult, <c>false</c> otherwise.</returns>
    public bool IsFromResultInvocation(InvocationExpressionSyntax invocation) {
      return IsTaskMethodInvocation(invocation, FromResultMethod);
    }

    /// <summary>
    /// Checks if the given member access is accessing <see cref="System.Threading.Tasks.Task.CompletedTask"/>.
    /// </summary>
    /// <param name="memberAccess">The member access to check.</param>
    /// <returns><c>true</c> if it's the member access of CompletedTask, <c>false</c> otherwise.</returns>
    public bool IsCompletedTaskAccess(MemberAccessExpressionSyntax memberAccess) {
      return IsTaskMemberAccess(memberAccess, CompletedTaskProperty);
    }

    /// <summary>
    /// Checks if the given node is a method or function (e.g. lambda) returns a task.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns><c>true</c> if the underlying method or function returns a task, <c>false</c> otherwise.</returns>
    public bool IsMethodOrFunctionReturningTask(SyntaxNode node) {
      return _semanticModel.TryGetMethodSymbolFromMethodOrFunctionDeclaration(node, out var method, _cancellationToken)
        && method!.ReturnType != null
        && IsTaskType(method!.ReturnType);
    }

    private bool IsAnyTaskOrValueTaskMethodInvocation(InvocationExpressionSyntax invocation, ICollection<string> taskMembers) {
      return _semanticModel.GetSymbolInfo(invocation, _cancellationToken).Symbol is IMethodSymbol method
        && IsAnyTaskOrValueTaskMember(method, taskMembers);
    }

    private bool IsAnyTaskOrValueTaskMember(ISymbol member, ICollection<string> taskMembers) {
      return taskMembers.Contains(member.Name)
        && (IsTaskType(member.ContainingType) || IsValueTaskType(member.ContainingType));
    }

    private bool IsTaskMethodInvocation(InvocationExpressionSyntax invocation, string taskMember) {
      return _semanticModel.GetSymbolInfo(invocation, _cancellationToken).Symbol is IMethodSymbol method
        && IsTaskMember(method, taskMember);
    }

    private bool IsTaskMemberAccess(MemberAccessExpressionSyntax invocation, string taskMember) {
      var symbol = _semanticModel.GetSymbolInfo(invocation, _cancellationToken).Symbol;
      return symbol != null
        && IsTaskMember(symbol, taskMember);
    }

    private bool IsTaskMember(ISymbol member, string taskMember) {
      return member.Name == taskMember && IsTaskType(member.ContainingType);
    }
  }
}
