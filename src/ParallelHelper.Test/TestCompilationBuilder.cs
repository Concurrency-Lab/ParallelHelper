using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Test.Analyzer;
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
  public class TestCompilationBuilder {
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
    public ImmutableDictionary<string, string> AnalyzerOptions { get; private set; }
      = ImmutableDictionary.Create<string, string>(AnalyzerConfigOptions.KeyComparer);
    public ImmutableArray<DiagnosticAnalyzer> Analyzers { get; private set; } = ImmutableArray<DiagnosticAnalyzer>.Empty;

    private TestCompilationBuilder() { }

    public static TestCompilationBuilder Create() {
      return new TestCompilationBuilder();
    }

    private TestCompilationBuilder(TestCompilationBuilder other) {
      Compilation = other.Compilation;
      AnalyzerOptions = other.AnalyzerOptions;
      Analyzers = other.Analyzers;
    }

    public TestCompilationBuilder AddSourceTexts(params string[] sourceTexts) {
      var syntaxTrees = sourceTexts.Select((source, index) => CSharpSyntaxTree.ParseText(
        source, path: string.Format(DefaultSourceFilenamePattern, Compilation.SyntaxTrees.Length + index)
      )).ToArray();
      return new TestCompilationBuilder(this) {
        Compilation = Compilation.AddSyntaxTrees(syntaxTrees),
      };
    }

    public TestCompilationBuilder AddReferences(params MetadataReference[] references) {
      return new TestCompilationBuilder(this) {
        Compilation = Compilation.AddReferences(references)
      };
    }

    public TestCompilationBuilder AddAnalyzerOption(string key, string value) {
      return new TestCompilationBuilder(this) {
        AnalyzerOptions = AnalyzerOptions.Add(key, value)
      };
    }

    public TestCompilationBuilder WithAnalyzerOptions(ImmutableDictionary<string, string> analyzerOptions) {
      return new TestCompilationBuilder(this) {
        AnalyzerOptions = analyzerOptions
      };
    }

    public TestCompilationBuilder AddAnalyzers(params DiagnosticAnalyzer[] analyzer) {
      return new TestCompilationBuilder(this) {
        Analyzers = Analyzers.AddRange(analyzer)
      };
    }

    public CSharpCompilation Build() {
      return Compilation;
    }

    public CompilationWithAnalyzers BuildWithAnalyzers() {
      return Compilation.WithAnalyzers(
        Analyzers,
        new AnalyzerOptions(
          ImmutableArray<AdditionalText>.Empty,
          new TestAnalyzerConfigOptionsProvider(AnalyzerOptions)
        )
      );
    }
  }
}
