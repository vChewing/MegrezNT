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
public struct KeyValuePaired {
  /// <summary>
  /// 初期化一組鍵值配對。
  /// </summary>
  /// <param name="key">鍵。一般情況下用來放置讀音等可以用來作為索引的內容。</param>
  /// <param name="value">資料值。</param>
  public KeyValuePaired(string key = "", string value = "") {
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

  /// <summary>
  /// 該鍵值配對是否不為空。
  /// </summary>
  /// <returns>只要鍵或者值任一為空，則傳回值為「否」。</returns>
  public bool IsValid() => !string.IsNullOrEmpty(Key) && !string.IsNullOrEmpty(Value);

  /// <summary>
  /// 判定兩個物件是否相等。
  /// </summary>
  /// <param name="obj">用來比較的物件。</param>
  /// <returns>若相等，則返回 true。</returns>
  public override bool Equals(object obj) {
    return obj is KeyValuePaired pair && Key == pair.Key && Value == pair.Value;
  }

  /// <summary>
  /// 將當前物件的內容輸出為雜湊資料。
  /// </summary>
  /// <returns>當前物件的內容輸出成的雜湊資料。</returns>
  public override int GetHashCode() => HashCode.Combine(Key, Value);

  /// <summary>
  /// 將當前物件的內容輸出為字串。
  /// </summary>
  /// <returns>當前物件的內容輸出成的字串。</returns>
  public override string ToString() => $"({Key},{Value})";

  /// <summary>
  /// 生成統一索引鍵，以作其它用途。如果該鍵值配對有任一為空，則生成空的統一索引鍵。
  /// </summary>
  /// <returns>生成的統一索引鍵。</returns>
  public string ToNGramKey() => IsValid() ? $"({Key},{Value})" : "()";
  /// <summary>
  /// 判定兩個物件是否相等。
  /// </summary>
  /// <param name="lhs"></param>
  /// <param name="rhs"></param>
  /// <returns></returns>
  public static bool operator ==(KeyValuePaired lhs, KeyValuePaired rhs) {
    return lhs.Key.Length == rhs.Key.Length && lhs.Value == rhs.Value;
  }
  /// <summary>
  /// 判定兩個物件是否相異。
  /// </summary>
  /// <param name="lhs"></param>
  /// <param name="rhs"></param>
  /// <returns></returns>
  public static bool operator !=(KeyValuePaired lhs, KeyValuePaired rhs) {
    return lhs.Key.Length != rhs.Key.Length || lhs.Value != rhs.Value;
  }

  /// <summary>
  ///
  /// </summary>
  /// <param name="lhs"></param>
  /// <param name="rhs"></param>
  /// <returns></returns>
  public static bool operator<(KeyValuePaired lhs, KeyValuePaired rhs) {
    return lhs.Key.Length < rhs.Key.Length ||
           lhs.Key.Length == rhs.Key.Length && String.Compare(lhs.Value, rhs.Value, StringComparison.Ordinal) < 0;
  }

  /// <summary>
  ///
  /// </summary>
  /// <param name="lhs"></param>
  /// <param name="rhs"></param>
  /// <returns></returns>
  public static bool operator>(KeyValuePaired lhs, KeyValuePaired rhs) {
    return lhs.Key.Length > rhs.Key.Length ||
           lhs.Key.Length == rhs.Key.Length && String.Compare(lhs.Value, rhs.Value, StringComparison.Ordinal) > 0;
  }

  /// <summary>
  ///
  /// </summary>
  /// <param name="lhs"></param>
  /// <param name="rhs"></param>
  /// <returns></returns>
  public static bool operator <=(KeyValuePaired lhs, KeyValuePaired rhs) {
    return lhs.Key.Length <= rhs.Key.Length ||
           lhs.Key.Length == rhs.Key.Length && String.Compare(lhs.Value, rhs.Value, StringComparison.Ordinal) <= 0;
  }

  /// <summary>
  ///
  /// </summary>
  /// <param name="lhs"></param>
  /// <param name="rhs"></param>
  /// <returns></returns>
  public static bool operator >=(KeyValuePaired lhs, KeyValuePaired rhs) {
    return lhs.Key.Length >= rhs.Key.Length ||
           lhs.Key.Length == rhs.Key.Length && String.Compare(lhs.Value, rhs.Value, StringComparison.Ordinal) >= 0;
  }
}
}