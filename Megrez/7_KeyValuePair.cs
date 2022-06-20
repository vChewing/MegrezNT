// CSharpened by (c) 2022 and onwards The vChewing Project (MIT-NTL License).
// Rebranded from (c) Lukhnos Liu's C++ library "Gramambular" (MIT License).
/*
Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

1. The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

2. No trademark license is granted to use the trade names, trademarks, service
marks, or product names of Contributor, except as required to fulfill notice
requirements above.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
namespace Megrez {
/// <summary>
/// 鍵值配對。
/// </summary>
public struct KeyValuePair {
  /// <summary>
  /// 初期化一組鍵值配對。
  /// </summary>
  /// <param name="key">鍵。一般情況下用來放置讀音等可以用來作為索引的內容。</param>
  /// <param name="value">資料值。</param>
  public KeyValuePair(string key, string value) {
    Key = key;
    Value = value;
  }

  /// <summary>
  /// 鍵。一般情況下用來放置讀音等可以用來作為索引的內容。
  /// </summary>
  public string Key { get; }
  /// <summary>
  /// 資料值。
  /// </summary>
  public string Value { get; }

  public override bool Equals(object obj) { return obj is KeyValuePair pair && Key == pair.Key && Value == pair.Value; }

  public override int GetHashCode() { return HashCode.Combine(Key, Value); }

  public override string ToString() => $"({Key},{Value})";

  public static bool operator ==(KeyValuePair lhs, KeyValuePair rhs) {
    return lhs.Key.Length == rhs.Key.Length && lhs.Value == rhs.Value;
  }

  public static bool operator !=(KeyValuePair lhs, KeyValuePair rhs) {
    return lhs.Key.Length != rhs.Key.Length || lhs.Value != rhs.Value;
  }

  public static bool operator<(KeyValuePair lhs, KeyValuePair rhs) {
    return lhs.Key.Length < rhs.Key.Length || lhs.Key.Length == rhs.Key.Length && lhs.Value.CompareTo(rhs.Value) < 0;
  }

  public static bool operator>(KeyValuePair lhs, KeyValuePair rhs) {
    return lhs.Key.Length > rhs.Key.Length || lhs.Key.Length == rhs.Key.Length && lhs.Value.CompareTo(rhs.Value) > 0;
  }

  public static bool operator <=(KeyValuePair lhs, KeyValuePair rhs) {
    return lhs.Key.Length <= rhs.Key.Length || lhs.Key.Length == rhs.Key.Length && lhs.Value.CompareTo(rhs.Value) <= 0;
  }

  public static bool operator >=(KeyValuePair lhs, KeyValuePair rhs) {
    return lhs.Key.Length >= rhs.Key.Length || lhs.Key.Length == rhs.Key.Length && lhs.Value.CompareTo(rhs.Value) >= 0;
  }
}
}