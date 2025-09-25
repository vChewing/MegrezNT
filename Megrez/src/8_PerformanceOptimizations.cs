// (c) 2025 and onwards The vChewing Project (LGPL v3.0 License or later).
// ====================
// This code is released under the SPDX-License-Identifier: `LGPL-3.0-or-later`.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Megrez {
  /// <summary>
  /// 用於頻繁計算的字串合併操作快取
  /// </summary>
  internal sealed class StringJoinCache {
    private static readonly Lazy<StringJoinCache> _shared = new(() => new StringJoinCache());
    public static StringJoinCache Shared => _shared.Value;

    private readonly Dictionary<string, string> _joinCache = new();
    private readonly object _lock = new();
    private const int MaxCacheSize = 1000;

    private StringJoinCache() { }

    public string GetCachedJoin(List<string> strings, string separator) {
      string key = string.Join("|", strings) + "|" + separator;

      lock (_lock) {
        if (_joinCache.TryGetValue(key, out string? cached)) {
          return cached;
        }

        string result = string.Join(separator, strings);

        // 防止快取無限制增長
        if (_joinCache.Count < MaxCacheSize) {
          _joinCache[key] = result;
        }

        return result;
      }
    }

    public void Clear() {
      lock (_lock) {
        _joinCache.Clear();
      }
    }
  }

  /// <summary>
  /// 輕量級物件池，用於減少頻繁配置臨時陣列
  /// </summary>
  /// <typeparam name="T">陣列元素類型</typeparam>
  internal sealed class ArrayPool<T> {
    private readonly Stack<List<T>> _arrays = new();
    private readonly Func<List<T>> _createArray;
    private readonly Action<List<T>> _resetArray;
    private readonly object _lock = new();

    public ArrayPool(Func<List<T>>? createArray = null, Action<List<T>>? resetArray = null) {
      _createArray = createArray ?? (() => new List<T>());
      _resetArray = resetArray ?? (array => array.Clear());
    }

    public List<T> Borrow() {
      lock (_lock) {
        if (_arrays.Count > 0) {
          return _arrays.Pop();
        }
        return _createArray();
      }
    }

    public void Return(List<T> array) {
      lock (_lock) {
        _resetArray(array);
        _arrays.Push(array);
      }
    }

    public TResult WithBorrowedArray<TResult>(Func<List<T>, TResult> body) {
      List<T> array = Borrow();
      try {
        return body(array);
      } finally {
        Return(array);
      }
    }
  }
}
