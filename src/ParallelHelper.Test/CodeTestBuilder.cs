using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelHelper.Test {
  public class CodeTestBuilder {
    private const string DefaultSourceFilenamePattern = "Test_{0}.cs";

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

    private static readonly CSharpCompilation BaseCompilation = CSharpCompilation.Create(
      assemblyName: "test.dll",
      references: References,
      options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );

    public CSharpCompilation Compilation { get; private set; } = BaseCompilation;
    public int SourceCount { get; private set; }
    public ImmutableDictionary<string, string> AnalyzerOptions { get; private set; }
      = ImmutableDictionary.Create<string, string>(AnalyzerConfigOptions.KeyComparer);

    private CodeTestBuilder() { }

    public static CodeTestBuilder Create() {
      return new CodeTestBuilder();
    }

    private CodeTestBuilder(CodeTestBuilder other) {
      Compilation = other.Compilation;
      SourceCount = other.SourceCount;
      AnalyzerOptions = other.AnalyzerOptions;
    }

    public CodeTestBuilder AddSourceTexts(params string[] sourceTexts) {
      var syntaxTrees = sourceTexts.Select((source, index) => CSharpSyntaxTree.ParseText(
        source, path: string.Format(DefaultSourceFilenamePattern, SourceCount + index)
      )).ToArray();
      return new CodeTestBuilder(this) {
        Compilation = Compilation.AddSyntaxTrees(syntaxTrees),
        SourceCount = SourceCount + syntaxTrees.Length
      };
    }

    public CodeTestBuilder WithReferences(params MetadataReference[] references) {
      return new CodeTestBuilder(this) {
        Compilation = Compilation.WithReferences(references)
      };
    }

    public CodeTestBuilder AddAnalyzerOption(string key, string value) {
      return new CodeTestBuilder(this) {
        AnalyzerOptions = AnalyzerOptions.Add(key, value)
      };
    }
  }
}
