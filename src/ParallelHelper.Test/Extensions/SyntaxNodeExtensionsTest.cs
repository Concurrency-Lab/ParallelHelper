using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Extensions;
using System.Linq;

namespace ParallelHelper.Test.Extensions {
  [TestClass]
  public class SyntaxNodeExtensionsTest {
    [TestMethod]
    public void IsNewActivationFrameReturnsTrueForMethodDeclarations() {
      const string source = @"
using System;

public class Test {
  public void TestMethod() {}
}";
      var declaration = CompilationFactory.GetNodesOfType<MethodDeclarationSyntax>(source)
        .Single();
      Assert.IsTrue(declaration.IsNewActivationFrame());
    }

    [TestMethod]
    public void IsNewActivationFrameReturnsTrueForLambdaExpressions() {
      const string source = @"
using System;

public class Test {
  public void TestMethod() {
    Action test = () => {};
  }
}";
      var declaration = CompilationFactory.GetNodesOfType<LambdaExpressionSyntax>(source)
        .Single();
      Assert.IsTrue(declaration.IsNewActivationFrame());
    }

    [TestMethod]
    public void IsNewActivationFrameReturnsTrueForDelegates() {
      const string source = @"
using System;

public class Test {
  public void TestMethod() {
    Action test = delegate {};
  }
}";
      var declaration = CompilationFactory.GetNodesOfType<AnonymousMethodExpressionSyntax>(source)
        .Single();
      Assert.IsTrue(declaration.IsNewActivationFrame());
    }

    [TestMethod]
    public void IsNewActivationFrameReturnsTrueForLocalFunction() {
      const string source = @"
using System;

public class Test {
  public void TestMethod() {
    void Test() {}
  }
}";
      var declaration = CompilationFactory.GetNodesOfType<LocalFunctionStatementSyntax>(source)
        .Single();
      Assert.IsTrue(declaration.IsNewActivationFrame());
    }

    [TestMethod]
    public void IsNewActivationFrameReturnsFalseForClassDeclarations() {
      const string source = @"
using System;

public class Test {
}";
      var declaration = CompilationFactory.GetNodesOfType<ClassDeclarationSyntax>(source)
        .Single();
      Assert.IsFalse(declaration.IsNewActivationFrame());
    }

    [TestMethod]
    public void IsNewActivationFrameReturnsFalseForLocalVariableDeclarations() {
      const string source = @"
using System;

public class Test {
  private void TestMethod() {
    int x = 1;
  }
}";
      var declaration = CompilationFactory.GetNodesOfType<LocalDeclarationStatementSyntax>(source)
        .Single();
      Assert.IsFalse(declaration.IsNewActivationFrame());
    }

    [TestMethod]
    public void DescendantNodesInSameActivationFrameExcludesNodesInsideParenthizedLambdaExpressions() {
      const string source = @"
using System;

public class Test {
  public void TestMethod() {
    int x = 1;
    Action lambda = () => {
      x += 1;
    };
    x++;
  }
}";
      var descendants = CompilationFactory.GetNodesOfType<MethodDeclarationSyntax>(source)
        .Single()
        .DescendantNodesInSameActivationFrame()
        .ToArray();
      Assert.AreEqual(0, descendants.OfType<AssignmentExpressionSyntax>().Count());
      Assert.AreEqual(2, descendants.OfType<VariableDeclarationSyntax>().Count());
      Assert.AreEqual(1, descendants.OfType<LambdaExpressionSyntax>().Count());
      Assert.AreEqual(1, descendants.OfType<PostfixUnaryExpressionSyntax>().Count());
    }

    [TestMethod]
    public void DescendantNodesInSameActivationFrameExcludesNodesInsideSimpleLambdaExpressions() {
      const string source = @"
using System;

public class Test {
  public void TestMethod() {
    int x = 1;
    Func<int> lambda = () => x += 1;
    x++;
  }
}";
      var descendants = CompilationFactory.GetNodesOfType<MethodDeclarationSyntax>(source)
        .Single()
        .DescendantNodesInSameActivationFrame()
        .ToArray();
      Assert.AreEqual(0, descendants.OfType<AssignmentExpressionSyntax>().Count());
      Assert.AreEqual(2, descendants.OfType<VariableDeclarationSyntax>().Count());
      Assert.AreEqual(1, descendants.OfType<LambdaExpressionSyntax>().Count());
      Assert.AreEqual(1, descendants.OfType<PostfixUnaryExpressionSyntax>().Count());
    }

    [TestMethod]
    public void DescendantNodesInSameActivationFrameExcludesNodesInsideLocalFunctionDeclarations() {
      const string source = @"
using System;

public class Test {
  public void TestMethod() {
    int x = 1;
    void LocalFunction() {
      x += 2;
    }
    x++;
  }
}";
      var descendants = CompilationFactory.GetNodesOfType<MethodDeclarationSyntax>(source)
        .Single()
        .DescendantNodesInSameActivationFrame()
        .ToArray();
      Assert.AreEqual(0, descendants.OfType<AssignmentExpressionSyntax>().Count());
      Assert.AreEqual(1, descendants.OfType<VariableDeclarationSyntax>().Count());
      Assert.AreEqual(1, descendants.OfType<LocalFunctionStatementSyntax>().Count());
      Assert.AreEqual(1, descendants.OfType<PostfixUnaryExpressionSyntax>().Count());
    }

    [TestMethod]
    public void DescendantNodesInSameActivationFrameExcludesNodesInsideDelegates() {
      const string source = @"
using System;

public class Test {
  public void TestMethod() {
    int x = 1;
    Action doIt = delegate {
      x += 2;
    };
    x++;
  }
}";
      var descendants = CompilationFactory.GetNodesOfType<MethodDeclarationSyntax>(source)
        .Single()
        .DescendantNodesInSameActivationFrame()
        .ToArray();
      Assert.AreEqual(0, descendants.OfType<AssignmentExpressionSyntax>().Count());
      Assert.AreEqual(2, descendants.OfType<VariableDeclarationSyntax>().Count());
      Assert.AreEqual(1, descendants.OfType<AnonymousFunctionExpressionSyntax>().Count());
      Assert.AreEqual(1, descendants.OfType<PostfixUnaryExpressionSyntax>().Count());
    }

    [TestMethod]
    public void DescendantNodesInSameActivationReturnsAllNodesInSameActivationFrame() {
      const string source = @"
using System;

public class Test {
  public void TestMethod() {
    int x = 1;
    x += 2;
    x++;
  }
}";
      var descendants = CompilationFactory.GetNodesOfType<MethodDeclarationSyntax>(source)
        .Single()
        .DescendantNodesInSameActivationFrame()
        .ToArray();
      Assert.AreEqual(1, descendants.OfType<AssignmentExpressionSyntax>().Count());
      Assert.AreEqual(1, descendants.OfType<VariableDeclarationSyntax>().Count());
      Assert.AreEqual(1, descendants.OfType<PostfixUnaryExpressionSyntax>().Count());
    }

    [TestMethod]
    public void DescendantNodesInSameActivationExcludesNodesFilteredByAdditionalCriteria() {
      const string source = @"
using System;

public class Test {
  public void TestMethod() {
    int x = 0;
    int y = 1;
    try {
      int k = 2;
      int l = 3;
    } catch(Exception e) {}
  }
}";
      var descendants = CompilationFactory.GetNodesOfType<MethodDeclarationSyntax>(source)
        .Single()
        .DescendantNodesInSameActivationFrame(node => !(node is TryStatementSyntax))
        .ToArray();
      Assert.AreEqual(2, descendants.OfType<VariableDeclarationSyntax>().Count());
    }

    [TestMethod]
    public void IsMethodOrFunctionWithAsyncModifierReturnsTrueIfMethodIsAsync() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public async Task TestMethodAsync() {}
}";
      var declaration = CompilationFactory.GetNodesOfType<MethodDeclarationSyntax>(source).Single();
      Assert.IsTrue(declaration.IsMethodOrFunctionWithAsyncModifier());
    }

    [TestMethod]
    public void IsMethodOrFunctionWithAsyncModifierReturnsFalseIfMethodIsNotAsync() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public void TestMethod() {}
}";
      var declaration = CompilationFactory.GetNodesOfType<MethodDeclarationSyntax>(source).Single();
      Assert.IsFalse(declaration.IsMethodOrFunctionWithAsyncModifier());
    }

    [TestMethod]
    public void IsMethodOrFunctionWithAsyncModifierReturnsTrueIfLambdaIsAsync() {
      const string source = @"
using System;
using System.Threading.Tasks;

public class Test {
  public void TestMethod() {
    Func<Task> action = async () => {};
  }
}";
      var declaration = CompilationFactory.GetNodesOfType<LambdaExpressionSyntax>(source).Single();
      Assert.IsTrue(declaration.IsMethodOrFunctionWithAsyncModifier());
    }

    [TestMethod]
    public void IsMethodOrFunctionWithAsyncModifierReturnsFalseIfLambdaIsNotAsync() {
      const string source = @"
using System;
using System.Threading.Tasks;

public class Test {
  public void TestMethod() {
    Action action = () => {};
  }
}";
      var declaration = CompilationFactory.GetNodesOfType<LambdaExpressionSyntax>(source).Single();
      Assert.IsFalse(declaration.IsMethodOrFunctionWithAsyncModifier());
    }

    [TestMethod]
    public void IsMethodOrFunctionWithAsyncModifierReturnsTrueIfDelegateIsAsync() {
      const string source = @"
using System;
using System.Threading.Tasks;

public class Test {
  public void TestMethod() {
    Func<Task> action = async delegate {};
  }
}";
      var declaration = CompilationFactory.GetNodesOfType<AnonymousFunctionExpressionSyntax>(source).Single();
      Assert.IsTrue(declaration.IsMethodOrFunctionWithAsyncModifier());
    }

    [TestMethod]
    public void IsMethodOrFunctionWithAsyncModifierReturnsFalseIfDelegateIsNotAsync() {
      const string source = @"
using System;
using System.Threading.Tasks;

public class Test {
  public void TestMethod() {
    Action action = delegate {};
  }
}";
      var declaration = CompilationFactory.GetNodesOfType<AnonymousFunctionExpressionSyntax>(source).Single();
      Assert.IsFalse(declaration.IsMethodOrFunctionWithAsyncModifier());
    }

    [TestMethod]
    public void IsMethodOrFunctionWithAsyncModifierReturnsTrueIfLocalFunctionIsAsync() {
      const string source = @"
using System;
using System.Threading.Tasks;

public class Test {
  public void TestMethod() {
    async Task Action() {}
  }
}";
      var declaration = CompilationFactory.GetNodesOfType<LocalFunctionStatementSyntax>(source).Single();
      Assert.IsTrue(declaration.IsMethodOrFunctionWithAsyncModifier());
    }

    [TestMethod]
    public void IsMethodOrFunctionWithAsyncModifierReturnsFalseIfLocalFunctionIsNotAsync() {
      const string source = @"
using System;
using System.Threading.Tasks;

public class Test {
  public void TestMethod() {
    void Action() {}
  }
}";
      var declaration = CompilationFactory.GetNodesOfType<LocalFunctionStatementSyntax>(source).Single();
      Assert.IsFalse(declaration.IsMethodOrFunctionWithAsyncModifier());
    }

    [TestMethod]
    public void IsMethodOrFunctionWithAsyncModifierReturnsFalseIfSyntaxNodeIsNeither() {
      const string source = @"
using System;
using System.Threading.Tasks;

public class Test {
  public void TestMethod() {
  }
}";
      var declaration = CompilationFactory.GetNodesOfType<ClassDeclarationSyntax>(source).Single();
      Assert.IsFalse(declaration.IsMethodOrFunctionWithAsyncModifier());
    }

    [TestMethod]
    public void GetAllWrittenExpressionsReturnsLeftOperandOfAssignment() {
      const string source = @"
public class Test {
  public void TestMethod() {
    int x;
    x = 1;
    if(x > 0) { }
  }
}";
      var writtenExpressions = CompilationFactory.GetNodesOfType<MethodDeclarationSyntax>(source).Single()
        .GetAllWrittenExpressions(default)
        .ToArray();
      Assert.AreEqual(1, writtenExpressions.Length);
      Assert.AreEqual(1, writtenExpressions.OfType<IdentifierNameSyntax>().Count());
    }

    [TestMethod]
    public void GetAllWrittenExpressionsReturnsFullExpressionOfLeftOperandOfAssignment() {
      const string source = @"
public class Test {
  private int x;

  public void TestMethod() {
    if(x > 0) { }
    this.x = 1;
    if(x > 0) { }
  }
}";
      var writtenExpressions = CompilationFactory.GetNodesOfType<MethodDeclarationSyntax>(source).Single()
        .GetAllWrittenExpressions(default)
        .ToArray();
      Assert.AreEqual(1, writtenExpressions.Length);
      Assert.AreEqual(1, writtenExpressions.OfType<MemberAccessExpressionSyntax>().Count());
    }

    [TestMethod]
    public void GetAllWrittenExpressionsReturnsOperandOfPostfixIncrement() {
      const string source = @"
public class Test {
  private int x;

  public void TestMethod() {
    if(x > 0) { }
    x++;
    if(x > 0) { }
  }
}";
      var writtenExpressions = CompilationFactory.GetNodesOfType<MethodDeclarationSyntax>(source).Single()
        .GetAllWrittenExpressions(default)
        .ToArray();
      Assert.AreEqual(1, writtenExpressions.Length);
      Assert.AreEqual(1, writtenExpressions.OfType<IdentifierNameSyntax>().Count());
    }

    [TestMethod]
    public void GetAllWrittenExpressionsReturnsOperandOfPrefixIncrement() {
      const string source = @"
public class Test {
  private int x;

  public void TestMethod() {
    if(x > 0) { }
    ++x;
    if(x > 0) { }
  }
}";
      var writtenExpressions = CompilationFactory.GetNodesOfType<MethodDeclarationSyntax>(source).Single()
        .GetAllWrittenExpressions(default)
        .ToArray();
      Assert.AreEqual(1, writtenExpressions.Length);
      Assert.AreEqual(1, writtenExpressions.OfType<IdentifierNameSyntax>().Count());
    }

    [TestMethod]
    public void GetAllWrittenExpressionsReturnsExpressionOfRefArgument() {
      const string source = @"
public class Test {
  private int x;

  public void TestMethod() {
    if(x > 0) { }
    Increment(ref x);
    if(x > 0) { }
  }

  private void Increment(ref int x) {
    x++;
  }
}";
      var writtenExpressions = CompilationFactory.GetNodesOfType<MethodDeclarationSyntax>(source)
        .OrderBy(declaration => declaration.GetLocation().SourceSpan)
        .First()
        .GetAllWrittenExpressions(default)
        .ToArray();
      Assert.AreEqual(1, writtenExpressions.Length);
      Assert.AreEqual(1, writtenExpressions.OfType<IdentifierNameSyntax>().Count());
    }

    [TestMethod]
    public void GetAllWrittenExpressionsReturnsExpressionOfOutArgument() {
      const string source = @"
public class Test {
  private int x;

  public void TestMethod() {
    if(x > 0) { }
    Reset(out x);
    if(x > 0) { }
  }

  private void Reset(out int x) {
    x = 0;
  }
}";
      var writtenExpressions = CompilationFactory.GetNodesOfType<MethodDeclarationSyntax>(source)
        .OrderBy(declaration => declaration.GetLocation().SourceSpan)
        .First()
        .GetAllWrittenExpressions(default)
        .ToArray();
      Assert.AreEqual(1, writtenExpressions.Length);
      Assert.AreEqual(1, writtenExpressions.OfType<IdentifierNameSyntax>().Count());
    }

    [TestMethod]
    public void GetAllWrittenExpressionsDoesNotReturnExpressionOfNormalArgument() {
      const string source = @"
public class Test {
  private int x;

  public void TestMethod() {
    if(x > 0) { }
    Compute(x);
    if(x > 0) { }
  }

  private void Compute(int x) {
  }
}";
      var writtenExpressions = CompilationFactory.GetNodesOfType<MethodDeclarationSyntax>(source)
        .OrderBy(declaration => declaration.GetLocation().SourceSpan)
        .First()
        .GetAllWrittenExpressions(default)
        .ToArray();
      Assert.AreEqual(0, writtenExpressions.Length);
    }

    [TestMethod]
    public void GetAllWrittenExpressionsReturnsAllWrittenExpressions() {
      const string source = @"
public class Test {
  private int x;

  public void TestMethod() {
    if(x > 0) { }
    x++;
    Reset(out x);
    if(x > 0) { }
    x *= 5;
  }

  private void Reset(out int x) {
    x = 0;
  }
}";
      var writtenExpressions = CompilationFactory.GetNodesOfType<MethodDeclarationSyntax>(source)
        .OrderBy(declaration => declaration.GetLocation().SourceSpan)
        .First()
        .GetAllWrittenExpressions(default)
        .ToArray();
      Assert.AreEqual(3, writtenExpressions.Length);
    }
  }
}
