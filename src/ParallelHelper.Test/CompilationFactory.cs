using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace ParallelHelper.Test {
  internal class CompilationFactory {
    public static Compilation CreateCompilation(params string[] sources) {
      //var syntaxTrees = sources.Select(source => CSharpSyntaxTree.ParseText(source, default, TestSourceFilePath));
      //return CSharpCompilation.Create("Test.dll", syntaxTrees, References, CompilationOptions);
      return CodeTestBuilder.Create()
        .AddSourceTexts(sources)
        .Compilation;
    }

    public static IEnumerable<TSyntaxNode> GetNodesOfType<TSyntaxNode>(string source) where TSyntaxNode : SyntaxNode {
      return CreateCompilation(source)
        .SyntaxTrees
        .Single()
        .GetRoot()
        .DescendantNodes()
        .OfType<TSyntaxNode>();
    }

    public static SemanticModel GetSemanticModel(string source) {
      var compilation = CreateCompilation(source);
      var syntaxTree = compilation.SyntaxTrees.Single();
      return compilation.GetSemanticModel(syntaxTree);
    }
  }
}
