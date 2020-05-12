using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Extensions;
using System.Linq;

namespace ParallelHelper.Test.Extensions {
  [TestClass]
  public class MethodSymbolExtensionsTest {
    private static IMethodSymbol GetParameterLessMethod(string source) {
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var methodDeclaration = semanticModel.SyntaxTree
        .GetRoot()
        .DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .Where(declaration => declaration.ParameterList.Parameters.Count == 0)
        .Single();
      return semanticModel.GetDeclaredSymbol(methodDeclaration);
    }
    private static IMethodSymbol GetMethodOfClass(string source, string className) {
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var methodDeclaration = semanticModel.SyntaxTree
        .GetRoot()
        .DescendantNodes()
        .OfType<ClassDeclarationSyntax>()
        .Where(declaration => declaration.Identifier.Text == className)
        .SelectMany(declaration => declaration.Members)
        .OfType<MethodDeclarationSyntax>()
        .Single();
      return semanticModel.GetDeclaredSymbol(methodDeclaration);
    }

    [TestMethod]
    public void GetAllOverloadsReturnsAllAvailableOverloads() {
      const string source = @"
public class Test {
  public void Overloaded() { }
  public void Overloaded(int x) { }
  public void Overloaded(string x) { }
  public void Overloaded(int x, string y) { }
}";
      var symbol = GetParameterLessMethod(source);
      Assert.AreEqual(4, symbol.GetAllOverloads(default).Count());
    }

    [TestMethod]
    public void GetAllOverloadsReturnsExcludesMethodsWithDifferentName() {
      const string source = @"
public class Test {
  public void Overloaded() { }
  public void Overloaded(int x) { }
  public void Overloaded(string x) { }
  public void Overloaded(int x, string y) { }

  public void Overloaded2(int x) { }
  public void Overloaded2(string x) { }
  public void Overloaded2(int x, string y) { }
}";
      var symbol = GetParameterLessMethod(source);
      Assert.AreEqual(4, symbol.GetAllOverloads(default).Count());
    }

    [TestMethod]
    public void GetAllOverloadsIncludesTheOriginalMethod() {
      const string source = @"
public class Test {
  public void Overloaded() { }
  public void Overloaded(int x) { }
  public void Overloaded(string x) { }
  public void Overloaded(int x, string y) { }
}";
      var symbol = GetParameterLessMethod(source);
      Assert.IsTrue(symbol.GetAllOverloads(default).Contains(symbol));
    }

    [TestMethod]
    public void GetAllOverloadsExcludesStaticIfInstanceMethodIsGiven() {
      const string source = @"
public class Test {
  public void Overloaded() { }
  public void Overloaded(int x) { }
  public static void Overloaded(string x) { }
  public void Overloaded(int x, string y) { }
}";
      var symbol = GetParameterLessMethod(source);
      Assert.AreEqual(3, symbol.GetAllOverloads(default).Count());
    }

    [TestMethod]
    public void GetAllOverloadsExcludesInstanceIfStaticMethodIsGiven() {
      const string source = @"
public class Test {
  public static void Overloaded() { }
  public void Overloaded(int x) { }
  public static void Overloaded(string x) { }
  public void Overloaded(int x, string y) { }
}";
      var symbol = GetParameterLessMethod(source);
      Assert.AreEqual(2, symbol.GetAllOverloads(default).Count());
    }

    [TestMethod]
    public void IsInterfaceImplementationReturnsTrueForDirectInterfaceImplementations() {
      const string source = @"
public interface ITest {
  void DoIt(string a, int b, object c);
}

public class Test : ITest {
  public void DoIt(string a, int b, object c) { }
}";
      var symbol = GetMethodOfClass(source, "Test");
      Assert.IsTrue(symbol.IsInterfaceImplementation(default));
    }

    [TestMethod]
    public void IsInterfaceImplementationReturnsFalseForNotMatchingParameterTypes() {
      const string source = @"
public interface ITest {
  void DoIt(string a, int b, object c);
}

public class Test : ITest {
  public void DoIt(string a, int b, string c) { }
}";
      var symbol = GetMethodOfClass(source, "Test");
      Assert.IsFalse(symbol.IsInterfaceImplementation(default));
    }

    [TestMethod]
    public void IsInterfaceImplementationReturnsFalseForDifferentMethodNames() {
      const string source = @"
public interface ITest {
  void DoIt(string a, int b, object c);
}

public class Test : ITest {
  public void DoIt2(string a, int b, string c) { }
}";
      var symbol = GetMethodOfClass(source, "Test");
      Assert.IsFalse(symbol.IsInterfaceImplementation(default));
    }

    [TestMethod]
    public void IsInterfaceImplementationReturnsFalseForDifferentParameterLengths() {
      const string source = @"
public interface ITest {
  void DoIt(string a, int b, object c);
}

public class Test : ITest {
  public void DoIt(string a, int b, object c, float x) { }
}";
      var symbol = GetMethodOfClass(source, "Test");
      Assert.IsFalse(symbol.IsInterfaceImplementation(default));
    }

    [TestMethod]
    public void IsInterfaceImplementationReturnsFalseNoInterfaceImplementation() {
      const string source = @"
public interface ITest {
  void DoIt(string a, int b, object c);
}

public class Test {
  public void DoIt(string a, int b, object c) { }
}";
      var symbol = GetMethodOfClass(source, "Test");
      Assert.IsFalse(symbol.IsInterfaceImplementation(default));
    }
  }
}
