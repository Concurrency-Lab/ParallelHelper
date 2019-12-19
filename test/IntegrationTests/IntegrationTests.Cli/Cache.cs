using System;
using System.Collections.Generic;

namespace IntegrationTests.Cli {
  public class Cache<TKey, TValue> {
    private readonly object _syncObject = new object();
    public readonly Dictionary<TKey, TValue> _cache = new Dictionary<TKey, TValue>();

    public TValue GetOrAdd(TKey key, Func<TValue> factory) {
      lock(_syncObject) {
        TValue value;
        if(_cache.TryGetValue(key, out value)) {
          return value;
        }
        value = factory();
        _cache.Add(key, value);
        return value;
      }
    }

    public bool TryGet(TKey key, out TValue value) {
      return _cache.TryGetValue(key, out value);
    }
  }
}
