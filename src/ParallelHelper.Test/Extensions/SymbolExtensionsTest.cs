using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Extensions;
using System.Linq;

namespace ParallelHelper.Test.Extensions {
  [TestClass]
  public class SymbolExtensionsTest {
    private static ISymbol GetSymbolOfSingleReturnStatement(string source) {
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      return semanticModel.SyntaxTree.GetRoot()
        .DescendantNodesAndSelf()
        .OfType<ReturnStatementSyntax>()
        .Select(statement => statement.Expression)
        .Select(expression => semanticModel.GetSymbolInfo(expression).Symbol)
        .Single();
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
      var symbol = GetSymbolOfSingleReturnStatement(source);
      Assert.IsTrue(symbol.IsVariable());
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
      var symbol = GetSymbolOfSingleReturnStatement(source);
      Assert.IsTrue(symbol.IsVariable());
    }

    [TestMethod]
    public void IsVariableReturnsTrueForParameters() {
      const string source = @"
public class Test {
  public int GetIt(int x) {
    return x;
  }
}";
      var symbol = GetSymbolOfSingleReturnStatement(source);
      Assert.IsTrue(symbol.IsVariable());
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
      var symbol = GetSymbolOfSingleReturnStatement(source);
      Assert.IsFalse(symbol.IsVariable());
    }

    [TestMethod]
    public void IsVariableReturnsFalseForNull() {
      ISymbol? symbol = null;
      Assert.IsFalse(symbol.IsVariable());
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
      var symbol = GetSymbolOfSingleReturnStatement(source);
      Assert.IsFalse(symbol.IsVariable());
    }
  }
}
