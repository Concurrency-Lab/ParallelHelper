﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.Smells {
  /// <summary>
  /// Analyzer that analyzes sources for methods that are potentially async (return a task and end with the *Async suffix).
  /// If this method is not async but throws an exception, the exception is on the level of the method invocation rather than
  /// encapsulated in the task as it would be the case for async methods.
  /// 
  /// <example>Illustrates a class with a potentially async method that throws an exception.
  /// <code>
  /// class Sample {
  ///   public Task DoWork(int input) {
  ///     throw new ArgumentException(nameof(input));
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class ThrowsInPotentiallyAsyncMethodAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S032";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Throws in Potentially Async Method";
    private static readonly LocalizableString MessageFormat = "A method that throws an exception that denotes itself as async behaves differently than an async method. Return Task.FromException(...) instead.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    private const string AsyncSuffix = "Async";
    private const string DefaultExcludedBaseTypes = "System.ArgumentException System.NotImplementedException";

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerWithSyntaxWalkerBase<MethodDeclarationSyntax> {
      private readonly TaskAnalysis _taskAnalysis;
      private readonly Stack<ITypeSymbol> _caughtExceptionTypes = new Stack<ITypeSymbol>();
      private readonly ISet<ITypeSymbol> _excludedBaseTypes;

      private bool IsMethodWithAsyncSuffix => Root.Identifier.Text.EndsWith(AsyncSuffix);
      private bool IsAsyncMethod => Root.Modifiers.Any(SyntaxKind.AsyncKeyword);

      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper(context)) {
        _taskAnalysis = new TaskAnalysis(context.SemanticModel, context.CancellationToken);
        _excludedBaseTypes = GetExcludedExceptionBaseTypes();
      }

      public override void Analyze() {
        if(!IsPotentiallyAsyncMethod()) {
          return;
        }
        base.Analyze();
      }

      private ISet<ITypeSymbol> GetExcludedExceptionBaseTypes() {
        return Context.Options.GetConfig(Rule, "exclusions", DefaultExcludedBaseTypes)
          .Split()
          .SelectMany(SemanticModel.GetTypesByName)
          .ToImmutableHashSet();
      }

      private bool IsPotentiallyAsyncMethod() {
        return !IsAsyncMethod
          && IsMethodWithAsyncSuffix
          && ReturnsTaskObject();
      }

      private bool ReturnsTaskObject() {
        var returnType = SemanticModel.GetTypeInfo(Root.ReturnType, CancellationToken).Type;
        return returnType != null && _taskAnalysis.IsTaskType(returnType);
      }

      public override void VisitThrowStatement(ThrowStatementSyntax node) {
        if(IsExceptionToReport(node.Expression)) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation()));
        }
        base.VisitThrowStatement(node);
      }

      public override void VisitThrowExpression(ThrowExpressionSyntax node) {
        if(IsExceptionToReport(node.Expression)) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation()));
        }
        base.VisitThrowExpression(node);
      }

      public override void VisitTryStatement(TryStatementSyntax node) {
        int trackedExceptions = 0;
        foreach(var catchClause in node.Catches) {
          var type = SemanticModel.GetTypeInfo(catchClause.Declaration.Type, CancellationToken).Type;
          if(type != null) {
            _caughtExceptionTypes.Push(type);
            trackedExceptions++;
          }
        }
        base.VisitTryStatement(node);
        for(int i = 0; i < trackedExceptions; i++) {
          _caughtExceptionTypes.Pop();
        }
      }

      private bool IsExceptionToReport(ExpressionSyntax node) {
        return !IsAnySubTypeOf(node, _excludedBaseTypes.Concat(_caughtExceptionTypes).Distinct());
      }

      private bool IsAnySubTypeOf(SyntaxNode? node, IEnumerable<ITypeSymbol> types) {
        if(node == null) {
          return false;
        }
        var nodeType = SemanticModel.GetTypeInfo(node, CancellationToken).Type;
        return nodeType != null
          && types.Any(baseType => baseType.IsBaseTypeOf(nodeType, CancellationToken));
      }
    }
  }
}
