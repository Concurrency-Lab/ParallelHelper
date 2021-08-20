using System.Collections.Immutable;

namespace ParallelHelper.Analyzer {
  /// <summary>
  /// Data class to represent a type with a list of class members by their names.
  /// </summary>
  public class ClassMemberDescriptor {
    /// <summary>
    /// Gets the type containing the members.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the class members.
    /// </summary>
    public IImmutableSet<string> Members { get; }

    public ClassMemberDescriptor(string type, params string[] members) {
      Type = type;
      Members = members.ToImmutableHashSet();
    }
  }
}
