using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.Bugs {
  /// <summary>
  /// Analyzer that analyzes sources for repetive awaits on the same <see cref="System.Threading.Tasks.ValueTask"/>.
  /// 
  /// <example>Illustrates a class where one method returns a <see cref="System.Threading.Tasks.ValueTask"/> and another
  /// awaits the same task multiple times.
  /// <code>
  /// class Sample {
  ///   public async Taskk&lt;int&gt; DoWorkAsync() {
  ///     var computationTask = ComputeAsync();
  ///     return await computationTask + await computationTask;
  ///   }
  ///   
  ///   private ValueTask&lt;int&gt; ComputeAsync() {
  ///     return new ValueTask&lt;int&gt;(5);
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class MultipleAwaitsOnValueTaskAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_B012";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Multiple Awaits on the same ValueTask";
    private static readonly LocalizableString MessageFormat = "The ValueTask of the variable '{0}' is awaited multiple times. This can be problematic since the underlying object may be recycled.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    private static readonly string[] ValueTaskTypes = {
      "System.Threading.Tasks.ValueTask",
      "System.Threading.Tasks.ValueTask`1"
    };

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration, SyntaxKind.AnonymousMethodExpression,
        SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : SyntaxNodeAnalyzerBase<SyntaxNode> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(context) { }

      public override void Analyze() {
        if(!Node.IsMethodOrFunctionWithAsyncModifier()) {
          return;
        }
        foreach(var (variable, expression) in GetAllAwaitExpressionsAwaitingCommonVariables()) {
          var location = expression.GetLocation();
          var variableName = variable.Name;
          Context.ReportDiagnostic(Diagnostic.Create(Rule, location, variableName));
        }
      }

      private IEnumerable<(ILocalSymbol Variable, AwaitExpressionSyntax Expression)> GetAllAwaitExpressionsAwaitingCommonVariables() {
        var valueTaskTypedVariables = GetAllValueTypedTaskDeclarations().ToArray();
        var writeAccessCountPerVariable = GetWriteAccessCountPerVariable();
        var variableToAwaitExpressionLookup = GetAwaitedVariableToAwaitExpressionLookup();
        return valueTaskTypedVariables
          .Where(declaration => !HasMoreThanOneWriteAccess(
            declaration.Symbol, declaration.Declarator.Initializer != null, writeAccessCountPerVariable
          ))
          .Select(declaration => declaration.Symbol)
          .Where(variable => variableToAwaitExpressionLookup[variable].Count() > 1)
          .SelectMany(variable => variableToAwaitExpressionLookup[variable].Select(expression => (variable, expression)));
      }

#pragma warning disable CS8619 // The null-check is made within the LINQ expression.
      private IEnumerable<(VariableDeclaratorSyntax Declarator, ILocalSymbol Symbol)> GetAllValueTypedTaskDeclarations() {
        return Node.DescendantNodesInSameActivationFrame()
          .WithCancellation(CancellationToken)
          .OfType<VariableDeclaratorSyntax>()
          .Select(declarator => (
            Declarator: declarator,
            Symbol: SemanticModel.GetDeclaredSymbol(declarator, CancellationToken) as ILocalSymbol
          ))
          .Where(declarationWithSymbol => declarationWithSymbol.Symbol != null)
          .Where(declarationWithSymbol => IsValueTaskTypedVariable(declarationWithSymbol.Symbol!));
      }
#pragma warning restore CS8619

      private bool IsValueTaskTypedVariable(ILocalSymbol symbol) {
        return symbol.Type != null
          && ValueTaskTypes.Any(valueTaskType => SemanticModel.IsEqualType(symbol.Type, valueTaskType));
      }

#pragma warning disable CS8619 // The null-check is made within the LINQ expression.
      private ILookup<ILocalSymbol, AwaitExpressionSyntax> GetAwaitedVariableToAwaitExpressionLookup() {
        return Node.DescendantNodes()
          .WithCancellation(CancellationToken)
          .OfType<AwaitExpressionSyntax>()
          .Select(expression => (Expression: expression, AwaitedVariable: TryGetAwaitedVariable(expression)))
          .Where(expressionWithAwaitedSymbol => expressionWithAwaitedSymbol.AwaitedVariable != null)
          .ToLookup(
            expressionWithAwaitedSymbol => expressionWithAwaitedSymbol.AwaitedVariable,
            expressionWithAwaitedSymbol => expressionWithAwaitedSymbol.Expression
          );
      }
#pragma warning restore CS8619

      private ILocalSymbol? TryGetAwaitedVariable(AwaitExpressionSyntax awaitExpression) {
        if(awaitExpression.Expression == null) {
          return null;
        }
        return SemanticModel.GetSymbolInfo(awaitExpression.Expression, CancellationToken).Symbol as ILocalSymbol;
      }

      private IDictionary<ILocalSymbol, int> GetWriteAccessCountPerVariable() {
        return Node.GetAllWrittenExpressions(CancellationToken)
          .Select(node => SemanticModel.GetSymbolInfo(node, CancellationToken).Symbol as ILocalSymbol)
          .IsNotNull()
          .GroupBy(variable => variable)
          .ToDictionary(
            variableGrouping => variableGrouping.Key,
            variableGrouping => variableGrouping.Count()
          );
      }

      private static bool HasMoreThanOneWriteAccess(
        ILocalSymbol variable,
        bool initializedOnDeclaration,
        IDictionary<ILocalSymbol, int> writeAccessCountPerVariable
      ) {
        if(writeAccessCountPerVariable.TryGetValue(variable, out var writeAccesses)) {
          int writeAccessOffset = initializedOnDeclaration ? 1 : 0;
          return writeAccesses + writeAccessOffset > 1;
        }
        return false;
      }
    }
  }
}
