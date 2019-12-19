using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ParallelHelper.Extensions {
  /// <summary>
  /// Extension methods for the use with <see cref="MethodDeclarationSyntax" /> nodes.
  /// </summary>
  public static class MethodDeclarationExtensions {
    /// <summary>
    /// Gets the location that only incorporates the signature of the method.
    /// </summary>
    /// <param name="methodDeclaration">The method declaration to get th method signature location of.</param>
    /// <returns>The location of the method signature.</returns>
    public static Location GetSignatureLocation(this MethodDeclarationSyntax methodDeclaration) {
      var start = methodDeclaration.GetLocation().SourceSpan.Start;
      var end = methodDeclaration.ParameterList.CloseParenToken.Span.End;
      return Location.Create(methodDeclaration.SyntaxTree, TextSpan.FromBounds(start, end));
    }
  }
}
