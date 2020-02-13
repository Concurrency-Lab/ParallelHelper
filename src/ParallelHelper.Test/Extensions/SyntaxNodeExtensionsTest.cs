using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParallelHelper.Test.Extensions {
  [TestClass]
  public class SyntaxNodeExtensionsTest {
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
