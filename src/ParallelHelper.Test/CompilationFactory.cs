using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelHelper.Test {
  internal class CompilationFactory {
    private static readonly MetadataReference[] References = new[] {
      MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
      MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51").Location),
      MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").Location),

      MetadataReference.CreateFromFile(typeof(Thread).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Parallel).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(ParallelQuery).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Partitioner).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(ImmutableArray).Assembly.Location),

      MetadataReference.CreateFromFile(Assembly.Load("System.Collections, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").Location),
      MetadataReference.CreateFromFile(typeof(File).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(WebClient).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(StreamReader).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Socket).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location)
    };
    private static readonly CSharpCompilationOptions CompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
    private const string TestSourceFilePath = "Test.cs";

    public static Compilation CreateCompilation(string source) {
      var syntaxTree = CSharpSyntaxTree.ParseText(source, default, TestSourceFilePath);
      return CSharpCompilation.Create("Test.dll", new[] { syntaxTree }, References, CompilationOptions);
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
