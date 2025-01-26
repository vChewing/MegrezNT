// CSharpened and further development by (c) 2022 and onwards The vChewing Project (MIT License).
// Was initially rebranded from (c) Lukhnos Liu's C++ library "Gramambular 2" (MIT License).
// Walking algorithm (Dijkstra) implemented by (c) 2025 and onwards The vChewing Project (MIT License).
// ====================
// This code is released under the MIT license (SPDX-License-Identifier: MIT)

using System;
using System.Collections.Generic;

namespace Megrez {
/// <summary>
/// 單元圖。
/// </summary>
public struct Unigram {
  /// <summary>
  /// 資料值，通常是詞語或單個字。
  /// </summary>
  /// <value>資料值。</value>
  public string Value { get; }
  /// <summary>
  /// 權重（雙精度小數）。
  /// </summary>
  /// <value>權重。</value>
  public double Score { get; }
  /// <summary>
  /// 初期化一筆「單元圖」。一筆單元圖由一筆資料值與一筆權重數值組成。
  /// </summary>
  /// <param name="value">資料值。</param>
  /// <param name="score">權重（雙精度小數）。</param>
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
  public override int GetHashCode() => new KeyValuePair<string, double>(Value, Score).GetHashCode();
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
  public static bool operator<(Unigram lhs, Unigram rhs) => lhs.Score < rhs.Score
                                                            || string.Compare(lhs.Value, rhs.Value,
                                                                              StringComparison.Ordinal) < 0;
  /// <summary>
  ///
  /// </summary>
  /// <param name="lhs"></param>
  /// <param name="rhs"></param>
  /// <returns></returns>
  public static bool operator>(Unigram lhs, Unigram rhs) => lhs.Score > rhs.Score
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
