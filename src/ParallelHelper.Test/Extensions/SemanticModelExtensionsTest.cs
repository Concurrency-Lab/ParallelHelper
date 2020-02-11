using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Extensions;
using System.Linq;

namespace ParallelHelper.Test.Extensions {
  [TestClass]
  public class SemanticModelExtensionsTest {
    private static ITypeSymbol GetReturnTypeOfSingleMethodDeclaration(SemanticModel semanticModel) {
      return semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .Select(declaration => semanticModel.GetDeclaredSymbol(declaration).ReturnType)
        .Single();
    }

    [TestMethod]
    public void IsEqualTypeReturnsTrueIfTypeMatches() {
      const string source = @"
using System.Threading;

public class Test {
  public Thread GetThread() {
    return new Thread(() => {});
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var threadType = GetReturnTypeOfSingleMethodDeclaration(semanticModel);
      Assert.IsTrue(semanticModel.IsEqualType(threadType, "System.Threading.Thread"));
    }

    [TestMethod]
    public void IsEqualTypeReturnsFalseIfTypeDoesNotMatch() {
      const string source = @"
using System.Threading;

public class Test {
  public Thread GetThread() {
    return new Thread(() => {});
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var threadType = GetReturnTypeOfSingleMethodDeclaration(semanticModel);
      Assert.IsFalse(semanticModel.IsEqualType(threadType, "System.Threading.SemaphoreSlim"));
    }

    [TestMethod]
    public void IsEqualTypeReturnsFalseIfTypeCannotBeResolved() {
      const string source = @"
using System.Threading;

public class Test {
  public Thread GetThread() {
    return new Thread(() => {});
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var threadType = GetReturnTypeOfSingleMethodDeclaration(semanticModel);
      Assert.IsFalse(semanticModel.IsEqualType(threadType, "ParallelHelper.Type.Not.Existing"));
    }

    [TestMethod]
    public void IsEqualTypeReturnsTrueIfGenericTypeMatches() {
      const string source = @"
using System.Collections.Generic;

public class Test {
  public List<string> ListMethod() {
    return new List<string>();
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var listType = GetReturnTypeOfSingleMethodDeclaration(semanticModel);
      Assert.IsTrue(semanticModel.IsEqualType(listType, "System.Collections.Generic.List`1"));
    }

    [TestMethod]
    public void IsEqualTypeReturnsFalseIfGenericTypeDoesNotMatch() {
      const string source = @"
using System.Collections.Generic;

public class Test {
  public List<string> ListMethod() {
    return new List<string>();
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var listType = GetReturnTypeOfSingleMethodDeclaration(semanticModel);
      Assert.IsFalse(semanticModel.IsEqualType(listType, "System.Collections.Generic.HashSet`1"));
    }

    [TestMethod]
    public void GetTypesByNameReturnsSearchedType() {
      const string source = "public class Test {}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var stringTypes = semanticModel.GetTypesByName("System.String").ToArray();
      Assert.AreEqual(1, stringTypes.Length);
      var stringType = stringTypes.Single();
      Assert.AreEqual("System", stringType.ContainingNamespace.Name);
      Assert.AreEqual("String", stringType.Name);
    }

    [TestMethod]
    public void GetTypesByNameResolvesTaskType() {
      const string source = "public class Test {}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var taskTypes = semanticModel.GetTypesByName("System.Threading.Tasks.Task").ToArray();
      var taskType = taskTypes.First();
      Assert.AreEqual("Tasks", taskType.ContainingNamespace.Name);
      Assert.AreEqual("Task", taskType.Name);
    }

    [TestMethod]
    public void GetTypesByNameReturnsEmptyEnumerableIfTypeCannotBeResolved() {
      const string source = "public class Test {}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var nonExistantTypes = semanticModel.GetTypesByName("ParallelHelper.Type.Not.Existing").ToArray();
      Assert.AreEqual(0, nonExistantTypes.Length);
    }

    [TestMethod]
    public void HasSideEffectsReturnsTrueIfLocalVariableIsDeclaredOutsideButAssignedInside() {
      const string source = @"
public class Test {
  public void TestMethod() {
    int value = 1;
    Func<int> increment = () => {
      value += 1;
    };
    increment();
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var lambda = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<LambdaExpressionSyntax>()
        .Single();
      Assert.IsTrue(semanticModel.HasSideEffects(lambda));
    }

    [TestMethod]
    public void HasSideEffectsReturnsFalseIfLocalVariableIsDeclaredOutsideButOnlyReadInside() {
      const string source = @"
public class Test {
  public void TestMethod() {
    int value = 1;
    Func<int> getValue = () => {
      return value;
    };
    getValue();
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var lambda = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<LambdaExpressionSyntax>()
        .Single();
      Assert.IsFalse(semanticModel.HasSideEffects(lambda));
    }

    [TestMethod]
    public void HasSideEffectsReturnsTrueIfLocalVariableIsDeclaredOutsideButIncrementedInside() {
      const string source = @"
public class Test {
  public void TestMethod() {
    int value = 1;
    Func<int> increment = () => {
      value++;
    };
    increment();
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var lambda = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<LambdaExpressionSyntax>()
        .Single();
      Assert.IsTrue(semanticModel.HasSideEffects(lambda));
    }

    [TestMethod]
    public void HasSideEffectsReturnsTrueIfLocalVariableIsDeclaredOutsideButChangedWithRefArgumentInside() {
      const string source = @"
public class Test {
  public void TestMethod() {
    int value = 1;
    Func<int> increment = () => {
      Increment(ref value);
    };
    increment();
  }

  private void Increment(ref int value) {
    value++;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var lambda = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<LambdaExpressionSyntax>()
        .Single();
      Assert.IsTrue(semanticModel.HasSideEffects(lambda));
    }

    [TestMethod]
    public void HasSideEffectsReturnsTrueIfLocalVariableIsDeclaredOutsideButChangedWithOutArgumentInside() {
      const string source = @"
public class Test {
  public void TestMethod() {
    int value = 1;
    Func<int> reset = () => {
      Reset(out value);
    };
    reset();
  }

  private void Reset(out int value) {
    value = 0;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var lambda = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<LambdaExpressionSyntax>()
        .Single();
      Assert.IsTrue(semanticModel.HasSideEffects(lambda));
    }

    [TestMethod]
    public void HasSideEffectsReturnsFalseIfLocalVariableIsDeclaredAndAssignedInside() {
      const string source = @"
public class Test {
  public void TestMethod() {
    Func<int> doIt = () => {
      int value = 1;
      value += 2;
    };
    doIt();
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var lambda = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<LambdaExpressionSyntax>()
        .Single();
      Assert.IsFalse(semanticModel.HasSideEffects(lambda));
    }

    [TestMethod]
    public void HasSideEffectsReturnsFalseIfLocalVariableIsDeclaredInOuterAndChangedInInnerLambda() {
      const string source = @"
public class Test {
  public void TestMethod() {
    Func<int> doIt = () => {
      int value = 1;
      Func<int> inner = () => {
        value = 0;
      };
    };
    doIt();
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var lambda = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<LambdaExpressionSyntax>()
        .OrderBy(lambda => lambda.GetLocation().SourceSpan)
        .First();
      Assert.IsFalse(semanticModel.HasSideEffects(lambda));
    }
  }
}
