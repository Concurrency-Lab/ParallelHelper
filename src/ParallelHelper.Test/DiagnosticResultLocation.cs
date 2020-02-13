using System;

namespace ParallelHelper.Test {
  /// <summary>
  /// Location where the diagnostic appears, as determined by path, line number, and column number.
  /// </summary>
  public struct DiagnosticResultLocation {
    public string Path { get; }
    public int Line { get; }
    public int Column { get; }

    private readonly int _hashCode;

    public DiagnosticResultLocation(int line, int column) : this("Test.cs", line, column) { }

    public DiagnosticResultLocation(string path, int line, int column) {
      Path = path;
      Line = line;
      Column = column;
      _hashCode = HashCode.Combine(path, Line, Column);
    }

    public override bool Equals(object? obj) {
      return obj is DiagnosticResultLocation other
        && Equals(Path, other.Path)
        && Equals(Line, other.Line)
        && Equals(Column, other.Column);
    }

    public override int GetHashCode() {
      return _hashCode;
    }

    public override string ToString() {
      return $"{Path}: {Line},{Column}";
    }

    public static bool operator==(DiagnosticResultLocation left, DiagnosticResultLocation right) {
      if(left == null) {
        return right == null;
      }
      return left.Equals(right);
    }

    public static bool operator!=(DiagnosticResultLocation left, DiagnosticResultLocation right) {
      return !(left == right);
    }
  }
}
