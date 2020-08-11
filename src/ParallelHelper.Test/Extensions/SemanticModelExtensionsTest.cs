using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Extensions;
using System;
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
using System;

public class Test {
  public void TestMethod() {
    int value = 1;
    Func<int> increment = () => {
      return value += 1;
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
using System;

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
using System;

public class Test {
  public void TestMethod() {
    int value = 1;
    Func<int> increment = () => {
      return value++;
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
using System;

public class Test {
  public void TestMethod() {
    int value = 1;
    Action increment = () => {
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
using System;

public class Test {
  public void TestMethod() {
    int value = 1;
    Action reset = () => {
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
using System;

public class Test {
  public void TestMethod() {
    Action doIt = () => {
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
using System;

public class Test {
  public void TestMethod() {
    Action doIt = () => {
      int value = 1;
      Action inner = () => {
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

    [TestMethod]
    public void TryGetMethodSymbolFromMethodOrFunctionDeclarationReturnsMethodSymbolForMethodDeclaration() {
      const string source = @"
using System;

public class Test {
  public void TestMethod() {
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var method = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .Single();
      Assert.IsTrue(semanticModel.TryGetMethodSymbolFromMethodOrFunctionDeclaration(method, out var symbol, default));
      Assert.IsNotNull(symbol);
    }

    [TestMethod]
    public void TryGetMethodSymbolFromMethodOrFunctionDeclarationReturnsMethodSymbolForLambdaExpression() {
      const string source = @"
using System;

public class Test {
  public void TestMethod() {
    Func<int> doIt = () => 1;
    doIt();
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var method = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ParenthesizedLambdaExpressionSyntax>()
        .Single();
      Assert.IsTrue(semanticModel.TryGetMethodSymbolFromMethodOrFunctionDeclaration(method, out var symbol, default));
      Assert.IsNotNull(symbol);
    }

    [TestMethod]
    public void TryGetMethodSymbolFromMethodOrFunctionDeclarationReturnsMethodSymbolForDelegate() {
      const string source = @"
using System;

public class Test {
  public void TestMethod() {
    Func<int> doIt = delegate {
      return 1;
    };
    doIt();
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var method = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<AnonymousFunctionExpressionSyntax>()
        .Single();
      Assert.IsTrue(semanticModel.TryGetMethodSymbolFromMethodOrFunctionDeclaration(method, out var symbol, default));
      Assert.IsNotNull(symbol);
    }

    [TestMethod]
    public void TryGetMethodSymbolFromMethodOrFunctionDeclarationReturnsMethodSymbolForLocalFunction() {
      const string source = @"
using System;

public class Test {
  public void TestMethod() {
    void DoIt() {
    }
    DoIt();
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var method = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<LocalFunctionStatementSyntax>()
        .Single();
      Assert.IsTrue(semanticModel.TryGetMethodSymbolFromMethodOrFunctionDeclaration(method, out var symbol, default));
      Assert.IsNotNull(symbol);
    }

    [TestMethod, ExpectedException(typeof(ArgumentException))]
    public void TryGetMethodSymbolFromMethodOrFunctionDeclarationThrowsArgumentExceptionForVariableDeclaration() {
      const string source = @"
using System;

public class Test {
  public void TestMethod() {
    int test = 1;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var method = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<VariableDeclaratorSyntax>()
        .Single();
      semanticModel.TryGetMethodSymbolFromMethodOrFunctionDeclaration(method, out var _, default);
    }

    [TestMethod]
    public void IsVariableReturnsTrueForLocalVariables() {
      const string source = @"
public class Test {
  public int GetIt() {
    var x = 1;
    return x;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var expression = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ReturnStatementSyntax>()
        .Select(statement => statement.Expression)
        .Single();
      Assert.IsTrue(semanticModel.IsVariable(expression, default));
    }

    [TestMethod]
    public void IsVariableReturnsTrueForFields() {
      const string source = @"
public class Test {
  private int x = 1;

  public int GetIt() {
    return x;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var expression = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ReturnStatementSyntax>()
        .Select(statement => statement.Expression)
        .Single();
      Assert.IsTrue(semanticModel.IsVariable(expression, default));
    }

    [TestMethod]
    public void IsVariableReturnsTrueForParameters() {
      const string source = @"
public class Test {
  public int GetIt(int x) {
    return x;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var expression = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ReturnStatementSyntax>()
        .Select(statement => statement.Expression)
        .Single();
      Assert.IsTrue(semanticModel.IsVariable(expression, default));
    }

    [TestMethod]
    public void IsVariableReturnsFalseForProperties() {
      const string source = @"
public class Test {
  private int X { get; set };

  public int GetIt() {
    return X;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var expression = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ReturnStatementSyntax>()
        .Select(statement => statement.Expression)
        .Single();
      Assert.IsFalse(semanticModel.IsVariable(expression, default));
    }

    [TestMethod]
    public void IsVariableReturnsFalseForMethods() {
      const string source = @"
public class Test {
  public int GetIt() {
    return GetItInternal();
  }

  private int GetItInternal() => 1;
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var expression = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ReturnStatementSyntax>()
        .Select(statement => statement.Expression)
        .Single();
      Assert.IsFalse(semanticModel.IsVariable(expression, default));
    }

    [TestMethod]
    public void IsVariableReturnsFalseForLiterals() {
      const string source = @"
public class Test {
  public int GetIt() {
    return 1;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var expression = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ReturnStatementSyntax>()
        .Select(statement => statement.Expression)
        .Single();
      Assert.IsFalse(semanticModel.IsVariable(expression, default));
    }

    [TestMethod]
    public void IsVariableReturnsFalseForUnresolvableIdentifiers() {
      const string source = @"
public class Test {
  public int GetIt() {
    return notExistant;
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var expression = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ReturnStatementSyntax>()
        .Select(statement => statement.Expression)
        .Single();
      Assert.IsFalse(semanticModel.IsVariable(expression, default));
    }

    [TestMethod]
    public void IsVariableReturnsFalseForClassNames() {
      const string source = @"
using System;

public class Test {
  public Type GetIt() {
    return typeof(Test);
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var expression = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<TypeOfExpressionSyntax>()
        .Select(expression => expression.Type)
        .Single();
      Assert.IsFalse(semanticModel.IsVariable(expression, default));
    }

    [TestMethod]
    public void IsVariableReturnsFalseForMethodNames() {
      const string source = @"
using System;

public class Test {
  public Action GetIt() {
    return GetItInternal;
  }

  private void GetItInternal() { }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var expression = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ReturnStatementSyntax>()
        .Select(statement => statement.Expression)
        .Single();
      Assert.IsFalse(semanticModel.IsVariable(expression, default));
    }
  }
}
