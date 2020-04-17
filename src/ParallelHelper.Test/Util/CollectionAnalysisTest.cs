using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Linq;

namespace ParallelHelper.Test.Util {
  [TestClass]
  public class CollectionAnalysisTest {
    [TestMethod]
    public void GetPotentiallyMutableCollectionFieldsReturnsGenericCollectionInterfaces() {
      const string source = @"
using System.Collections.Generic;

public class Test {
  private ISet<string> set;
  private IDictionary<string, string> dictionary;
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var declaration = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ClassDeclarationSyntax>()
        .Single();
      var analysis = new CollectionAnalysis(semanticModel, default);
      Assert.AreEqual(2, analysis.GetPotentiallyMutableCollectionFields(declaration).Count());
    }

    [TestMethod]
    public void GetPotentiallyMutableCollectionFieldsReturnsGenericCollectionItself() {
      const string source = @"
using System.Collections.Generic;

public class Test {
  private ICollection<string> collection;
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var declaration = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ClassDeclarationSyntax>()
        .Single();
      var analysis = new CollectionAnalysis(semanticModel, default);
      Assert.AreEqual(1, analysis.GetPotentiallyMutableCollectionFields(declaration).Count());
    }

    [TestMethod]
    public void GetPotentiallyMutableCollectionFieldsReturnsCollectionInterfaces() {
      const string source = @"
using System.Collections;

public class Test {
  private IDictionary dictionary;
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var declaration = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ClassDeclarationSyntax>()
        .Single();
      var analysis = new CollectionAnalysis(semanticModel, default);
      Assert.AreEqual(1, analysis.GetPotentiallyMutableCollectionFields(declaration).Count());
    }

    [TestMethod]
    public void GetPotentiallyMutableCollectionFieldsReturnsCollectionItself() {
      const string source = @"
using System.Collections;

public class Test {
  private ICollection collection;
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var declaration = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ClassDeclarationSyntax>()
        .Single();
      var analysis = new CollectionAnalysis(semanticModel, default);
      Assert.AreEqual(1, analysis.GetPotentiallyMutableCollectionFields(declaration).Count());
    }

    [TestMethod]
    public void GetPotentiallyMutableCollectionFieldsDoesNotReturnNonCollectionFields() {
      const string source = @"
using System.Collections;

public class Test {
  private int number;
  private string text;
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var declaration = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ClassDeclarationSyntax>()
        .Single();
      var analysis = new CollectionAnalysis(semanticModel, default);
      Assert.AreEqual(0, analysis.GetPotentiallyMutableCollectionFields(declaration).Count());
    }

    [TestMethod]
    public void GetPotentiallyMutableCollectionFieldsReturnsMutableButNotImmutableCollections() {
      const string source = @"
using System.Collections.Generic;
using System.Collections.Immutable;

public class Test {
  private ISet<string> set;
  private IDictionary<string, string> dictionary;
  private ImmutableList<string> immutableList;
  private ImmutableDictionary<string, string> immutableDictionary;
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var declaration = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<ClassDeclarationSyntax>()
        .Single();
      var analysis = new CollectionAnalysis(semanticModel, default);
      Assert.AreEqual(2, analysis.GetPotentiallyMutableCollectionFields(declaration).Count());
    }

    [TestMethod]
    public void IsPotentiallyMutableCollectionReturnsTrueForGenericSet() {
      const string source = @"
using System.Collections.Generic;
using System.Collections.Immutable;

public class Test {
  private ISet<string> set;
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var field = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<FieldDeclarationSyntax>()
        .SelectMany(f => f.Declaration.Variables)
        .Select(f => (IFieldSymbol)semanticModel.GetDeclaredSymbol(f))
        .Single();
      var analysis = new CollectionAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.IsPotentiallyMutableCollection(field.Type));
    }

    [TestMethod]
    public void IsPotentiallyMutableCollectionReturnsTrueForGenericCollectionItself() {
      const string source = @"
using System.Collections.Generic;
using System.Collections.Immutable;

public class Test {
  private ICollection<string> collection;
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var field = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<FieldDeclarationSyntax>()
        .SelectMany(f => f.Declaration.Variables)
        .Select(f => (IFieldSymbol)semanticModel.GetDeclaredSymbol(f))
        .Single();
      var analysis = new CollectionAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.IsPotentiallyMutableCollection(field.Type));
    }

    [TestMethod]
    public void IsPotentiallyMutableCollectionReturnsFalseForImmutableList() {
      const string source = @"
using System.Collections.Generic;
using System.Collections.Immutable;

public class Test {
  private IImmutableList<string> list;
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var field = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<FieldDeclarationSyntax>()
        .SelectMany(f => f.Declaration.Variables)
        .Select(f => (IFieldSymbol)semanticModel.GetDeclaredSymbol(f))
        .Single();
      var analysis = new CollectionAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsPotentiallyMutableCollection(field.Type));
    }

    [TestMethod]
    public void IsPotentiallyMutableCollectionReturnsFalseForNonCollectionType() {
      const string source = @"
using System.Collections.Generic;
using System.Collections.Immutable;

public class Test {
  private int number;
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var field = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<FieldDeclarationSyntax>()
        .SelectMany(f => f.Declaration.Variables)
        .Select(f => (IFieldSymbol)semanticModel.GetDeclaredSymbol(f))
        .Single();
      var analysis = new CollectionAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsPotentiallyMutableCollection(field.Type));
    }

    [TestMethod]
    public void IsImmutableCollectionReturnsFalseForGenericList() {
      const string source = @"
using System.Collections.Generic;
using System.Collections.Immutable;

public class Test {
  private IList<string> list;
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var field = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<FieldDeclarationSyntax>()
        .SelectMany(f => f.Declaration.Variables)
        .Select(f => (IFieldSymbol)semanticModel.GetDeclaredSymbol(f))
        .Single();
      var analysis = new CollectionAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsImmutableCollection(field.Type));
    }

    [TestMethod]
    public void IsImmutableCollectionReturnsTrueForImmutableList() {
      const string source = @"
using System.Collections.Generic;
using System.Collections.Immutable;

public class Test {
  private IImmutableList<string> list;
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var field = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<FieldDeclarationSyntax>()
        .SelectMany(f => f.Declaration.Variables)
        .Select(f => (IFieldSymbol)semanticModel.GetDeclaredSymbol(f))
        .Single();
      var analysis = new CollectionAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.IsImmutableCollection(field.Type));
    }

    [TestMethod]
    public void IsImmutableCollectionReturnsFalseForNonCollectionType() {
      const string source = @"
using System.Collections.Generic;
using System.Collections.Immutable;

public class Test {
  private int number;
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var field = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<FieldDeclarationSyntax>()
        .SelectMany(f => f.Declaration.Variables)
        .Select(f => (IFieldSymbol)semanticModel.GetDeclaredSymbol(f))
        .Single();
      var analysis = new CollectionAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsImmutableCollection(field.Type));
    }
  }
}
