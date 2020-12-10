using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace ParallelHelper.Test {
  internal class CompilationFactory {
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

    private static Compilation CreateCompilation(params string[] sources) {
      return TestCompilationBuilder.Create()
        .AddSourceTexts(sources)
        .Build();
    }
  }
}
