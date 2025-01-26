// CSharpened and further development by (c) 2022 and onwards The vChewing Project (MIT License).
// Was initially rebranded from (c) Lukhnos Liu's C++ library "Gramambular 2" (MIT License).
// Walking algorithm (Dijkstra) implemented by (c) 2025 and onwards The vChewing Project (MIT License).
// ====================
// This code is released under the MIT license (SPDX-License-Identifier: MIT)

#pragma warning disable CS1591

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Megrez {
/// <summary>
/// C# 某些地方有點難用，就在這裡客製化一些功能拓展。
/// </summary>
public static class CSharpExtensions {
  // MARK: - String.Joined (Swift-Style)

  public static string Joined(this IEnumerable<string> self, string separator = "") {
    if (!string.IsNullOrEmpty(separator)) return string.Join(separator, self);
    StringBuilder output = new();
    foreach (string x in self) output.Append(x);
    return output.ToString();
  }

  // MARK: - UTF8 String Length

  public static int LiteralCount(this string self) => new StringInfo(self).LengthInTextElements;

  // MARK: - UTF8 String Char Array

  public static List<string> LiteralCharComponents(this string self) {
    List<string> result = new();
    TextElementEnumerator charEnum = StringInfo.GetTextElementEnumerator(self);
    while (charEnum.MoveNext()) result.Add(charEnum.GetTextElement());
    return result;
  }

  // MARK: - Enumerable.IsEmpty()

  public static bool IsEmpty<T>(this List<T> theObject) => theObject.Count == 0;

  // MARK: - Enumerable.Enumerated() (Swift-Style)

  public static IEnumerable<EnumeratedItem<T>> Enumerated<T>(this IEnumerable<T> source) =>
      source.Select((item, index) => new EnumeratedItem<T>(index, item));

  // MARK: - List.Reversed() (Swift-Style)
  public static List<T> Reversed<T>(this List<T> self) => self.ToArray().Reverse().ToList();

  // MARK: - Stable Sort Extension

  // Ref: https://stackoverflow.com/a/148123/4162914

  public static void StableSort<T>(this T[] values, Comparison<T> comparison) {
    KeyValuePair<int, T>[] keys = new KeyValuePair<int, T>[values.Length];
    for (int i = 0; i < values.Length; i++) keys[i] = new(i, values[i]);
    Array.Sort(keys, values, new StabilizingComparer<T>(comparison));
  }

  public static List<T> StableSorted<T>(this List<T> values, Comparison<T> comparison) {
    KeyValuePair<int, T>[] keys = new KeyValuePair<int, T>[values.Count()];
    for (int i = 0; i < values.Count(); i++) keys[i] = new(i, values[i]);
    T[] theValues = values.ToArray();
    Array.Sort(keys, theValues, new StabilizingComparer<T>(comparison));
    return theValues.ToList();
  }

  private sealed class StabilizingComparer<T> : IComparer<KeyValuePair<int, T>> {
    private readonly Comparison<T> _comparison;

    public StabilizingComparer(Comparison<T> comparison) { _comparison = comparison; }

    public int Compare(KeyValuePair<int, T> x, KeyValuePair<int, T> y) {
      int result = _comparison(x.Value, y.Value);
      return result != 0 ? result : x.Key.CompareTo(y.Key);
    }
  }
}

// MARK: - Range with Int Bounds

/// <summary>
/// 一個「可以返回整數的上下限」的自訂 Range 類型。
/// </summary>
public struct BRange : IEnumerable<int> {
  public int Lowerbound { get; }
  public int Upperbound { get; }
  public BRange(int lowerbound, int upperbound) {
    Lowerbound = Math.Min(lowerbound, upperbound);
    Upperbound = Math.Max(lowerbound, upperbound);
  }

  public List<int> ToList() {
    List<int> result = new();
    for (int i = Lowerbound; i < Upperbound; i++) {
      result.Add(i);
    }
    return result;
  }

  public IEnumerable<EnumeratedItem<int>> Enumerated() => ToList().Enumerated();

  IEnumerator<int> IEnumerable<int>.GetEnumerator() => ToList().GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => ToList().GetEnumerator();
}

public struct EnumeratedItem<T> {
  public int Offset;
  public T Value;
  public EnumeratedItem(int offset, T value) {
    Offset = offset;
    Value = value;
  }
}

// MARK: - HybridPriorityQueue

/// <summary>
/// 針對 Sandy Bridge 架構最佳化的混合優先佇列實作。
/// </summary>
public class HybridPriorityQueue<T> : IDisposable
    where T : IComparable<T> {
  // 考慮 Sandy Bridge 的 L1 快取大小，調整閾值以符合 32KB L1D 快取行為。
  private const int ArrayThreshold = 12;   // 快取大小。
  private const int InitialCapacity = 16;  // 預設容量設為 2 的冪次以優化記憶體對齊。
  private T[] _storage;                    // 改用陣列以減少記憶體間接引用。
  private int _count;                      // 追蹤實際元素數量。
  private readonly bool _isReversed;
  private bool _usingArray;

  public HybridPriorityQueue(bool reversed = false) {
    _isReversed = reversed;
    _storage = new T[InitialCapacity];
    _count = 0;
    _usingArray = true;
  }

  /// <summary>
  /// 取得佇列中的元素數量。
  /// </summary>
  public int Count => _count;

  /// <summary>
  /// 檢查佇列是否為空。
  /// </summary>
  public bool IsEmpty => _count == 0;

  public void Enqueue(T element) {
    // 確保容量足夠
    if (_count == _storage.Length) {
      Array.Resize(ref _storage, _storage.Length * 2);
    }

    if (_usingArray) {
      if (_count >= ArrayThreshold) {
        SwitchToHeap();
        _storage[_count++] = element;
        HeapifyUp(_count - 1);
        return;
      }

      // 使用二分搜尋找到插入點。
      int insertIndex = FindInsertionPoint(element);
      // 手動移動元素以避免使用 Array.Copy（減少函數呼叫開銷）。
      for (int i = _count; i > insertIndex; i--) {
        _storage[i] = _storage[i - 1];
      }
      _storage[insertIndex] = element;
      _count++;
    } else {
      _storage[_count] = element;
      HeapifyUp(_count++);
    }
  }

  public T? Dequeue() {
    if (_count == 0) return default;

    T result = _storage[0];
    _count--;

    if (_usingArray) {
      // 手動移動元素以避免使用 Array.Copy。
      for (int i = 0; i < _count; i++) {
        _storage[i] = _storage[i + 1];
      }
      return result;
    }

    // 堆積模式。
    _storage[0] = _storage[_count];
    if (_count > 0) HeapifyDown(0);
    return result;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private int FindInsertionPoint(T element) {
    int left = 0;
    int right = _count;

    // 展開循環以提高分支預測效率。
    while (right - left > 1) {
      int mid = (left + right) >> 1;
      int midStorage = element.CompareTo(_storage[mid]);
      if (_isReversed ? midStorage > 0 : midStorage < 0) {
        right = mid;
      } else {
        left = mid;
      }
    }

    // 處理邊界情況。
    int leftStorage = element.CompareTo(_storage[left]);
    bool marginCondition = _isReversed ? leftStorage <= 0 : leftStorage >= 0;
    return left < _count && marginCondition ? left + 1 : left;
  }

  private void SwitchToHeap() {
    try {
      _usingArray = false;
      // 就地轉換為堆積，使用更有效率的方式。
      for (int i = (_count >> 1) - 1; i >= 0; i--) {
        HeapifyDown(i);
      }
    } catch {
      Clear();  // 確保發生異常時也能清理。
      throw;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void HeapifyUp(int index) {
    T item = _storage[index];
    while (index > 0) {
      int parentIndex = (index - 1) >> 1;
      T parent = _storage[parentIndex];
      if (Compare(item, parent) >= 0) break;
      _storage[index] = parent;
      index = parentIndex;
    }
    _storage[index] = item;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void HeapifyDown(int index) {
    T item = _storage[index];
    int half = _count >> 1;

    while (index < half) {
      int leftChild = (index << 1) + 1;
      int rightChild = leftChild + 1;
      int bestChild = leftChild;

      T leftChildItem = _storage[leftChild];

      if (rightChild < _count) {
        T rightChildItem = _storage[rightChild];
        if (Compare(rightChildItem, leftChildItem) < 0) {
          bestChild = rightChild;
          leftChildItem = rightChildItem;
        }
      }

      if (Compare(item, leftChildItem) <= 0) break;

      _storage[index] = leftChildItem;
      index = bestChild;
    }
    _storage[index] = item;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private int Compare(T a, T b) => _isReversed ? b.CompareTo(a) : a.CompareTo(b);

  /// <summary>
  /// 清空佇列。
  /// </summary>
  public void Clear() {
    _count = 0;
    _usingArray = true;
    if (_storage.Length > InitialCapacity) {
      _storage = new T[InitialCapacity];
    }
    Array.Clear(_storage, 0, _storage.Length);  // 清除所有參考。
  }

  public void Dispose() {
    Clear();
    _storage = null;
  }
}
}  // namespace Megrez
