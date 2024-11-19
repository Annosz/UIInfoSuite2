using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UIInfoSuite2.Infrastructure.Structures;

public class DataCache<TKey, TValue> where TKey : IEquatable<TKey>
{
  private readonly Dictionary<TKey, TValue> _cacheMap;

  // TODO LRU Cache keys
  private readonly Func<TValue, TKey> _keyGeneratorFunc;

  public DataCache(Func<TValue, TKey> keyGeneratorFunc, IEqualityComparer<TKey>? comparer = null)
  {
    _keyGeneratorFunc = keyGeneratorFunc;
    _cacheMap = new Dictionary<TKey, TValue>(comparer);
  }

  public void Add(TValue value)
  {
    TKey key = _keyGeneratorFunc(value);
    _cacheMap[key] = value;
  }

  public void Add(TKey key, TValue value)
  {
    _cacheMap[key] = value;
  }

  public TValue? Get(TKey key)
  {
    return _cacheMap.GetValueOrDefault(key);
  }

  public bool TryGet(TKey key, [NotNullWhen(true)] out TValue? value)
  {
    return _cacheMap.TryGetValue(key, out value);
  }
}
