using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.Bugs {
  /// <summary>
  /// Analyzer that analyzes sources for the use of LINQ's To-Operations that cause race-conditions when used concurrently
  /// with update operations on a concurrent collection.
  /// 
  /// <example>Illustrates a class with a method that converts a <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}"/> to
  /// a <see cref="System.Collections.Generic.List{T}"/> using a LINQ extension.
  /// <code>
  /// class Sample {
  ///   private readonly ConcurrentDictionary&lt;int, string&gt; entries = new ConcurrentDictionary&lt;int, string&gt;();
  ///   
  ///   public List&lt;KeyValuePair&lt;int, string&gt;&gt; Entries(Sample other) {
  ///     return entries.ToList();
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class LinqToCollectionOnConcurrentDictionaryAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_B008";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "LINQ To* Operation on ConcurrentDictionary";
    private static readonly LocalizableString MessageFormat = "LINQ's extension methods to convert a ConcurrentDictionary to a generic collection are not thread-safe.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    // TODO add all immutable types
    private static readonly LinqDescriptor[] LinqDescriptors = {
      new LinqDescriptor("System.Linq.Enumerable", "ToArray", "ToList"),
      new LinqDescriptor("System.Collections.Immutable.ImmutableArray", "ToImmutableArray"),
      new LinqDescriptor("System.Collections.Immutable.ImmutableDictionary", "ToImmutableDictionary"),
      new LinqDescriptor("System.Collections.Immutable.ImmutableHashSet", "ToImmutableHashSet"),
      new LinqDescriptor("System.Collections.Immutable.ImmutableList", "ToImmutableList"),
    };

    private static readonly string[] ConcurrentCollectionTypes = {
      "System.Collections.Concurrent.ConcurrentDictionary`2",
    };

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerBase<InvocationExpressionSyntax> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper<InvocationExpressionSyntax>(context)) { }

      public override void Analyze() {
        if(IsAccessingConcurrentCollection() && IsLinqOperation()) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, Root.GetLocation()));
        }
      }

      private bool IsAccessingConcurrentCollection() {
        return Root.Expression is MemberAccessExpressionSyntax memberAccess
          && IsMemberAccessOnConcurrentCollection(memberAccess);
      }

      private bool IsMemberAccessOnConcurrentCollection(MemberAccessExpressionSyntax memberAccess) {
        var type = SemanticModel.GetTypeInfo(memberAccess.Expression, CancellationToken).Type;
        return type != null 
          && ConcurrentCollectionTypes
            .WithCancellation(CancellationToken)
            .Any(collectionType => SemanticModel.IsEqualType(type, collectionType));
      }

      private bool IsLinqOperation() {
        return SemanticModel.GetSymbolInfo(Root, CancellationToken).Symbol is IMethodSymbol method
          && LinqDescriptors
            .WithCancellation(CancellationToken)
            .Any(descriptor => IsLinqDescriptor(method, descriptor));
      }

      private bool IsLinqDescriptor(IMethodSymbol method, LinqDescriptor descriptor) {
        return SemanticModel.IsEqualType(method.ContainingType, descriptor.Type)
          && descriptor.Methods.Any(method.Name.Equals);
      }
    }

    private class LinqDescriptor {
      public string Type { get; }
      public IReadOnlyCollection<string> Methods { get; }

      public LinqDescriptor(string type, params string[] methods) {
        Type = type;
        Methods = methods;
      }
    }
  }
}
