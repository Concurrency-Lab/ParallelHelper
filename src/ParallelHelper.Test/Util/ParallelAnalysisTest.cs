using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Util;
using System.Linq;

namespace ParallelHelper.Test.Util {
  [TestClass]
  public class ParallelAnalysisTest {
    [TestMethod]
    public void TryGetParallelForOrForEachDelegateReturnsFalseWhenForOfNoneParallelType() {
      const string source = @"
public class Test {
  public void Run() {
    Parallel.For(0, 10, x => {});
  }
}

public static class Parallel {
  public void For(int start, int end, Action<int> action) {}
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new ParallelAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.TryGetParallelForOrForEachDelegate(invocation, out var _));
    }

    [TestMethod]
    public void TryGetParallelForOrForEachDelegateReturnsFalseWhenForEachOfNoneParallelType() {
      const string source = @"
public class Test {
  public void Run() {
    Parallel.ForEach(new int[0], x => {});
  }
}

public static class Parallel {
  public void ForEach<TSource>(IEnumerable<TSource>, Action<TSource> action) {}
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new ParallelAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.TryGetParallelForOrForEachDelegate(invocation, out var _));
    }

    [TestMethod]
    public void TryGetParallelForOrForEachDelegateReturnsFalseWhenTypeOfForCannotBeResolved() {
      const string source = @"
public class Test {
  public void Run() {
    Parallel.For(0, 10, x => {});
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new ParallelAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.TryGetParallelForOrForEachDelegate(invocation, out var _));
    }

    [TestMethod]
    public void TryGetParallelForOrForEachDelegateReturnsFalseWhenTypeOfForEachCannotBeResolved() {
      const string source = @"
public class Test {
  public void Run() {
    Parallel.ForEach(new int[0], x => {});
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new ParallelAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.TryGetParallelForOrForEachDelegate(invocation, out var _));
    }

    [TestMethod]
    public void TryGetParallelForOrForEachDelegateReturnsTrueForParallelForInvocation() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public void Run() {
    Parallel.For(0, 10, x => {});
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new ParallelAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.TryGetParallelForOrForEachDelegate(invocation, out var _));
    }

    [TestMethod]
    public void TryGetParallelForOrForEachDelegateReturnsTrueForParallelForEachInvocation() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public void Run() {
    Parallel.ForEach(new int[0], x => {});
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new ParallelAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.TryGetParallelForOrForEachDelegate(invocation, out var _));
    }

    [TestMethod]
    public void TryGetParallelForOrForEachDelegateSetsExpressionToDelegateForParallelFor() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public void Run() {
    Parallel.For(0, 10, x => {});
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new ParallelAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.TryGetParallelForOrForEachDelegate(invocation, out var action));
      Assert.IsInstanceOfType(action, typeof(LambdaExpressionSyntax));
    }

    [TestMethod]
    public void TryGetParallelForOrForEachDelegateSetsExpressionToDelegateForParallelForEach() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public void Run() {
    Parallel.ForEach(new int[0], x => {});
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new ParallelAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.TryGetParallelForOrForEachDelegate(invocation, out var action));
      Assert.IsInstanceOfType(action, typeof(LambdaExpressionSyntax));
    }

    [TestMethod]
    public void IsParallelForReturnsFalseWhenForOfNoneParallelType() {
      const string source = @"
public class Test {
  public void Run() {
    Parallel.For(0, 10, x => {});
  }
}

public static class Parallel {
  public void For(int start, int end, Action<int> action) {}
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new ParallelAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsParallelFor(invocation));
    }

    [TestMethod]
    public void IsParallelForReturnsFalseWhenTypeOfForCannotBeResolved() {
      const string source = @"
public class Test {
  public void Run() {
    Parallel.For(0, 10, x => {});
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new ParallelAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsParallelFor(invocation));
    }

    [TestMethod]
    public void IsParallelForReturnsFalseForParallelForEachInvocation() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public void Run() {
    Parallel.For(new int[0], x => {});
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new ParallelAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsParallelFor(invocation));
    }

    [TestMethod]
    public void IsParallelForReturnsTrueForParallelForInvocation() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public void Run() {
    Parallel.For(0, 10, x => {});
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new ParallelAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.IsParallelFor(invocation));
    }

    [TestMethod]
    public void IsParallelForEachReturnsFalseWhenForEachOfNoneParallelType() {
      const string source = @"
public class Test {
  public void Run() {
    Parallel.ForEach(new int[0], x => {});
  }
}

public static class Parallel {
  public void ForEach<TSource>(IEnumerable<TSource>, Action<TSource> action) {}
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new ParallelAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsParallelForEach(invocation));
    }

    [TestMethod]
    public void IsParallelForEachReturnsFalseWhenTypeOfForEachCannotBeResolved() {
      const string source = @"
public class Test {
  public void Run() {
    Parallel.ForEach(new int[0], x => {});
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new ParallelAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsParallelForEach(invocation));
    }

    [TestMethod]
    public void IsParallelForEachReturnsFalseForParallelForInvocation() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public void Run() {
    Parallel.For(0, 10, x => {});
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new ParallelAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsParallelForEach(invocation));
    }

    [TestMethod]
    public void IsParallelForEachReturnsTrueForParallelForEachInvocation() {
      const string source = @"
using System.Threading.Tasks;

public class Test {
  public void Run() {
    Parallel.ForEach(new int[0], x => {});
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new ParallelAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.IsParallelForEach(invocation));
    }
  }
}
