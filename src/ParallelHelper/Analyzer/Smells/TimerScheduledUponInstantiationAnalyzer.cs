using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Extensions;
using ParallelHelper.Util;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Analyzer.Smells {
  /// <summary>
  /// Analyzer that analyzes sources for timers that are scheduled upon their instantiation.
  /// 
  /// <example>Illustrates a class that instantiates a timer which is immediately scheduled.
  /// <code>
  /// using System.Threading;
  /// 
  /// class Sample {
  ///   public void DoWorkAsync() {
  ///     var timer = new Timer(TimerFired, null, 10, Timeout.Infinite);
  ///   }
  ///   
  ///   private void TimerFired(object arg) {
  ///     // ...
  ///   }
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class TimerScheduledUponInstantiationAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "PH_S008";

    private const string Category = "Concurrency";

    private static readonly LocalizableString Title = "Timer Scheduled upon Instantiation";
    private static readonly LocalizableString MessageFormat = "Instantiating a timer that schedules the callback right with its instantiation may lead to undesired behavior.";
    private static readonly LocalizableString Description = "";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
      DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden,
      isEnabledByDefault: true, description: Description, helpLinkUri: HelpLinkFactory.CreateUri(DiagnosticId)
    );

    private const string TimerType = "System.Threading.Timer";
    private const int DueTimeArgumentPosition = 2;
    private const string TimeoutType = "System.Threading.Timeout";
    private const string InfiniteField = "Infinite";
    private static readonly string[] OneLiterals = { "1", "1L" };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.RegisterSyntaxNodeAction(AnalyzeExpressionStatement, SyntaxKind.ObjectCreationExpression);
    }

    private static void AnalyzeExpressionStatement(SyntaxNodeAnalysisContext context) {
      new Analyzer(context).Analyze();
    }

    private class Analyzer : InternalAnalyzerBase<ObjectCreationExpressionSyntax> {
      public Analyzer(SyntaxNodeAnalysisContext context) : base(new SyntaxNodeAnalysisContextWrapper(context)) { }

      public override void Analyze() {
        if(Root.ArgumentList == null || !IsTimerConstruction()) {
          return;
        }

        var arguments = Root.ArgumentList.Arguments;
        if(arguments.Count < 3 || IsInfinite(arguments[DueTimeArgumentPosition])) {
          return;
        }

        Context.ReportDiagnostic(Diagnostic.Create(Rule, Root.GetLocation()));
      }

      private bool IsTimerConstruction() {
        var typeSymbol = SemanticModel.GetTypeInfo(Root, CancellationToken).Type;
        return typeSymbol != null && SemanticModel.IsEqualType(typeSymbol, TimerType);
      }

      private bool IsInfinite(ArgumentSyntax argument) {
        var expression = argument.Expression;
        if(IsMinusOneConstant(expression)) {
          return true;
        } 

        var symbol = SemanticModel.GetSymbolInfo(argument.Expression, CancellationToken).Symbol;
        return symbol != null && IsTimeoutInfinite(symbol);
      }

      private static bool IsMinusOneConstant(ExpressionSyntax expression) {
        return expression is PrefixUnaryExpressionSyntax prefix
          && prefix.OperatorToken.IsKind(SyntaxKind.MinusToken)
          && IsOneLiteral(prefix.Operand);
      }

      private static bool IsOneLiteral(ExpressionSyntax expression) {
        return expression is LiteralExpressionSyntax literal
          && OneLiterals.Any(l => l.Equals(literal.Token.Text, StringComparison.OrdinalIgnoreCase));
      }

      private bool IsTimeoutInfinite(ISymbol symbol) {
        return symbol is IFieldSymbol field
          && InfiniteField.Equals(field.Name)
          && SemanticModel.IsEqualType(field.ContainingType, TimeoutType);
      }
    }
  }
}