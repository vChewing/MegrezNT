// (c) 2022 and onwards The vChewing Project (LGPL v3.0 License or later).
// ====================
// This code is released under the SPDX-License-Identifier: `LGPL-3.0-or-later`.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Megrez {
  /// <summary>
  /// 語言模型的基礎資料單位。
  /// </summary>
  public sealed class Unigram : IEquatable<Unigram> {
    /// <summary>
    /// 建立語言模型基礎資料單位副本。單元圖由詞彙內容和對應的統計權重組成。
    /// </summary>
    /// <param name="keyArray">索引鍵陣列。</param>
    /// <param name="value">詞彙內容。</param>
    /// <param name="score">統計權重（雙精度浮點數）。</param>
    public Unigram(List<string> keyArray, string value = "", double score = 0) {
      KeyArray = keyArray.ToList();
      Value = value;
      Score = score;
    }

    /// <summary>
    /// 建立語言模型基礎資料單位的預設副本。
    /// </summary>
    public Unigram() : this(new List<string>()) { }

    /// <summary>
    /// 索引鍵陣列。
    /// </summary>
    public List<string> KeyArray { get; }

    /// <summary>
    /// 詞彙內容，可以是單字或詞組。
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// 統計權重（雙精度浮點數）。
    /// </summary>
    public double Score { get; }

    /// <summary>
    /// 幅長（索引鍵陣列的元素數量）。
    /// </summary>
    public int SegLength => KeyArray.Count;

    /// <summary>
    /// 檢查是否「讀音字長與候選字字長不一致」。
    /// </summary>
    public bool IsReadingMismatched => KeyArray.Count != Value.LiteralCount();

    /// <summary>
    /// 建立一個單元圖的淺層複製品。
    /// </summary>
    /// <param name="keyArrayOverride">如指定，則使用新的索引鍵陣列。</param>
    /// <returns>單元圖複製品。</returns>
    public Unigram Copy(List<string>? keyArrayOverride = null) {
      List<string> newKeyArray = keyArrayOverride?.ToList() ?? KeyArray.ToList();
      return new(newKeyArray, Value, Score);
    }

    /// <summary>
    /// 傳回代表目前物件的字串。
    /// </summary>
    /// <returns>表示目前物件的字串。</returns>
    public override string ToString() => $"({string.Join("-", KeyArray)},{Value},{Score})";

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Unigram);

    /// <inheritdoc />
    public bool Equals(Unigram? other) {
      if (ReferenceEquals(null, other)) return false;
      if (ReferenceEquals(this, other)) return true;
      return KeyArray.SequenceEqual(other.KeyArray)
             && Value == other.Value
             && Math.Abs(Score - other.Score) < 0.0000001f;
    }

    /// <inheritdoc />
    public override int GetHashCode() {
      unchecked {
        int hash = 17;
        foreach (string key in KeyArray) {
          hash = hash * 23 + key.GetHashCode();
        }

        hash = hash * 23 + Value.GetHashCode();
        hash = hash * 23 + Score.GetHashCode();
        return hash;
      }
    }

    /// <summary>
    /// 判斷兩個單元圖是否相等。
    /// </summary>
    public static bool operator ==(Unigram lhs, Unigram rhs) => lhs?.Equals(rhs) ?? rhs is null;

    /// <summary>
    /// 判斷兩個單元圖是否不相等。
    /// </summary>
    public static bool operator !=(Unigram lhs, Unigram rhs) => !(lhs == rhs);
  }
} // namespace Megrez
