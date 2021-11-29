using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Util;
using System.Linq;

namespace ParallelHelper.Test.Util {
  [TestClass]
  public class TaskAnalysisTest {
    [TestMethod]
    public void IsTaskTypedReturnsTrueForTaskVariable() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public Task Start() {
    var task = Task.CompletedTask;
    return task;
  }
}

public static class Parallel {
  public void For(int start, int end, Action<int> action) {}
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var variable = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ReturnStatementSyntax>()
        .Select(returnStatement => returnStatement.Expression)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.IsTaskTyped(variable));
    }

    [TestMethod]
    public void IsTaskTypedReturnsTrueForMethodInvocationReturningTask() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public Task<int> GetValueAsync() {
    return ComputeValueAsync();
  }

  private Task<int> ComputeValueAsync() => Task.FromResult(1);
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ReturnStatementSyntax>()
        .Select(returnStatement => returnStatement.Expression)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.IsTaskTyped(invocation));
    }

    [TestMethod]
    public void IsTaskTypedReturnsFalseForVariableOfOtherType() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public int GetValue() {
    int x = 1;
    return x;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var variable = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ReturnStatementSyntax>()
        .Select(returnStatement => returnStatement.Expression)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsTaskTyped(variable));
    }

    [TestMethod]
    public void IsTaskTypedReturnsFalseForUnresolvableVariable() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public Task<int> GetValueAsync() {
    return unknown;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var variable = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ReturnStatementSyntax>()
        .Select(returnStatement => returnStatement.Expression)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsTaskTyped(variable));
    }

    [TestMethod]
    public void IsTaskTypeReturnsTrueForTask() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public Task DoItAsync() {
    return Task.CompletedTask;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .Select(declaration => declaration.ReturnType)
        .Select(returnType => semanticModel.GetTypeInfo(returnType).Type)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.IsTaskType(type));
    }

    [TestMethod]
    public void IsTaskTypeReturnsFalseForNonTask() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public int DoIt() {
    return 1;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .Select(declaration => declaration.ReturnType)
        .Select(returnType => semanticModel.GetTypeInfo(returnType).Type)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsTaskType(type));
    }

    [TestMethod]
    public void IsTaskTypeReturnsFalseForValueTask() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public ValueTask<int> DoItAsync() {
    return new ValueTask(1);
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .Select(declaration => declaration.ReturnType)
        .Select(returnType => semanticModel.GetTypeInfo(returnType).Type)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsTaskType(type));
    }

    [TestMethod]
    public void IsTaskTypeWithoutResultReturnsTrueForTaskWithoutResult() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public Task DoItAsync() {
    return Task.CompletedTask;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .Select(declaration => declaration.ReturnType)
        .Select(returnType => semanticModel.GetTypeInfo(returnType).Type)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.IsTaskTypeWithoutResult(type));
    }

    [TestMethod]
    public void IsTaskTypeWithoutResultReturnsFalseForTaskWithResult() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public Task<int> DoItAsync() {
    return Task.FromResult(1);
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .Select(declaration => declaration.ReturnType)
        .Select(returnType => semanticModel.GetTypeInfo(returnType).Type)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsTaskTypeWithoutResult(type));
    }

    [TestMethod]
    public void IsTaskTypeWithoutResultReturnsFalseForValueTask() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public ValueTask<int> DoItAsync() {
    return new ValueTask(1);
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .Select(declaration => declaration.ReturnType)
        .Select(returnType => semanticModel.GetTypeInfo(returnType).Type)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsTaskTypeWithoutResult(type));
    }

    [TestMethod]
    public void IsValueTaskTypedReturnsTrueForValueTaskVariable() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public ValueTask<int> Start() {
    return new ValueTask(1);
  }
}

public static class Parallel {
  public void For(int start, int end, Action<int> action) {}
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var variable = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ReturnStatementSyntax>()
        .Select(returnStatement => returnStatement.Expression)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.IsValueTaskTyped(variable));
    }

    [TestMethod]
    public void IsValueTaskTypedReturnsTrueForMethodInvocationReturningValueTask() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public ValueTask<int> GetValueAsync() {
    return ComputeValueAsync();
  }

  private ValueTask<int> ComputeValueAsync() => new ValueTask(1);
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ReturnStatementSyntax>()
        .Select(returnStatement => returnStatement.Expression)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.IsValueTaskTyped(invocation));
    }

    [TestMethod]
    public void IsValueTaskTypedReturnsFalseForVariableOfOtherType() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public int GetValue() {
    int x = 1;
    return x;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var variable = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ReturnStatementSyntax>()
        .Select(returnStatement => returnStatement.Expression)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsValueTaskTyped(variable));
    }

    [TestMethod]
    public void IsValueTaskTypedReturnsFalseForUnresolvableVariable() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public ValueTask<int> GetValueAsync() {
    return unknown;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var variable = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ReturnStatementSyntax>()
        .Select(returnStatement => returnStatement.Expression)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsValueTaskTyped(variable));
    }

    [TestMethod]
    public void IsValueTaskTypeReturnsTrueForValueTask() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public ValueTask<int> DoItAsync() {
    return new ValueTask(1);
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .Select(declaration => declaration.ReturnType)
        .Select(returnType => semanticModel.GetTypeInfo(returnType).Type)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.IsValueTaskType(type));
    }

    [TestMethod]
    public void IsValueTaskTypeReturnsFalseForNonValueTask() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public int DoIt() {
    return 1;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .Select(declaration => declaration.ReturnType)
        .Select(returnType => semanticModel.GetTypeInfo(returnType).Type)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsValueTaskType(type));
    }

    [TestMethod]
    public void IsValueTaskTypeReturnsFalseForTask() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public Task<int> DoItAsync() {
    return Task.FromResult(1);
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .Select(declaration => declaration.ReturnType)
        .Select(returnType => semanticModel.GetTypeInfo(returnType).Type)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsValueTaskType(type));
    }

    [TestMethod]
    public void IsBlockingPropertyAccessReturnsTrueForAccessingTaskResult() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public int GetValue() {
    var task = Task.Run(() => 1);
    return task.Result;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ReturnStatementSyntax>()
        .Select(returnStatement => (MemberAccessExpressionSyntax)returnStatement.Expression)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.IsBlockingPropertyAccess(type));
    }

    [TestMethod]
    public void IsBlockingPropertyAccessReturnsTrueForAccessingValueTaskResult() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public int GetValue() {
    var task = new ValueTask<int>(1);
    return task.Result;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ReturnStatementSyntax>()
        .Select(returnStatement => (MemberAccessExpressionSyntax)returnStatement.Expression)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.IsBlockingPropertyAccess(type));
    }

    [TestMethod]
    public void IsBlockingPropertyAccessReturnsFalseForAccessingTaskStatus() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public TaskStatus GetStatus() {
    var task = Task.Run(() => 1);
    return task.Status;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ReturnStatementSyntax>()
        .Select(returnStatement => (MemberAccessExpressionSyntax)returnStatement.Expression)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsBlockingPropertyAccess(type));
    }

    [TestMethod]
    public void IsBlockingPropertyAccessReturnsFalseForAccessingResultPropertyOfNonTaskType() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public TaskStatus GetStatus() {
    var test = new Test();
    return test.Result;
  }

  private class Test {
    public Result => 1;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ReturnStatementSyntax>()
        .Select(returnStatement => (MemberAccessExpressionSyntax)returnStatement.Expression)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsBlockingPropertyAccess(type));
    }

    [TestMethod]
    public void IsBlockingPropertyAccessReturnsFalseForAccessingResultPropertyOfUnresolvableType() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public TaskStatus GetStatus() {
    var test = new Test();
    return test.Result;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ReturnStatementSyntax>()
        .Select(returnStatement => (MemberAccessExpressionSyntax)returnStatement.Expression)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsBlockingPropertyAccess(type));
    }

    [TestMethod]
    public void IsBlockingPropertyAccessReturnsFalseForAccessingUnknownTaskProperty() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public TaskStatus GetStatus() {
    var task = Task.Run(() => 1);
    return task.SomeNonExistantProperty;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ReturnStatementSyntax>()
        .Select(returnStatement => (MemberAccessExpressionSyntax)returnStatement.Expression)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsBlockingPropertyAccess(type));
    }

    [TestMethod]
    public void IsBlockingMethodInvocationReturnsTrueForInvokingTaskWait() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public void DoIt() {
    var task = Task.Run(() => 1);
    task.Wait();
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Skip(1)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.IsBlockingMethodInvocation(type));
    }

    [TestMethod]
    public void IsBlockingMethodInvocationReturnsFalseForInvokingTaskToString() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public string GetInfo() {
    var task = Task.Run(() => 1);
    return task.ToString();
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Skip(1)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsBlockingMethodInvocation(type));
    }

    [TestMethod]
    public void IsBlockingMethodInvocationReturnsFalseForInvokingWaitMethodOfNonTaskType() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public void DoIt() {
    var test = new Test();
    test.Wait();
  }

  private class Test {
    public void Wait() { }
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsBlockingMethodInvocation(type));
    }

    [TestMethod]
    public void IsBlockingMethodInvocationReturnsFalseForInvokingWaitMethodOfUnresolvableType() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public void DoIt() {
    var test = new Test();
    test.Wait();
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsBlockingMethodInvocation(type));
    }

    [TestMethod]
    public void IsBlockingMethodInvocationReturnsFalseForInvokingUnknownTaskMethod() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public void DoIt() {
    var task = Task.Run(() => 1);
    task.SomeNonExistantMethod();
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Skip(1)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsBlockingMethodInvocation(type));
    }

    [TestMethod]
    public void IsContinuationMethodInvocationReturnsTrueForInvokingTaskContinueWith() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public void DoIt() {
    var task = Task.Run(() => 1);
    task.ContinueWith(t => 2);
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Skip(1)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.IsContinuationMethodInvocation(type));
    }

    [TestMethod]
    public void IsContinuationMethodInvocationReturnsFalseForInvokingTaskToString() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public string GetInfo() {
    var task = Task.Run(() => 1);
    return task.ToString();
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Skip(1)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsContinuationMethodInvocation(type));
    }

    [TestMethod]
    public void IsContinuationMethodInvocationReturnsFalseForInvokingContinueWithMethodOfNonTaskType() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public void DoIt() {
    var test = new Test();
    test.ContinueWith(t => 1);
  }

  private class Test {
    public void ContinueWith(Action<Test, object> action) { }
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsContinuationMethodInvocation(type));
    }

    [TestMethod]
    public void IsContinuationMethodInvocationReturnsFalseForInvokingContinueWithMethodOfUnresolvableType() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public void DoIt() {
    var test = new Test();
    test.ContinueWith(t => 2);
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsContinuationMethodInvocation(type));
    }

    [TestMethod]
    public void IsContinuationMethodInvocationReturnsFalseForInvokingUnknownTaskMethod() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public void DoIt() {
    var task = Task.Run(() => 1);
    task.SomeNonExistantMethod();
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Skip(1)
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsContinuationMethodInvocation(type));
    }

    [TestMethod]
    public void IsMethodOrFunctionReturningTaskReturnsTrueForTaskReturningMethod() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public Task DoItAsync() {
    return Task.CompletedTask;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.IsMethodOrFunctionReturningTask(type));
    }

    [TestMethod]
    public void IsMethodOrFunctionReturningTaskReturnsTrueForTaskReturningSimpleLambda() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public void DoIt() {
    Func<Task<int>> test = () => Task.FromResult(1);
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<LambdaExpressionSyntax>()
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.IsMethodOrFunctionReturningTask(type));
    }

    [TestMethod]
    public void IsMethodOrFunctionReturningTaskReturnsFalseForVoidMethod() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public void DoIt() {
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsMethodOrFunctionReturningTask(type));
    }

    [TestMethod]
    public void IsMethodOrFunctionReturningTaskReturnsFalseForMethodWithUnresolvableReturnType() {
      const string source = @"
public class Test {
  public Task DoIt() {
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsMethodOrFunctionReturningTask(type));
    }

    [TestMethod]
    public void IsMethodOrFunctionReturningTaskReturnsFalseForNonTaskReturningMethod() {
      const string source = @"
public class Test {
  public int DoIt() {
    return 1;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var type = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsMethodOrFunctionReturningTask(type));
    }

    [TestMethod]
    public void IsFromResultInvocationReturnsTrueForInvokingTaskFromResult() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public void DoIt() {
    var task = Task.FromResult(1);
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.IsFromResultInvocation(invocation));
    }

    [TestMethod]
    public void IsFromResultInvocationReturnsFalseForInvokingUnknownTaskMethod() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public void DoIt() {
    var task = Task.SomeNotExistantMethod(1);
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsFromResultInvocation(invocation));
    }

    [TestMethod]
    public void IsFromResultInvocationReturnsFalseForInvokingDifferentMethod() {
      const string source = @"
using System;
using System.Threading.Tasks;

public class Test {
  public void DoIt() {
    var task = Task.FromException(new Exception());
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsFromResultInvocation(invocation));
    }



    [TestMethod]
    public void IsCompletedTaskAccessReturnsTrueForAccessingCompletedTask() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public void DoIt() {
    var task = Task.CompletedTask;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var memberAccess = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<MemberAccessExpressionSyntax>()
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.IsCompletedTaskAccess(memberAccess));
    }

    [TestMethod]
    public void IsCompletedTaskAccessReturnsFalseForAccessingUnknownTaskMember() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public void DoIt() {
    var task = Task.SomeNotExistantProperty;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var memberAccess = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<MemberAccessExpressionSyntax>()
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsCompletedTaskAccess(memberAccess));
    }

    [TestMethod]
    public void IsCompletedTaskAccessReturnsFalseForAccessingDifferentMember() {
      const string source = @"
using System;
using System.Threading.Tasks;

public class Test {
  public void DoIt() {
    var task = Task.Factory;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var memberAccess = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<MemberAccessExpressionSyntax>()
        .Single();
      var analysis = new TaskAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsCompletedTaskAccess(memberAccess));
    }
  }
}
