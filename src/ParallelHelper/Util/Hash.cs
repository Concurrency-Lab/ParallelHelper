using System.Collections.Immutable;
using System.Linq;

namespace ParallelHelper.Util {
  /// <summary>
  /// Used to calculate hash codes of given objects.
  /// </summary>
  public class Hash {
    private const int HashPrime = 59;
    private const int NullHash = 0;

    private readonly int _value;

    private Hash() : this(1) { }

    private Hash(int value) {
      _value = value;
    }

    /// <summary>
    /// Creates a new hash object with a given object.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="obj">Object to start the hash chain with.</param>
    /// <returns>The generated hash object.</returns>
    public static Hash With<T>(T obj) {
      return new Hash().And(obj);
    }

    /// <summary>
    /// Creates a new hash object with a immutable set.
    /// </summary>
    /// <typeparam name="T">The type of the immutable set's values.</typeparam>
    /// <param name="immutableSet">The immutable set to get the hash of.</param>
    /// <returns>A new hash object.</returns>
    public static Hash WithSet<T>(IImmutableSet<T> immutableSet) {
      return new Hash().AndSet(immutableSet);
    }

    /// <summary>
    /// Gets a new hash object with the given object.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="obj">Object to append to the hash chain.</param>
    /// <returns>A new hash object.</returns>
    public Hash And<T>(T obj) {
      return new Hash(HashPrime * _value + (obj?.GetHashCode() ?? NullHash));
    }

    /// <summary>
    /// Gets a new hash object with the immutable set.
    /// </summary>
    /// <typeparam name="T">The type of the immutable set's values.</typeparam>
    /// <param name="immutableSet">The immutable set to get the hash of.</param>
    /// <returns>A new hash object.</returns>
    public Hash AndSet<T>(IImmutableSet<T> immutableSet) {
      // TODO the order of the items has to be consistent to produce the same hashcode.
      int hash = _value;
      foreach(var entry in immutableSet.Select(e => e?.GetHashCode() ?? NullHash).OrderBy(e => e)) {
        hash = HashPrime * hash + entry;
      }
      return new Hash(hash);
    }

    /// <summary>
    /// Gets the current hash.
    /// </summary>
    /// <returns>The current hash.</returns>
    public int Get() {
      return _value;
    }
  }
}
