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
using System.Collections.Generic;

namespace Megrez {
/// <summary>
/// 單元圖。
/// </summary>
public struct Unigram {
  /// <summary>
  /// 初期化一筆「單元圖」。一筆單元圖由一組鍵值配對與一筆權重數值組成。
  /// </summary>
  /// <param name="keyValue">鍵值。</param>
  /// <param name="score">權重（雙精度小數）。</param>
  public Unigram(KeyValuePaired keyValue, double score) {
    KeyValue = keyValue;
    Score = score;
  }

  /// <summary>
  /// 鍵值。
  /// </summary>
  public KeyValuePaired KeyValue { get; set; }
  /// <summary>
  /// 權重。
  /// </summary>
  public double Score { get; set; }

  /// <summary>
  /// 判定兩個物件是否相等。
  /// </summary>
  /// <param name="obj">用來比較的物件。</param>
  /// <returns>若相等，則返回 true。</returns>
  public override bool Equals(object obj) {
    return obj is Unigram unigram && EqualityComparer<KeyValuePaired>.Default.Equals(KeyValue, unigram.KeyValue) &&
           Math.Abs(Score - unigram.Score) < 0.0000001f;
  }

  /// <summary>
  /// 將當前物件的內容輸出為雜湊資料。
  /// </summary>
  /// <returns>當前物件的內容輸出成的雜湊資料。</returns>
  public override int GetHashCode() { return HashCode.Combine(KeyValue, Score); }

  /// <summary>
  /// 將當前物件的內容輸出為字串。
  /// </summary>
  /// <returns>當前物件的內容輸出成的字串。</returns>
  public override string ToString() => $"({KeyValue},{Score})";
  /// <summary>
  /// 判定兩個物件是否相等。
  /// </summary>
  /// <param name="lhs"></param>
  /// <param name="rhs"></param>
  /// <returns></returns>
  public static bool operator ==(Unigram lhs, Unigram rhs) {
    return lhs.KeyValue == rhs.KeyValue && Math.Abs(lhs.Score - rhs.Score) < 0.0000001f;
  }
  /// <summary>
  /// 判定兩個物件是否相異。
  /// </summary>
  /// <param name="lhs"></param>
  /// <param name="rhs"></param>
  /// <returns></returns>
  public static bool operator !=(Unigram lhs, Unigram rhs) => lhs.KeyValue != rhs.KeyValue
                                                              || Math.Abs(lhs.Score - rhs.Score) > 0.0000001f;

  /// <summary>
  ///
  /// </summary>
  /// <param name="lhs"></param>
  /// <param name="rhs"></param>
  /// <returns></returns>
  public static bool operator<(Unigram lhs, Unigram rhs) {
    return lhs.KeyValue < rhs.KeyValue || lhs.KeyValue == rhs.KeyValue && lhs.Score < rhs.Score;
  }

  /// <summary>
  ///
  /// </summary>
  /// <param name="lhs"></param>
  /// <param name="rhs"></param>
  /// <returns></returns>
  public static bool operator>(Unigram lhs, Unigram rhs) {
    return lhs.KeyValue > rhs.KeyValue || lhs.KeyValue == rhs.KeyValue && lhs.Score > rhs.Score;
  }

  /// <summary>
  ///
  /// </summary>
  /// <param name="lhs"></param>
  /// <param name="rhs"></param>
  /// <returns></returns>
  public static bool operator <=(Unigram lhs, Unigram rhs) {
    return lhs.KeyValue <= rhs.KeyValue || lhs.KeyValue == rhs.KeyValue && lhs.Score <= rhs.Score;
  }

  /// <summary>
  ///
  /// </summary>
  /// <param name="lhs"></param>
  /// <param name="rhs"></param>
  /// <returns></returns>
  public static bool operator >=(Unigram lhs, Unigram rhs) {
    return lhs.KeyValue >= rhs.KeyValue || lhs.KeyValue == rhs.KeyValue && lhs.Score >= rhs.Score;
  }
}
}