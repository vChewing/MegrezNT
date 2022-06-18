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
  /// <param name="KeyValue">鍵值。</param>
  /// <param name="Score">權重（雙精度小數）。</param>
  public Unigram(KeyValuePair KeyValue, double Score) {
    this.KeyValue = KeyValue;
    this.Score = Score;
  }

  /// <summary>
  /// 鍵值。
  /// </summary>
  public KeyValuePair KeyValue { get; set; }
  /// <summary>
  /// 權重。
  /// </summary>
  public double Score { get; set; }

  public override bool Equals(object Obj) {
    return Obj is Unigram Unigram && EqualityComparer<KeyValuePair>.Default.Equals(KeyValue, Unigram.KeyValue) &&
           Score == Unigram.Score;
  }

  public override int GetHashCode() { return HashCode.Combine(KeyValue, Score); }

  public override string ToString() => $"({KeyValue},{Score})";

  public static bool operator ==(Unigram Lhs, Unigram Rhs) {
    return Lhs.KeyValue == Rhs.KeyValue && Lhs.Score == Rhs.Score;
  }

  public static bool operator !=(Unigram Lhs, Unigram Rhs) {
    return Lhs.KeyValue != Rhs.KeyValue || Lhs.Score != Rhs.Score;
  }

  public static bool operator<(Unigram Lhs, Unigram Rhs) {
    return (Lhs.KeyValue < Rhs.KeyValue) || (Lhs.KeyValue == Rhs.KeyValue && Lhs.Score < Rhs.Score);
  }

  public static bool operator>(Unigram Lhs, Unigram Rhs) {
    return (Lhs.KeyValue > Rhs.KeyValue) || (Lhs.KeyValue == Rhs.KeyValue && Lhs.Score > Rhs.Score);
  }

  public static bool operator <=(Unigram Lhs, Unigram Rhs) {
    return (Lhs.KeyValue <= Rhs.KeyValue) || (Lhs.KeyValue == Rhs.KeyValue && Lhs.Score <= Rhs.Score);
  }

  public static bool operator >=(Unigram Lhs, Unigram Rhs) {
    return (Lhs.KeyValue >= Rhs.KeyValue) || (Lhs.KeyValue == Rhs.KeyValue && Lhs.Score >= Rhs.Score);
  }
}
}