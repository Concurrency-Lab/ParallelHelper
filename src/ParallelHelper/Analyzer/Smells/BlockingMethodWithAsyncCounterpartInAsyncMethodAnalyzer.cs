﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.Smells {
  /// <summary>
  /// Analyzer that analyzes sources for the use of blocking methods that do have async counterparts inside async methods.
  /// 
  /// <example>Illustrates a class with an async method that uses a blocking method that does have an async counterpart.
  /// <code>
  /// class Sample {
  ///   public async Task DoWorkAsync() {
  ///     using(var client = new TcpClient())
  ///     using(var reader = new StreamReader(client.GetStream()) {
  ///       var buffer = new char[1024];
  ///       reader.Read(buffer, 0, buffer.Length);
  ///     }
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class BlockingMethodWithAsyncCounterpartInAsyncMethodAnalyzer : DiagnosticAnalyzer {
    // TODO include methods that end with Async? Since methods could be implemented synchronously
    //      because the async counterpart is not known.
    public const string DiagnosticId = "PH_S019";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Blocking Method in Async Method";
    private static readonly LocalizableString MessageFormat = "The blocking method '{0}' is used inside an async method, although it appears to have an async counterpart '{1}'.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    private const string AsyncSuffix = "Async";
    private const string MatchOption = "match";
    private const string DefaultExcludedMethods = @"Microsoft.EntityFrameworkCore.DbContext:Add,AddRange
Microsoft.EntityFrameworkCore.DbSet`1:Add,AddRange";

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeAsyncCandidate, SyntaxKind.MethodDeclaration, SyntaxKind.AnonymousMethodExpression,
        SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression);
    }

    private static void AnalyzeAsyncCandidate(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerBase<SyntaxNode> {
      private readonly TaskAnalysis _taskAnalysis;

      private bool IsAsyncMethod => Root is MethodDeclarationSyntax method
        && method.Modifiers.Any(SyntaxKind.AsyncKeyword);
      private bool IsAsyncAnonymousFunction => Root is AnonymousFunctionExpressionSyntax function
        && function.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);

      private bool IsMatchReturnTypeEnabled => Context.Options.GetConfig(Rule, "returnType", MatchOption) == MatchOption;
      private int ParameterTypeMatchCount => int.TryParse(Context.Options.GetConfig(Rule, "parameterTypeMatchCount", "1"), out var count) ? count : 0;

      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper(context)) {
        _taskAnalysis = new TaskAnalysis(context.SemanticModel, context.CancellationToken);
      }

      public override void Analyze() {
        if(!IsAsyncMethod && !IsAsyncAnonymousFunction) {
          return;
        }
        var excludedMethods = GetExcludedMethods().ToArray();
        foreach(var invocation in GetAllInvocations()) {
          AnalyzeInvocation(invocation, excludedMethods);
        }
      }

      private IEnumerable<ClassMemberDescriptor> GetExcludedMethods() {
        return Context.Options.GetConfigAsMemberDescriptors(Rule, "exclusions", DefaultExcludedMethods);
      }

      private IEnumerable<InvocationExpressionSyntax> GetAllInvocations() {
        return Root.DescendantNodesInSameActivationFrame()
          .WithCancellation(CancellationToken)
          .OfType<InvocationExpressionSyntax>();
      }

      private void AnalyzeInvocation(InvocationExpressionSyntax invocation, IReadOnlyCollection<ClassMemberDescriptor> excludedMethods) {
        if(SemanticModel.GetSymbolInfo(invocation, CancellationToken).Symbol is IMethodSymbol method
            && !IsPotentiallyAsyncMethod(method) && TryGetAsyncCounterpartName(method, out var asyncName)
            && !IsExcludedMethod(method, excludedMethods)) {
          Context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), method.Name, asyncName));
        }
      }

      private bool IsExcludedMethod(IMethodSymbol method, IReadOnlyCollection<ClassMemberDescriptor> excludedMethods) {
        return excludedMethods.AnyContainsMember(SemanticModel, method);
      }

      private bool IsPotentiallyAsyncMethod(IMethodSymbol method) {
        return method.IsAsync || method.Name.EndsWith(AsyncSuffix) || _taskAnalysis.IsTaskType(method.ReturnType);
      }

      private bool TryGetAsyncCounterpartName(IMethodSymbol method, out string asyncName) {
        var candidateName = method.Name + AsyncSuffix;
        if(HasMethodWithNameAndAsyncReturnType(method, candidateName)) {
          asyncName = candidateName;
          return true;
        }
        asyncName = "";
        return false;
      }

      private bool HasMethodWithNameAndAsyncReturnType(IMethodSymbol method, string candidateName) {
        bool matchReturnType = IsMatchReturnTypeEnabled;
        int parameterTypeMatchCount = ParameterTypeMatchCount;
        return method.ContainingType.GetAllPublicMembers()
          .WithCancellation(CancellationToken)
          .OfType<IMethodSymbol>()
          .Where(candidate => candidate.Name == candidateName)
          .Where(candidate => !matchReturnType || IsMethodWithCompatibleAwaitableReturnType(method, candidate))
          .Any(candidate => MatchesTheGivenNumberOfParameterTypes(method, candidate, parameterTypeMatchCount));
      }

      private bool IsMethodWithCompatibleAwaitableReturnType(IMethodSymbol method, IMethodSymbol candidate) {
        if(!_taskAnalysis.IsTaskType(candidate.ReturnType)) {
          return false;
        }
        if(_taskAnalysis.IsTaskTypeWithoutResult(candidate.ReturnType)) {
          return method.ReturnType.SpecialType == SpecialType.System_Void;
        }
        var candidateReturnType = ((INamedTypeSymbol)candidate.ReturnType).TypeArguments.Single();
        return SymbolEqualityComparer.Default.Equals(candidateReturnType, method.ReturnType)
          || (candidateReturnType is ITypeParameterSymbol && method.ConstructedFrom.ReturnType is ITypeParameterSymbol);
      }

      private bool MatchesTheGivenNumberOfParameterTypes(IMethodSymbol method, IMethodSymbol candidate, int parameterTypeMatchCount) {
        if(parameterTypeMatchCount == 0) {
          return true;
        }
        int parametersToMatch = Math.Min(parameterTypeMatchCount, method.Parameters.Length);
        if(candidate.Parameters.Length < parametersToMatch) {
          return false;
        }
        return method.Parameters
          .WithCancellation(CancellationToken)
          .Zip(candidate.Parameters, (methodParameter, candidateParameter) => methodParameter.Type.Equals(candidateParameter.Type, SymbolEqualityComparer.Default))
          .Take(parametersToMatch)
          .All(equalType => equalType);
      }
    }
  }
}
