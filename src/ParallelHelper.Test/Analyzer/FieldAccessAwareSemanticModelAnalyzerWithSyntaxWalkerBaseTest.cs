using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer;
using ParallelHelper.Extensions;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Test.Analyzer {
  [TestClass]
  public class FieldAccessAwareSemanticModelAnalyzerWithSyntaxWalkerBaseTest {
    private class TestAnalyzer : FieldAccessAwareSemanticModelAnalyzerWithSyntaxWalkerBase {
      public static TestAnalyzer Create(string source, params string[] fieldsToTrack) {
        var semanticModel = CompilationFactory.GetSemanticModel(source);
        var context = new SemanticModelAnalysisContext(
          semanticModel,
          new AnalyzerOptions(ImmutableArray.Create<AdditionalText>()),
          diagnostic => { },
          diagnostic => false,
          default
        );
        var fieldSymbolsToTrack = GetFieldsToTrack(semanticModel, new HashSet<string>(fieldsToTrack));
        return new TestAnalyzer(context, fieldSymbolsToTrack);
      }

      private static ISet<IFieldSymbol> GetFieldsToTrack(SemanticModel semanticModel, ISet<string> fieldsToTrack) {
        return semanticModel.SyntaxTree.GetRoot()
          .DescendantNodes()
          .OfType<FieldDeclarationSyntax>()
          .SelectMany(declaration => declaration.Declaration.Variables)
          .Select(variable => semanticModel.GetDeclaredSymbol(variable))
          .Cast<IFieldSymbol>()
          .Where(field => fieldsToTrack.Contains(field.Name))
          .ToImmutableHashSet();
      }

      public TestAnalyzer(SemanticModelAnalysisContext context, ISet<IFieldSymbol> fieldsToTrack) : base(context, fieldsToTrack) { }
    }

    [TestMethod]
    public void TracksFieldReadAccessesInExpressionOfPublicMethod() {
      const string source = @"
public class Test {
  private int a, b;

  public int GetValue() {
    return a + a;
  }
}";
      var analyzer = TestAnalyzer.Create(source, "a", "b");
      analyzer.Analyze();
      Assert.AreEqual(2, analyzer.FieldAccesses.Count);
      Assert.IsTrue(analyzer.FieldAccesses.All(access => !access.IsWriting));
    }

    [TestMethod]
    public void TracksAddAssignmentAsReadWriteInPublicMethod() {
      const string source = @"
public class Test {
  private int a, b;

  public void Add(int amount) {
    b += amount;
  }
}";
      var analyzer = TestAnalyzer.Create(source, "a", "b");
      analyzer.Analyze();
      Assert.AreEqual(2, analyzer.FieldAccesses.Count);
      Assert.IsTrue(analyzer.FieldAccesses.Any(access => access.IsWriting));
      Assert.IsTrue(analyzer.FieldAccesses.Any(access => !access.IsWriting));
    }

    [TestMethod]
    public void OnlyTracksLeftHandSideAsWritingInAssignment() {
      const string source = @"
public class Test {
  private int a, b;

  public void Add(int amount) {
    b += a;
  }
}";
      var analyzer = TestAnalyzer.Create(source, "a");
      analyzer.Analyze();
      Assert.AreEqual(1, analyzer.FieldAccesses.Count);
      Assert.IsTrue(analyzer.FieldAccesses.All(access => !access.IsWriting));
    }

    [TestMethod]
    public void TracksSimpleAssignmentAsWriteOnly() {
      const string source = @"
public class Test {
  private int a, b;

  public void Reset() {
    b = a;
  }
}";
      var analyzer = TestAnalyzer.Create(source, "b");
      analyzer.Analyze();
      Assert.AreEqual(1, analyzer.FieldAccesses.Count);
      Assert.IsTrue(analyzer.FieldAccesses.All(access => access.IsWriting));
    }

    [TestMethod]
    public void TracksPrefixIncrementAsReadWrite() {
      const string source = @"
public class Test {
  private int a, b;

  public void Increment() {
    ++a;
  }
}";
      var analyzer = TestAnalyzer.Create(source, "a", "b");
      analyzer.Analyze();
      Assert.AreEqual(2, analyzer.FieldAccesses.Count);
      Assert.IsTrue(analyzer.FieldAccesses.Any(access => access.IsWriting));
      Assert.IsTrue(analyzer.FieldAccesses.Any(access => !access.IsWriting));
    }

    [TestMethod]
    public void TracksPostfixDecrementAsReadWrite() {
      const string source = @"
public class Test {
  private int a, b;

  public void Increment() {
    b--;
  }
}";
      var analyzer = TestAnalyzer.Create(source, "a", "b");
      analyzer.Analyze();
      Assert.AreEqual(2, analyzer.FieldAccesses.Count);
      Assert.IsTrue(analyzer.FieldAccesses.Any(access => access.IsWriting));
      Assert.IsTrue(analyzer.FieldAccesses.Any(access => !access.IsWriting));
    }

    [TestMethod]
    public void TracksMemberAccessAsRead() {
      const string source = @"
public class Test {
  private object a, b;

  public void Increment() {
    a.Equals(null);
  }
}";
      var analyzer = TestAnalyzer.Create(source, "a", "b");
      analyzer.Analyze();
      Assert.AreEqual(1, analyzer.FieldAccesses.Count);
      Assert.IsTrue(analyzer.FieldAccesses.Any(access => !access.IsWriting));
    }

    [TestMethod]
    public void TracksReturnAsRead() {
      const string source = @"
public class Test {
  private int a, b;

  public int GetValue() {
    return a;
  }
}";
      var analyzer = TestAnalyzer.Create(source, "a", "b");
      analyzer.Analyze();
      Assert.AreEqual(1, analyzer.FieldAccesses.Count);
      Assert.IsTrue(analyzer.FieldAccesses.Any(access => !access.IsWriting));
    }

    [TestMethod]
    public void TracksConditionOfConditionalExpressionAsRead() {
      const string source = @"
public class Test {
  private bool a, b;

  public void GetValue() {
    if(a ? 1 : 2) {
    }
  }
}";
      var analyzer = TestAnalyzer.Create(source, "a", "b");
      analyzer.Analyze();
      Assert.AreEqual(1, analyzer.FieldAccesses.Count);
      Assert.IsTrue(analyzer.FieldAccesses.Any(access => !access.IsWriting));
    }

    [TestMethod]
    public void TracksConditionalAccessAsRead() {
      const string source = @"
public class Test {
  private object a, b;

  public void GetValue() {
    a?.Equals(null);
  }
}";
      var analyzer = TestAnalyzer.Create(source, "a", "b");
      analyzer.Analyze();
      Assert.AreEqual(1, analyzer.FieldAccesses.Count);
      Assert.IsTrue(analyzer.FieldAccesses.Any(access => !access.IsWriting));
    }

    [TestMethod]
    public void TracksConditionOfIfStatementAsRead() {
      const string source = @"
public class Test {
  private bool a, b;

  public void GetValue() {
    if(a) {
    }
  }
}";
      var analyzer = TestAnalyzer.Create(source, "a", "b");
      analyzer.Analyze();
      Assert.AreEqual(1, analyzer.FieldAccesses.Count);
      Assert.IsTrue(analyzer.FieldAccesses.Any(access => !access.IsWriting));
    }

    [TestMethod]
    public void TracksConditionOfWhileStatementAsRead() {
      const string source = @"
public class Test {
  private bool a, b;

  public void GetValue() {
    while(a) {
    }
  }
}";
      var analyzer = TestAnalyzer.Create(source, "a", "b");
      analyzer.Analyze();
      Assert.AreEqual(1, analyzer.FieldAccesses.Count);
      Assert.IsTrue(analyzer.FieldAccesses.Any(access => !access.IsWriting));
    }

    [TestMethod]
    public void TracksConditionOfDoStatementAsRead() {
      const string source = @"
public class Test {
  private bool a, b;

  public void GetValue() {
    do {
    } while(a);
  }
}";
      var analyzer = TestAnalyzer.Create(source, "a", "b");
      analyzer.Analyze();
      Assert.AreEqual(1, analyzer.FieldAccesses.Count);
      Assert.IsTrue(analyzer.FieldAccesses.Any(access => !access.IsWriting));
    }

    [TestMethod]
    public void TracksSwitchExpressionAsRead() {
      const string source = @"
public class Test {
  private bool a, b;

  public void GetValue() {
    switch(b) {
    }
  }
}";
      var analyzer = TestAnalyzer.Create(source, "a", "b");
      analyzer.Analyze();
      Assert.AreEqual(1, analyzer.FieldAccesses.Count);
      Assert.IsTrue(analyzer.FieldAccesses.Any(access => !access.IsWriting));
    }

    [TestMethod]
    public void TracksCastAsRead() {
      const string source = @"
public class Test {
  private bool a, b;

  public void GetValue() {
    var x = (object)a;
  }
}";
      var analyzer = TestAnalyzer.Create(source, "a", "b");
      analyzer.Analyze();
      Assert.AreEqual(1, analyzer.FieldAccesses.Count);
      Assert.IsTrue(analyzer.FieldAccesses.Any(access => !access.IsWriting));
    }

    [TestMethod]
    public void TracksAnonymousObjectMemberValueAsRead() {
      const string source = @"
public class Test {
  private bool a, b;

  public void GetValue() {
    var x = new { A = a };
  }
}";
      var analyzer = TestAnalyzer.Create(source, "a", "b");
      analyzer.Analyze();
      Assert.AreEqual(1, analyzer.FieldAccesses.Count);
      Assert.IsTrue(analyzer.FieldAccesses.Any(access => !access.IsWriting));
    }

    [TestMethod]
    public void TracksStringInterpolationAsRead() {
      const string source = @"
public class Test {
  private bool a, b;

  public void GetValue() {
    var text = $""a b c {a}"";
  }
}";
      var analyzer = TestAnalyzer.Create(source, "a", "b");
      analyzer.Analyze();
      Assert.AreEqual(1, analyzer.FieldAccesses.Count);
      Assert.IsTrue(analyzer.FieldAccesses.Any(access => !access.IsWriting));
    }

    [TestMethod]
    public void TracksSimpleArgumentAsReadOnly() {
      const string source = @"
public class Test {
  private int a, b;

  public void DoIt() {
    CallMe(a);
  }

  public void CallMe(int x) {}
}";
      var analyzer = TestAnalyzer.Create(source, "a", "b");
      analyzer.Analyze();
      Assert.AreEqual(1, analyzer.FieldAccesses.Count);
      Assert.IsTrue(analyzer.FieldAccesses.Any(access => !access.IsWriting));
    }

    [TestMethod]
    public void TracksOutArgumentAsWriteOnly() {
      const string source = @"
public class Test {
  private int a, b;

  public void DoIt() {
    CallMe(out a);
  }

  public void CallMe(out int x) { x = 0; }
}";
      var analyzer = TestAnalyzer.Create(source, "a", "b");
      analyzer.Analyze();
      Assert.AreEqual(1, analyzer.FieldAccesses.Count);
      Assert.IsTrue(analyzer.FieldAccesses.Any(access => access.IsWriting));
    }

    [TestMethod]
    public void TracksRefArgumentAsReadWrite() {
      const string source = @"
public class Test {
  private int a, b;

  public void DoIt() {
    CallMe(ref a);
  }

  public void CallMe(ref int x) { }
}";
      var analyzer = TestAnalyzer.Create(source, "a", "b");
      analyzer.Analyze();
      Assert.AreEqual(2, analyzer.FieldAccesses.Count);
      Assert.IsTrue(analyzer.FieldAccesses.Any(access => access.IsWriting));
      Assert.IsTrue(analyzer.FieldAccesses.Any(access => !access.IsWriting));
    }

    [TestMethod]
    public void TracksWhenClauseOfExceptionAsRead() {
      const string source = @"
public class Test {
  private bool a, b;

  public void DoIt() {
    var x = true;
    switch(x) {
    case true when(a):
      break;
    }
  }
}";
      var analyzer = TestAnalyzer.Create(source, "a", "b");
      analyzer.Analyze();
      Assert.AreEqual(1, analyzer.FieldAccesses.Count);
      Assert.IsTrue(analyzer.FieldAccesses.Any(access => !access.IsWriting));
    }

    [TestMethod]
    public void TracksEnclosingMethodAsEnclosingScope() {
      const string source = @"
public class Test {
  private int a, b;

  public void Reset() {
    b = a;
  }
}";
      var analyzer = TestAnalyzer.Create(source, "b");
      analyzer.Analyze();
      var fieldAccess = analyzer.FieldAccesses.Single();

      Assert.IsInstanceOfType(fieldAccess.EnclosingScope, typeof(MethodDeclarationSyntax));
      var method = (MethodDeclarationSyntax)fieldAccess.EnclosingScope;
      Assert.AreEqual("Reset", method.Identifier.Text);
    }

    [TestMethod]
    public void TracksEnclosingPropertyAccessorAsEnclosingScope() {
      const string source = @"
public class Test {
  private int a, b;

  public int A {
    get {
      return a;
    }
  }
}";
      var analyzer = TestAnalyzer.Create(source, "a", "b");
      analyzer.Analyze();
      var fieldAccess = analyzer.FieldAccesses.Single();
      Assert.IsInstanceOfType(fieldAccess.EnclosingScope, typeof(AccessorDeclarationSyntax));
    }

    [TestMethod]
    public void TracksEnclosingLockStatement() {
      const string source = @"
public class Test {
  private readonly object syncObject = new object();
  private int a, b;

  public void Reset() {
    lock(syncObject) {
      b = a;
    }
  }
}";
      var analyzer = TestAnalyzer.Create(source, "a", "b");
      analyzer.Analyze();
      Assert.AreEqual(2, analyzer.FieldAccesses.Count);
      Assert.IsTrue(analyzer.FieldAccesses.All(access => access.IsInsideLock));
    }

    [TestMethod]
    public void InnerMostLockStatementIsEnclosingLock() {
      const string source = @"
public class Test {
  private readonly object syncObject1 = new object();
  private readonly object syncObject2 = new object();
  private int a, b;

  public void Reset() {
    lock(syncObject1) {
      lock(syncObject2) {
        b = a;
      }
    }
  }
}";
      var analyzer = TestAnalyzer.Create(source, "a", "b");
      analyzer.Analyze();
      Assert.AreEqual(2, analyzer.FieldAccesses.Count);
      var syncObjectNames = analyzer.FieldAccesses
        .Select(access => access.EnclosingLock?.Expression)
        .IsNotNull()
        .Cast<IdentifierNameSyntax>()
        .Select(identifier => identifier.Identifier.Text);
      foreach(var syncObjectName in syncObjectNames) {
        Assert.AreEqual("syncObject2", syncObjectName);
      }
    }

    [TestMethod]
    public void DoesNotTrackAccessesInsideParenthizedLambda() {
      const string source = @"
public class Test {
  private int a, b;

  public void Reset() {
    Action action = () => {
      b = a;
    },
  }
}";
      var analyzer = TestAnalyzer.Create(source, "a", "b");
      analyzer.Analyze();
      Assert.AreEqual(0, analyzer.FieldAccesses.Count);
    }

    [TestMethod]
    public void DoesNotTrackAccessesInsideSimpleLambda() {
      const string source = @"
public class Test {
  private int a, b;

  public void Reset() {
    Func<int> action = () => a + b;
  }
}";
      var analyzer = TestAnalyzer.Create(source, "a", "b");
      analyzer.Analyze();
      Assert.AreEqual(0, analyzer.FieldAccesses.Count);
    }

    [TestMethod]
    public void DoesNotTrackAccessesInsideDelegate() {
      const string source = @"
public class Test {
  private int a, b;

  public void Reset() {
    Action action = delegate {
      a = b;
    };
  }
}";
      var analyzer = TestAnalyzer.Create(source, "a", "b");
      analyzer.Analyze();
      Assert.AreEqual(0, analyzer.FieldAccesses.Count);
    }
  }
}
