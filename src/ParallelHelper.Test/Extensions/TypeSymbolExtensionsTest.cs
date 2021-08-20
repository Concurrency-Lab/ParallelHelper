using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Extensions;
using System;
using System.Linq;

namespace ParallelHelper.Test.Extensions {
  [TestClass]
  public class TypeSymbolExtensionsTest {
    private const string BaseTypeTestSource = @"
public class A { }
public class B : A { }
public class C : B { }";

    private const string AccessibleMemberTestSource = @"
public class A {
  int defFieldA;
  private int privFieldA;
  protected int protFieldA;
  internal int intFieldA;
  public int pubFieldA;

  int DefPropertyA { get; set; }
  private int PrivPropertyA { get; set; }
  protected int ProtPropertyA { get; set; }
  internal int IntPropertyA { get; set; }
  public int PubPropertyA { get; set; }

  void DefMethodA() {}
  private void PrivMethodA() {}
  protected void ProtMethodA() {}
  internal void IntMethodA() {}
  public void PubMethodA() {}
}

public class B : A {
  int defFieldB;
  private int privFieldB;
  protected int protFieldB;
  internal int intFieldB;
  public int pubFieldB;

  int DefPropertyB { get; set; }
  private int PrivPropertyB { get; set; }
  protected int ProtPropertyB { get; set; }
  internal int IntPropertyB { get; set; }
  public int PubPropertyB { get; set; }

  void DefMethodB() {}
  private void PrivMethodB() {}
  protected void ProtMethodB() {}
  internal void IntMethodB() {}
  public void PubMethodB() {}
}

public class C : B {
  int defFieldC;
  private int privFieldC;
  protected int protFieldC;
  internal int intFieldC;
  public int pubFieldC;

  int DefPropertyC { get; set; }
  private int PrivPropertyC { get; set; }
  protected int ProtPropertyC { get; set; }
  internal int IntPropertyC { get; set; }
  public int PubPropertyC { get; set; }

  void DefMethodC() {}
  private void PrivMethodC() {}
  protected void ProtMethodC() {}
  internal void IntMethodC() {}
  public void PubMethodC() {}
}";

    private static ITypeSymbol GetTypeSymbolByClassName(string source, string className) {
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      return semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ClassDeclarationSyntax>()
        .Where(declaration => declaration.Identifier.Text.Equals(className))
        .Select(declaration => semanticModel.GetDeclaredSymbol(declaration))
        .Single();
    }

    [TestMethod]
    public void GetAllBaseTypesAndSelfIncludesSelf() {
      const string source = BaseTypeTestSource;
      var typeSymbol = GetTypeSymbolByClassName(source, "C");
      var baseTypesAndSelf = typeSymbol.GetAllBaseTypesAndSelf();
      Assert.IsTrue(baseTypesAndSelf.Contains(typeSymbol));
    }

    [TestMethod]
    public void GetAllBaseTypesAndSelfIncludesAllBaseTypes() {
      const string source = BaseTypeTestSource;
      var typeSymbol = GetTypeSymbolByClassName(source, "C");
      var baseTypesAndSelf = typeSymbol.GetAllBaseTypesAndSelf().ToArray();
      Assert.AreEqual(4, baseTypesAndSelf.Length);
      Assert.IsTrue(baseTypesAndSelf.Any(type => type.Name.Equals("C")));
      Assert.IsTrue(baseTypesAndSelf.Any(type => type.Name.Equals("B")));
      Assert.IsTrue(baseTypesAndSelf.Any(type => type.Name.Equals("A")));
      Assert.IsTrue(baseTypesAndSelf.Any(type => type.Name.Equals("Object")));
    }

    [TestMethod]
    public void GetAllBaseTypesAndSelfReturnsObjectAsImplicitBaseType() {
      const string source = "public class A { }";
      var typeSymbol = GetTypeSymbolByClassName(source, "A");
      var baseTypesAndSelf = typeSymbol.GetAllBaseTypesAndSelf().ToArray();
      Assert.AreEqual(2, baseTypesAndSelf.Length);
      Assert.IsTrue(baseTypesAndSelf.Any(type => type.Name.Equals("Object")));
    }

    [TestMethod]
    public void GetAllBaseTypesExcludesSelf() {
      var typeSymbol = GetTypeSymbolByClassName(BaseTypeTestSource, "C");
      var baseTypesAndSelf = typeSymbol.GetAllBaseTypes();
      Assert.IsFalse(baseTypesAndSelf.Contains(typeSymbol));
    }

    [TestMethod]
    public void GetAllBaseTypesIncludesAllBaseTypes() {
      var typeSymbol = GetTypeSymbolByClassName(BaseTypeTestSource, "C");
      var baseTypesAndSelf = typeSymbol.GetAllBaseTypes().ToArray();
      Assert.AreEqual(3, baseTypesAndSelf.Length);
      Assert.IsTrue(baseTypesAndSelf.Any(type => type.Name.Equals("B")));
      Assert.IsTrue(baseTypesAndSelf.Any(type => type.Name.Equals("A")));
      Assert.IsTrue(baseTypesAndSelf.Any(type => type.Name.Equals("Object")));
    }

    [TestMethod]
    public void GetAllBaseTypesReturnsObjectAsImplicitBaseType() {
      const string source = "public class A { }";
      var typeSymbol = GetTypeSymbolByClassName(source, "A");
      var baseTypesAndSelf = typeSymbol.GetAllBaseTypes().ToArray();
      Assert.AreEqual(1, baseTypesAndSelf.Length);
      Assert.IsTrue(baseTypesAndSelf.Any(type => type.Name.Equals("Object")));
    }

    [TestMethod]
    public void GetAllBaseTypesReturnsEmptySetForSystemObject() {
      const string source = "public class A { }";
      var typeSymbol = GetTypeSymbolByClassName(source, "A").BaseType;
      var baseTypesAndSelf = typeSymbol.GetAllBaseTypes().ToArray();
      Assert.AreEqual(0, baseTypesAndSelf.Length);
    }

    [TestMethod]
    public void GetAllAccessibleMembersReturnsAllMembersOfSelf() {
      var typeSymbol = GetTypeSymbolByClassName(AccessibleMemberTestSource, "C");
      var accessibleMembers = typeSymbol.GetAllAccessibleMembers().ToArray();
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("defFieldC")));
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("privFieldC")));
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("protFieldC")));
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("intFieldC")));
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("pubFieldC")));

      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("DefPropertyC")));
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("PrivPropertyC")));
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("ProtPropertyC")));
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("IntPropertyC")));
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("PubPropertyC")));

      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("DefMethodC")));
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("PrivMethodC")));
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("ProtMethodC")));
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("IntMethodC")));
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("PubMethodC")));
    }

    [TestMethod]
    public void GetAllAccessibleMembersReturnsAllNonPrivateMembersOfAllBaseTypes() {
      var typeSymbol = GetTypeSymbolByClassName(AccessibleMemberTestSource, "C");
      var accessibleMembers = typeSymbol.GetAllAccessibleMembers().ToArray();
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("protFieldB")));
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("intFieldB")));
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("pubFieldB")));

      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("ProtPropertyB")));
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("IntPropertyB")));
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("PubPropertyB")));

      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("ProtMethodB")));
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("IntMethodB")));
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("PubMethodB")));

      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("protFieldA")));
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("intFieldA")));
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("pubFieldA")));

      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("ProtPropertyA")));
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("IntPropertyA")));
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("PubPropertyA")));

      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("ProtMethodA")));
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("IntMethodA")));
      Assert.IsTrue(accessibleMembers.Any(member => member.Name.Equals("PubMethodA")));
    }

    [TestMethod]
    public void GetAllAccessibleMembersDoesNotIncludePrivateMembersOfBaseTypes() {
      var typeSymbol = GetTypeSymbolByClassName(AccessibleMemberTestSource, "C");
      var accessibleMembers = typeSymbol.GetAllAccessibleMembers().ToArray();

      Assert.IsFalse(accessibleMembers.Any(member => member.Name.Equals("defFieldB")));
      Assert.IsFalse(accessibleMembers.Any(member => member.Name.Equals("privFieldB")));

      Assert.IsFalse(accessibleMembers.Any(member => member.Name.Equals("DefPropertyB")));
      Assert.IsFalse(accessibleMembers.Any(member => member.Name.Equals("PrivPropertyB")));

      Assert.IsFalse(accessibleMembers.Any(member => member.Name.Equals("DefMethodB")));
      Assert.IsFalse(accessibleMembers.Any(member => member.Name.Equals("PrivMethodB")));

      Assert.IsFalse(accessibleMembers.Any(member => member.Name.Equals("defFieldA")));
      Assert.IsFalse(accessibleMembers.Any(member => member.Name.Equals("privFieldA")));

      Assert.IsFalse(accessibleMembers.Any(member => member.Name.Equals("DefPropertyA")));
      Assert.IsFalse(accessibleMembers.Any(member => member.Name.Equals("PrivPropertyA")));

      Assert.IsFalse(accessibleMembers.Any(member => member.Name.Equals("DefMethodA")));
      Assert.IsFalse(accessibleMembers.Any(member => member.Name.Equals("PrivMethodA")));
    }

    [TestMethod]
    public void GetAllNonPrivateMembersReturnsAllNonPrivateMembersOfAllBaseTypesAndSelf() {
      var typeSymbol = GetTypeSymbolByClassName(AccessibleMemberTestSource, "C");
      var nonPrivateMembers = typeSymbol.GetAllNonPrivateMembers().ToArray();
      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("protFieldC")));
      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("intFieldC")));
      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("pubFieldC")));

      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("ProtPropertyC")));
      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("IntPropertyC")));
      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("PubPropertyC")));

      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("ProtMethodC")));
      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("IntMethodC")));
      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("PubMethodC")));

      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("protFieldB")));
      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("intFieldB")));
      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("pubFieldB")));

      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("ProtPropertyB")));
      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("IntPropertyB")));
      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("PubPropertyB")));

      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("ProtMethodB")));
      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("IntMethodB")));
      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("PubMethodB")));

      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("protFieldA")));
      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("intFieldA")));
      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("pubFieldA")));

      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("ProtPropertyA")));
      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("IntPropertyA")));
      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("PubPropertyA")));

      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("ProtMethodA")));
      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("IntMethodA")));
      Assert.IsTrue(nonPrivateMembers.Any(member => member.Name.Equals("PubMethodA")));
    }

    [TestMethod]
    public void GetAllNonPrivateMembersDoesNotIncludePrivateMembersOfBaseTypesAndSelf() {
      var typeSymbol = GetTypeSymbolByClassName(AccessibleMemberTestSource, "C");
      var nonPrivateMembers = typeSymbol.GetAllNonPrivateMembers().ToArray();
      Assert.IsFalse(nonPrivateMembers.Any(member => member.Name.Equals("defFieldC")));
      Assert.IsFalse(nonPrivateMembers.Any(member => member.Name.Equals("privFieldC")));

      Assert.IsFalse(nonPrivateMembers.Any(member => member.Name.Equals("DefPropertyC")));
      Assert.IsFalse(nonPrivateMembers.Any(member => member.Name.Equals("PrivPropertyC")));

      Assert.IsFalse(nonPrivateMembers.Any(member => member.Name.Equals("DefMethodC")));
      Assert.IsFalse(nonPrivateMembers.Any(member => member.Name.Equals("PrivMethodC")));

      Assert.IsFalse(nonPrivateMembers.Any(member => member.Name.Equals("defFieldB")));
      Assert.IsFalse(nonPrivateMembers.Any(member => member.Name.Equals("privFieldB")));

      Assert.IsFalse(nonPrivateMembers.Any(member => member.Name.Equals("DefPropertyB")));
      Assert.IsFalse(nonPrivateMembers.Any(member => member.Name.Equals("PrivPropertyB")));

      Assert.IsFalse(nonPrivateMembers.Any(member => member.Name.Equals("DefMethodB")));
      Assert.IsFalse(nonPrivateMembers.Any(member => member.Name.Equals("PrivMethodB")));

      Assert.IsFalse(nonPrivateMembers.Any(member => member.Name.Equals("defFieldA")));
      Assert.IsFalse(nonPrivateMembers.Any(member => member.Name.Equals("privFieldA")));

      Assert.IsFalse(nonPrivateMembers.Any(member => member.Name.Equals("DefPropertyA")));
      Assert.IsFalse(nonPrivateMembers.Any(member => member.Name.Equals("PrivPropertyA")));

      Assert.IsFalse(nonPrivateMembers.Any(member => member.Name.Equals("DefMethodA")));
      Assert.IsFalse(nonPrivateMembers.Any(member => member.Name.Equals("PrivMethodA")));
    }

    [TestMethod]
    public void GetAllPublicMembersReturnsAllPublicMembersOfAllBaseTypesAndSelf() {
      var typeSymbol = GetTypeSymbolByClassName(AccessibleMemberTestSource, "C");
      var publicMembers = typeSymbol.GetAllPublicMembers().ToArray();
      Assert.IsTrue(publicMembers.Any(member => member.Name.Equals("pubFieldC")));
      Assert.IsTrue(publicMembers.Any(member => member.Name.Equals("PubPropertyC")));
      Assert.IsTrue(publicMembers.Any(member => member.Name.Equals("PubMethodC")));

      Assert.IsTrue(publicMembers.Any(member => member.Name.Equals("pubFieldB")));
      Assert.IsTrue(publicMembers.Any(member => member.Name.Equals("PubPropertyB")));
      Assert.IsTrue(publicMembers.Any(member => member.Name.Equals("PubMethodB")));

      Assert.IsTrue(publicMembers.Any(member => member.Name.Equals("pubFieldA")));
      Assert.IsTrue(publicMembers.Any(member => member.Name.Equals("PubPropertyA")));
      Assert.IsTrue(publicMembers.Any(member => member.Name.Equals("PubMethodA")));
    }

    [TestMethod]
    public void GetAllPublicMembersReturnsNoNonPublicMembersOfBaseTypesAndSelf() {
      var typeSymbol = GetTypeSymbolByClassName(AccessibleMemberTestSource, "C");
      var publicMembers = typeSymbol.GetAllPublicMembers().ToArray();
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("defFieldC")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("privFieldC")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("protFieldC")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("intFieldC")));

      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("DefPropertyC")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("PrivPropertyC")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("ProtPropertyC")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("IntPropertyC")));

      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("DefMethodC")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("PrivMethodC")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("ProtMethodC")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("IntMethodC")));

      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("defFieldB")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("privFieldB")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("protFieldB")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("intFieldB")));

      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("DefPropertyB")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("PrivPropertyB")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("ProtPropertyB")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("IntPropertyB")));

      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("DefMethodB")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("PrivMethodB")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("ProtMethodB")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("IntMethodB")));

      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("defFieldA")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("privFieldA")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("protFieldA")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("intFieldA")));

      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("DefPropertyA")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("PrivPropertyA")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("ProtPropertyA")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("IntPropertyA")));

      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("DefMethodA")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("PrivMethodA")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("ProtMethodA")));
      Assert.IsFalse(publicMembers.Any(member => member.Name.Equals("IntMethodA")));
    }

    [TestMethod]
    public void GetAllMembersReturnsAllMembersOfBaseTypesAndSelf() {
      var typeSymbol = GetTypeSymbolByClassName(AccessibleMemberTestSource, "C");
      var allMembers = typeSymbol.GetAllMembers().ToArray();
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("defFieldC")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("privFieldC")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("protFieldC")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("intFieldC")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("pubFieldC")));

      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("DefPropertyC")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("PrivPropertyC")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("ProtPropertyC")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("IntPropertyC")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("PubPropertyC")));

      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("DefMethodC")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("PrivMethodC")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("ProtMethodC")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("IntMethodC")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("PubMethodC")));

      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("defFieldB")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("privFieldB")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("protFieldB")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("intFieldB")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("pubFieldB")));

      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("DefPropertyB")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("PrivPropertyB")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("ProtPropertyB")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("IntPropertyB")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("PubPropertyB")));

      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("DefMethodB")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("PrivMethodB")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("ProtMethodB")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("IntMethodB")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("PubMethodB")));

      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("defFieldA")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("privFieldA")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("protFieldA")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("intFieldA")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("pubFieldA")));

      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("DefPropertyA")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("PrivPropertyA")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("ProtPropertyA")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("IntPropertyA")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("PubPropertyA")));

      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("DefMethodA")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("PrivMethodA")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("ProtMethodA")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("IntMethodA")));
      Assert.IsTrue(allMembers.Any(member => member.Name.Equals("PubMethodA")));
    }
  }
}
