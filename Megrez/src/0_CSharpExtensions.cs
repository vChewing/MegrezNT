// (c) 2022 and onwards The vChewing Project (LGPL v3.0 License or later).
// ====================
// This code is released under the SPDX-License-Identifier: `LGPL-3.0-or-later`.

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
  }

  // MARK: - Range with Int Bounds

  /// <summary>
  /// 一個「可以返回整數的上下限」的自訂 Range 類型。
  /// </summary>
  public struct BRange : IEnumerable<int> {
    public int Lowerbound { get; }
    public int Upperbound { get; }
    public BRange(int lowerbound, int upperbound) {
      Lowerbound = lowerbound;
      Upperbound = (upperbound < lowerbound) ? lowerbound : upperbound;
    }

    public List<int> ToList() {
      List<int> result = new();
      for (int i = Lowerbound; i <= Upperbound; i++) {
        result.Add(i);
      }
      return result;
    }

    public IEnumerable<EnumeratedItem<int>> Enumerated() => ToList().Enumerated();

    IEnumerator<int> IEnumerable<int>.GetEnumerator() => ToList().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ToList().GetEnumerator();
  }

  /// <summary>
  /// 一個「可以返回整數的上下限」的自訂 Range 類型。該類型允許邊界顛倒。
  /// </summary>
  public struct BRangeSwappable : IEnumerable<int> {
    public int Lowerbound { get; }
    public int Upperbound { get; }
    public BRangeSwappable(int lowerbound, int upperbound) {
      Lowerbound = Math.Min(lowerbound, upperbound);
      Upperbound = Math.Max(lowerbound, upperbound);
    }

    public List<int> ToList() {
      List<int> result = new();
      for (int i = Lowerbound; i <= Upperbound; i++) {
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
    private readonly bool _isReversed;
    private bool _usingArray;
    private readonly object _syncRoot = new();
    private bool _isDisposed;

    public HybridPriorityQueue(bool reversed = false) {
      _isReversed = reversed;
      _storage = new T[InitialCapacity];
      Count = 0;
      _usingArray = true;
    }

    /// <summary>
    /// 取得佇列中的元素數量。
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// 檢查佇列是否為空。
    /// </summary>
    public bool IsEmpty => Count == 0;

    public void Enqueue(T element) {
      lock (_syncRoot) {
        // 確保容量足夠
        if (Count == _storage.Length) {
          Array.Resize(ref _storage, _storage.Length * 2);
        }

        if (_usingArray) {
          if (Count >= ArrayThreshold) {
            SwitchToHeap();
            _storage[Count++] = element;
            HeapifyUp(Count - 1);
            return;
          }

          // 使用二分搜尋找到插入點。
          int insertIndex = FindInsertionPoint(element);
          // 手動移動元素以避免使用 Array.Copy（減少函數呼叫開銷）。
          for (int i = Count; i > insertIndex; i--) {
            _storage[i] = _storage[i - 1];
          }
          _storage[insertIndex] = element;
          Count++;
        } else {
          _storage[Count] = element;
          HeapifyUp(Count++);
        }
      }
    }

    public T? Dequeue() {
      lock (_syncRoot) {
        if (Count == 0) return default;

        T result = _storage[0];
        Count--;

        if (_usingArray) {
          // 手動移動元素以避免使用 Array.Copy。
          for (int i = 0; i < Count; i++) {
            _storage[i] = _storage[i + 1];
          }
          return result;
        }

        // 堆積模式。
        _storage[0] = _storage[Count];
        if (Count > 0) HeapifyDown(0);
        return result;
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindInsertionPoint(T element) {
      int left = 0;
      int right = Count;

      while (left < right) {
        int mid = left + ((right - left) >> 1);
        int comparison = Compare(element, _storage[mid]);
        if (comparison < 0) {
          right = mid;
        } else {
          left = mid + 1;
        }
      }

      // 處理邊界情況
      if (left > 0) {
        int comparison = Compare(element, _storage[left - 1]);
        if (comparison < 0) {
          return left - 1;
        }
      }
      return left;
    }

    private void SwitchToHeap() {
      T[] backupStorage = (T[])_storage.Clone();
      int backupCount = Count;

      try {
        _usingArray = false;
        for (int i = (Count >> 1) - 1; i >= 0; i--) {
          HeapifyDown(i);
        }
      } catch (Exception) {
        _storage = backupStorage;
        Count = backupCount;
        _usingArray = true;
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
      int half = Count >> 1;

      while (index < half) {
        int leftChild = (index << 1) + 1;
        int rightChild = leftChild + 1;
        int bestChild = leftChild;

        T leftChildItem = _storage[leftChild];

        if (rightChild < Count) {
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
      lock (_syncRoot) {
        if (_storage.Length != 0) {
          Array.Clear(_storage, 0, _storage.Length);
          if (_storage.Length > InitialCapacity) {
            _storage = new T[InitialCapacity];
          }
        }
        Count = 0;
        _usingArray = true;
      }
    }

    public void Dispose() {
      if (_isDisposed) return;

      Clear();
      if (_storage.Length != 0) {
        Array.Clear(_storage, 0, _storage.Length);
        _storage = null!;
      }
      _isDisposed = true;
      GC.SuppressFinalize(this);
    }

    protected virtual void ThrowIfDisposed() {
      if (_isDisposed) {
        throw new ObjectDisposedException(nameof(HybridPriorityQueue<T>));
      }
    }

    public override int GetHashCode() {
      unchecked {
        int hash = 17;
        hash = hash * 23 + _storage.GetHashCode();
        hash = hash * 23 + Count.GetHashCode();
        hash = hash * 23 + _isReversed.GetHashCode();
        hash = hash * 23 + _usingArray.GetHashCode();
        return hash;
      }
    }
  }
}  // namespace Megrez
