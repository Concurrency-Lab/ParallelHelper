using System.Collections.Generic;

namespace ParallelHelper.Analyzer {
  /// <summary>
  /// Data class to represent a type with a list of methods by their names.
  /// </summary>
  public class MethodDescriptor {
    /// <summary>
    /// Gets the type containing the methods.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the methods.
    /// </summary>
    public IReadOnlyCollection<string> Methods { get; }

    public MethodDescriptor(string type, IReadOnlyCollection<string> methods) {
      Type = type;
      Methods = methods;
    }
  }
}
