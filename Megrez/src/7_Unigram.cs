// (c) 2022 and onwards The vChewing Project (LGPL v3.0 License or later).
// ====================
// This code is released under the SPDX-License-Identifier: `LGPL-3.0-or-later`.

using System;
using System.Collections.Generic;

namespace Megrez {
  /// <summary>
  /// 語言模型的基礎資料單位。
  /// </summary>
  public struct Unigram {
    /// <summary>
    /// 詞彙內容，可以是單字或詞組。
    /// </summary>
    /// <value>詞彙內容。</value>
    public string Value { get; }
    /// <summary>
    /// 統計權重（雙精度浮點數）。
    /// </summary>
    /// <value>統計權重。</value>
    public double Score { get; }
    /// <summary>
    /// 建立語言模型基礎資料單位實例。單元圖由詞彙內容和對應的統計權重組成。
    /// </summary>
    /// <param name="value">詞彙內容。</param>
    /// <param name="score">統計權重（雙精度浮點數）。</param>
    public Unigram(string value = "", double score = 0) {
      Value = value;
      Score = score;
    }
    /// <summary>
    ///
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object obj) => obj is Unigram unigram && Value == unigram.Value
                                               && Math.Abs(Score - unigram.Score) < 0.0000001f;
    /// <summary>
    /// 做為預設雜湊函式。
    /// </summary>
    /// <returns>目前物件的雜湊碼。</returns>
    public override int GetHashCode() {
      unchecked {
        int hash = 17;
        hash = hash * 23 + Value.GetHashCode();
        hash = hash * 23 + Score.GetHashCode();
        return hash;
      }
    }
    /// <summary>
    /// 傳回代表目前物件的字串。
    /// </summary>
    /// <returns>表示目前物件的字串。</returns>
    public override string ToString() => $"({Value},{Score})";
    /// <summary>
    ///
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    /// <returns></returns>
    public static bool operator ==(Unigram lhs, Unigram rhs) => lhs.Value == rhs.Value
                                                                && Math.Abs(lhs.Score - rhs.Score) < 0.0000001f;
    /// <summary>
    ///
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    /// <returns></returns>
    public static bool operator !=(Unigram lhs, Unigram rhs) => lhs.Value != rhs.Value
                                                                || Math.Abs(lhs.Score - rhs.Score) > 0.0000001f;
    /// <summary>
    ///
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    /// <returns></returns>
    public static bool operator <(Unigram lhs, Unigram rhs) => lhs.Score < rhs.Score
                                                              || string.Compare(lhs.Value, rhs.Value,
                                                                                StringComparison.Ordinal) < 0;
    /// <summary>
    ///
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    /// <returns></returns>
    public static bool operator >(Unigram lhs, Unigram rhs) => lhs.Score > rhs.Score
                                                              || string.Compare(lhs.Value, rhs.Value,
                                                                                StringComparison.Ordinal) > 0;
    /// <summary>
    ///
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    /// <returns></returns>
    public static bool operator <=(Unigram lhs, Unigram rhs) => lhs.Score <= rhs.Score
                                                                || string.Compare(lhs.Value, rhs.Value,
                                                                                  StringComparison.Ordinal) <= 0;
    /// <summary>
    ///
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    /// <returns></returns>
    public static bool operator >=(Unigram lhs, Unigram rhs) => lhs.Score >= rhs.Score
                                                                || string.Compare(lhs.Value, rhs.Value,
                                                                                  StringComparison.Ordinal) >= 0;
  }
}  // namespace Megrez
