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
  /// <summary>
  /// Class to setup a <see cref="CSharpCompilation"/> and a <see cref="CompilationWithAnalyzers"/> instance
  /// with common settings. All changes are immutable; thus, intermediate states can be used independently.
  /// </summary>
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

    private CSharpCompilation _compilation = BaseCompilation;
    private ImmutableDictionary<string, string> _analyzerOptions = ImmutableDictionary.Create<string, string>(AnalyzerConfigOptions.KeyComparer);
    private ImmutableArray<DiagnosticAnalyzer> _analyzers = ImmutableArray<DiagnosticAnalyzer>.Empty;

    private TestCompilationBuilder() { }

    /// <summary>
    /// Creates a new builder instance.
    /// </summary>
    /// <returns>The newly created builder instance.</returns>
    public static TestCompilationBuilder Create() {
      return new TestCompilationBuilder();
    }

    private TestCompilationBuilder(TestCompilationBuilder other) {
      _compilation = other._compilation;
      _analyzerOptions = other._analyzerOptions;
      _analyzers = other._analyzers;
    }

    /// <summary>
    /// Adds the given list of sources to the compilation with an automatically generated name.
    /// </summary>
    /// <param name="sourceTexts">The source texts to add.</param>
    /// <returns>A new compilation builder instance that includes the new source texts.</returns>
    public TestCompilationBuilder AddSourceTexts(params string[] sourceTexts) {
      var syntaxTrees = sourceTexts.Select((source, index) => CSharpSyntaxTree.ParseText(
        source, path: string.Format(DefaultSourceFilenamePattern, _compilation.SyntaxTrees.Length + index)
      )).ToArray();
      return new TestCompilationBuilder(this) {
        _compilation = _compilation.AddSyntaxTrees(syntaxTrees),
      };
    }

    /// <summary>
    /// Adds the given list of references to the compilation.
    /// </summary>
    /// <param name="references">The references to add.</param>
    /// <returns>A new compilation builder instance that includes the new references.</returns>
    public TestCompilationBuilder AddReferences(params MetadataReference[] references) {
      return new TestCompilationBuilder(this) {
        _compilation = _compilation.AddReferences(references)
      };
    }

    /// <summary>
    /// Adds the given analyzer option to the compilation.
    /// </summary>
    /// <param name="key">The key of the analyzer option to add.</param>
    /// <param name="value">The value of the analyzer option to add.</param>
    /// <returns>A new compilation builder instance that includes the new analyzer option.</returns>
    public TestCompilationBuilder AddAnalyzerOption(string key, string value) {
      return new TestCompilationBuilder(this) {
        _analyzerOptions = _analyzerOptions.Add(key, value)
      };
    }

    /// <summary>
    /// Replaces the previously configured analyzer options with the provided ones.
    /// </summary>
    /// <param name="analyzerOptions">The new analyzer options.</param>
    /// <returns>A new compilation builder instance that replaced all analyzer options with the given ones.</returns>
    public TestCompilationBuilder WithAnalyzerOptions(ImmutableDictionary<string, string> analyzerOptions) {
      return new TestCompilationBuilder(this) {
        _analyzerOptions = analyzerOptions
      };
    }

    /// <summary>
    /// Adds the given list of analyzers to the compilation.
    /// </summary>
    /// <param name="analyzers">The key of the analyzer option to add.</param>
    /// <returns>A new compilation builder instance that includes the new analyzers.</returns>
    public TestCompilationBuilder AddAnalyzers(params DiagnosticAnalyzer[] analyzers) {
      return new TestCompilationBuilder(this) {
        _analyzers = _analyzers.AddRange(analyzers)
      };
    }

    /// <summary>
    /// Generates a compilation with the configured settings.
    /// </summary>
    /// <returns>The new compilation.</returns>
    public CSharpCompilation Build() {
      return _compilation;
    }

    /// <summary>
    /// Generates a compilation that includes analyzers with the configured settings.
    /// </summary>
    /// <returns>The new compilation.</returns>
    public CompilationWithAnalyzers BuildWithAnalyzers() {
      return _compilation.WithAnalyzers(
        _analyzers,
        new AnalyzerOptions(
          ImmutableArray<AdditionalText>.Empty,
          new TestAnalyzerConfigOptionsProvider(_analyzerOptions)
        )
      );
    }
  }
}
