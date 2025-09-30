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
  /// 一個「可以返回整數的上下限」的自訂閉區間類型。
  /// </summary>
  public struct ClosedRange : IEnumerable<int> {
    public int Lowerbound { get; }
    public int Upperbound { get; }
    public ClosedRange(int lowerbound, int upperbound) {
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
  /// 一個允許自動交換上下界的閉區間型別。
  /// </summary>
  public struct ClosedRangeSwappable : IEnumerable<int> {
    public int Lowerbound { get; }
    public int Upperbound { get; }
    public ClosedRangeSwappable(int lowerbound, int upperbound) {
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
}  // namespace Megrez
