using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Extensions;
using System;
using System.Linq;

namespace ParallelHelper.Test.Extensions {
  [TestClass]
  public class MethodDeclarationExtensionsTest {
    [TestMethod]
    public void GetSignatureLocationReturnsTheLocationBeginningFromTheModifiersUntilTheClosingParenthese() {
      const string source = @"
namespace SignatureTest {
  class Test {
    public static void FindMyLocation() {
    }
  }
}
";
      var method = CompilationFactory.GetNodesOfType<MethodDeclarationSyntax>(source).Single();
      var signatureSourceSpan = method.GetSignatureLocation().SourceSpan;
      var start = signatureSourceSpan.Start;
      var length = signatureSourceSpan.End - signatureSourceSpan.Start;
      Assert.AreEqual("public static void FindMyLocation()", source.Substring(start, length));
    }
  }
}
