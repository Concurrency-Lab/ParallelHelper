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

namespace ParallelHelper.Analyzer.BestPractices {
  /// <summary>
  /// Analyzer that analyzes sources for the use of blocking results from async methods/tasks without enclosing them with a gate-keeper.
  /// 
  /// <example>Illustrates a class that uses a blocking <see cref="System.Threading.Tasks.Task.Wait"/> on an async method without enclosing it with a 
  /// <see cref="System.Threading.Tasks.Task.Run(System.Action)"/> invocation to avoid blocking the potential synchronization context.
  /// <code>
  /// class Sample {
  ///   private object syncObject = new object();
  ///   
  ///   public void DoWork() {
  ///     DoWorkAsync().Wait();
  ///   }
  ///   
  ///   public async Task DoWorkAsync() {
  ///   
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class MissingAsyncGateKeeperAnalyzer : DiagnosticAnalyzer {
    // TODO maybe limit this to only analyze code that potentially has a synchronization context.
    public const string DiagnosticId = "PH_P005";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Missing Gate-Keeper";
    private static readonly LocalizableString MessageFormat = "The blocking access '{0}' lacks a gate-keeper. It is recommended to enclose blocking task operations with a Task.Run to prevent the capturing of the synchronization context.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    private const string AsyncSuffix = "Async";

    private static readonly BlockingMemberDescriptor[] BlockingMethods = {
      new BlockingMemberDescriptor("System.Threading.Tasks.Task", new string[] { "Wait" }),
    };


    private static readonly BlockingMemberDescriptor[] BlockingProperties = {
      new BlockingMemberDescriptor("System.Threading.Tasks.Task`1", new string[] { "Result" })
    };

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
      context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context) {
      new Analyzer<IMethodSymbol, InvocationExpressionSyntax>(
        context, BlockingMethods, 
        node => (node.Expression as MemberAccessExpressionSyntax)?.Expression
      ).Analyze();
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context) {
      new Analyzer<IPropertySymbol, MemberAccessExpressionSyntax>(context, BlockingProperties, node => node.Expression).Analyze();
    }

    private class Analyzer<TMemberSymbol, TExpression> : InternalAnalyzerBase<TExpression> where TMemberSymbol : ISymbol where TExpression : ExpressionSyntax {
      private readonly Func<TExpression, ExpressionSyntax?> _getInstanceExpression;
      private readonly IReadOnlyList<BlockingMemberDescriptor> _blockDescriptors;

      public Analyzer(SyntaxNodeAnalysisContext context, IReadOnlyList<BlockingMemberDescriptor> blockDescriptors, Func<TExpression, ExpressionSyntax?> getInstanceExpression)
          : base(new SyntaxNodeAnalysisContextWrapper(context)) {
        _blockDescriptors = blockDescriptors;
        _getInstanceExpression = getInstanceExpression;
      }

      public override void Analyze() {
        if(SemanticModel.GetSymbolInfo(Root, CancellationToken).Symbol is TMemberSymbol member && IsBlockingMemberAccessOnAsyncMethod(member)) {
          var access = $"{member.ContainingType.Name}.{member.Name}";
          Context.ReportDiagnostic(Diagnostic.Create(Rule, Root.GetLocation(), access));
        }
      }

      private bool IsBlockingMemberAccessOnAsyncMethod(TMemberSymbol member) {
        var instanceExpression = _getInstanceExpression(Root);
        return instanceExpression != null
          && IsPotentiallyAsyncMethod(instanceExpression)
          && IsBlockingMemberAccess(member);
      }

      private bool IsPotentiallyAsyncMethod(ExpressionSyntax expression) {
        return SemanticModel.GetSymbolInfo(expression, CancellationToken).Symbol is IMethodSymbol method
          && (method.IsAsync || method.Name.EndsWith(AsyncSuffix));
      }

      private bool IsBlockingMemberAccess(TMemberSymbol member) {
        return _blockDescriptors
          .WithCancellation(CancellationToken)
          .Any(descriptor => IsMemberOfDescriptor(member, descriptor));
      }

      private bool IsMemberOfDescriptor(TMemberSymbol member, BlockingMemberDescriptor descriptor) {
        return SemanticModel.IsEqualType(member.ContainingType, descriptor.Type)
          && descriptor.Member.Any(member.Name.Equals);
      }
    }

    private class BlockingMemberDescriptor {
      public string Type { get; }
      public IReadOnlyList<string> Member { get; }

      public BlockingMemberDescriptor(string type, IReadOnlyList<string> member) {
        Type = type;
        Member = member;
      }
    }
  }
}
