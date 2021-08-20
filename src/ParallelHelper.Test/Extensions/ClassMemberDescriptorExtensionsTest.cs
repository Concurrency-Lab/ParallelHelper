using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer;
using ParallelHelper.Extensions;
using System.Linq;

namespace ParallelHelper.Test.Extensions {
  [TestClass]
  public class ClassMemberDescriptorExtensionsTest {
    private static readonly ClassMemberDescriptor[] TestDescriptors = {
      new ClassMemberDescriptor("System.Threading.Thread", "Start", "StartWithTypo", "Abort", "Name"),
      new ClassMemberDescriptor("System.Threading.Tasks.Task", "Run", "RunWithTypo", "Wait", "Status"),
      new ClassMemberDescriptor("System.String", "Empty"),
    };

    [TestMethod]
    public void AnyContainsMemberReturnsTrueForExistingTypeAndMethod() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public static Task DoAsTask(Action action) {
    return Task.Run(action);
  }
}
";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var method = semanticModel.SyntaxTree.GetRoot().DescendantNodesAndSelf()
        .OfType<InvocationExpressionSyntax>()
        .Select(expression => semanticModel.GetSymbolInfo(expression).Symbol)
        .Cast<IMethodSymbol>()
        .Single();
      Assert.IsTrue(TestDescriptors.AnyContainsMember(semanticModel, method));
    }

    [TestMethod]
    public void AnyContainsMemberReturnsFalseForUnresolvableTypeAndMethod() {
      const string source = @"
using System;

class Test {
  public static Task DoAsTask(Action action) {
    return Task.Run(action);
  }
}
";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var method = semanticModel.SyntaxTree.GetRoot().DescendantNodesAndSelf()
        .OfType<InvocationExpressionSyntax>()
        .Select(expression => semanticModel.GetSymbolInfo(expression).Symbol)
        .Cast<IMethodSymbol>()
        .Single();
      Assert.IsFalse(TestDescriptors.AnyContainsMember(semanticModel, method));
    }

    [TestMethod]
    public void AnyContainsMemberReturnsFalseForUnresolvableMethod() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public static Task DoAsTask(Action action) {
    return Task.RunWithTypo(action);
  }
}
";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var method = semanticModel.SyntaxTree.GetRoot().DescendantNodesAndSelf()
        .OfType<InvocationExpressionSyntax>()
        .Select(expression => semanticModel.GetSymbolInfo(expression).Symbol)
        .Cast<IMethodSymbol>()
        .Single();
      Assert.IsFalse(TestDescriptors.AnyContainsMember(semanticModel, method));
    }

    [TestMethod]
    public void AnyContainsMemberReturnsFalseForExistingTypeWithMethodOfOtherDescriptor() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public static Task DoAsTask(Action action) {
    return Task.Start(action);
  }
}
";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var method = semanticModel.SyntaxTree.GetRoot().DescendantNodesAndSelf()
        .OfType<InvocationExpressionSyntax>()
        .Select(expression => semanticModel.GetSymbolInfo(expression).Symbol)
        .Cast<IMethodSymbol>()
        .Single();
      Assert.IsFalse(TestDescriptors.AnyContainsMember(semanticModel, method));
    }

    [TestMethod]
    public void AnyContainsMemberReturnsTrueForExistingTypeAndProperty() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public static TaskStatus GetStatus(Action action) {
    return task.Status;
  }
}
";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var property = semanticModel.SyntaxTree.GetRoot().DescendantNodesAndSelf()
        .OfType<MemberAccessExpressionSyntax>()
        .Select(expression => semanticModel.GetSymbolInfo(expression).Symbol)
        .Cast<IPropertySymbol>()
        .Single();
      Assert.IsFalse(TestDescriptors.AnyContainsMember(semanticModel, property));
    }

    [TestMethod]
    public void AnyContainsMemberReturnsTrueForExistingTypeAndField() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public static string EmptyString() {
    return string.Empty;
  }
}
";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var field = semanticModel.SyntaxTree.GetRoot().DescendantNodesAndSelf()
        .OfType<MemberAccessExpressionSyntax>()
        .Select(expression => semanticModel.GetSymbolInfo(expression).Symbol)
        .Cast<IFieldSymbol>()
        .Single();
      Assert.IsTrue(TestDescriptors.AnyContainsMember(semanticModel, field));
    }

    [TestMethod]
    public void AnyContainsInvokedMethodReturnsTrueForExistingTypeAndMethod() {
      const string source = @"
using System;
using System.Threading;

class Test {
  public static Thread DoAsThread(Action action) {
    var thread = new Thread(action);
    thread.Start();
    return thread;
  }
}
";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot().DescendantNodesAndSelf()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      Assert.IsTrue(TestDescriptors.AnyContainsInvokedMethod(semanticModel, invocation, default));
    }

    [TestMethod]
    public void AnyContainsInvokedMethodReturnsFalseForUnresolvableTypeAndMethod() {
      const string source = @"
using System;

class Test {
  public static Thread DoAsThread(Action action) {
    var thread = new Thread(action);
    thread.Start();
    return thread;
  }
}
";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot().DescendantNodesAndSelf()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      Assert.IsFalse(TestDescriptors.AnyContainsInvokedMethod(semanticModel, invocation, default));
    }

    [TestMethod]
    public void AnyContainsInvokedMethodReturnsFalseForUnresolvableMethod() {
      const string source = @"
using System;
using System.Threading;

class Test {
  public static Thread DoAsThread(Action action) {
    var thread = new Thread(action);
    thread.StartWithTypo();
    return thread;
  }
}
";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot().DescendantNodesAndSelf()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      Assert.IsFalse(TestDescriptors.AnyContainsInvokedMethod(semanticModel, invocation, default));
    }
  }
}
