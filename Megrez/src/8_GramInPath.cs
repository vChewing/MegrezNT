// (c) 2022 and onwards The vChewing Project (LGPL v3.0 License or later).
// ====================
// This code is released under the SPDX-License-Identifier: `LGPL-3.0-or-later`.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Megrez {
  /// <summary>
  /// 輕量化節點封裝，便於將節點內的有效資訊以不可變形式獨立傳遞。
  /// </summary>
  /// <remarks>
  /// 該結構體的所有成員均為不可變狀態。
  /// </remarks>
  public readonly struct GramInPath : IEquatable<GramInPath> {
    // MARK: - Lifecycle

    /// <summary>
    /// 初期化一個新的 GramInPath 副本。
    /// </summary>
    /// <param name="gram">單元圖。</param>
    /// <param name="isExplicit">是否被覆寫。</param>
    public GramInPath(Unigram gram, bool isExplicit) {
      Gram = gram;
      IsExplicit = isExplicit;
    }

    // MARK: - Properties

    /// <summary>
    /// 單元圖資料。
    /// </summary>
    public readonly Unigram Gram;

    /// <summary>
    /// 是否被覆寫。
    /// </summary>
    public readonly bool IsExplicit;

    /// <summary>
    /// 索引鍵陣列。
    /// </summary>
    public List<string> KeyArray => Gram.KeyArray;

    /// <summary>
    /// 讀音是否不匹配。
    /// </summary>
    public bool IsReadingMismatched => Gram.IsReadingMismatched;

    /// <summary>
    /// 單元圖的值。
    /// </summary>
    public string Value => Gram.Value;

    /// <summary>
    /// 單元圖的權重分數。
    /// </summary>
    public double Score => Gram.Score;

    /// <summary>
    /// 區段長度（索引鍵陣列的數量）。
    /// </summary>
    public int SegLength => Gram.SegLength;

    /// <summary>
    /// 該節點當前狀態所展示的鍵值配對。
    /// </summary>
    public KeyValuePaired AsCandidatePair => new(KeyArray, Value);

    /// <summary>
    /// 將當前單元圖的讀音陣列按照給定的分隔符銜接成一個字串。
    /// </summary>
    /// <param name="separator">給定的分隔符，預設值為 Compositor.TheSeparator。</param>
    /// <returns>已經銜接完畢的字串。</returns>
    public string JoinedCurrentKey(string separator) => string.Join(separator, KeyArray);

    /// <summary>
    /// 判斷當前 GramInPath 是否等於另一個 GramInPath。
    /// </summary>
    /// <param name="other">要比較的另一個 GramInPath 物件。</param>
    /// <returns>如果相等則返回 true，否則返回 false。</returns>
    public bool Equals(GramInPath other) {
      return Equals(Gram, other.Gram) && IsExplicit == other.IsExplicit;
    }

    /// <summary>
    /// 判斷當前 GramInPath 是否等於指定的物件。
    /// </summary>
    /// <param name="obj">要比較的物件。</param>
    /// <returns>如果相等則返回 true，否則返回 false。</returns>
    public override bool Equals(object? obj) => obj is GramInPath other && Equals(other);

    /// <summary>
    /// 獲取當前 GramInPath 的雜湊碼。
    /// </summary>
    /// <returns>雜湊碼值。</returns>
    public override int GetHashCode() {
      unchecked {
        int hash = 17;
        hash = hash * 23 + Gram.GetHashCode();
        hash = hash * 23 + IsExplicit.GetHashCode();
        return hash;
      }
    }

    /// <summary>
    /// 判斷兩個 GramInPath 是否相等。
    /// </summary>
    /// <param name="left">左側 GramInPath 物件。</param>
    /// <param name="right">右側 GramInPath 物件。</param>
    /// <returns>如果相等則返回 true，否則返回 false。</returns>
    public static bool operator ==(GramInPath left, GramInPath right) => left.Equals(right);

    /// <summary>
    /// 判斷兩個 GramInPath 是否不相等。
    /// </summary>
    /// <param name="left">左側 GramInPath 物件。</param>
    /// <param name="right">右側 GramInPath 物件。</param>
    /// <returns>如果不相等則返回 true，否則返回 false。</returns>
    public static bool operator !=(GramInPath left, GramInPath right) => !(left == right);
  }
}

// MARK: - Extensions for List<GramInPath>

namespace Megrez {
  /// <summary>
  /// 提供 GramInPath 陣列的擴展方法。
  /// </summary>
  public static class GramInPathArrayExtensions {
    /// <summary>
    /// 從一個節點陣列當中取出目前的選字字串陣列。
    /// </summary>
    public static List<string> Values(this List<GramInPath> grams) => grams.Select(g => g.Value).ToList();

    /// <summary>
    /// 從一個節點陣列當中取出目前的索引鍵陣列。
    /// </summary>
    public static List<string> JoinedKeys(this List<GramInPath> grams, string separator) =>
      grams.Select(g => string.Join(separator, g.KeyArray)).ToList();

    /// <summary>
    /// 從一個節點陣列當中取出目前的索引鍵陣列。
    /// </summary>
    public static List<List<string>> KeyArrays(this List<GramInPath> grams) =>
      grams.Select(g => g.KeyArray).ToList();

    /// <summary>
    /// 游標對映配對資料。
    /// </summary>
    public struct GramBorderPointDictPair {
      /// <summary>
      /// 區域到游標的對映表。
      /// </summary>
      public Dictionary<int, int> regionCursorMap;

      /// <summary>
      /// 游標到區域的對映表。
      /// </summary>
      public Dictionary<int, int> cursorRegionMap;
    }

    /// <summary>
    /// 返回一連串的節點起點。結果為 (Result A, Result B) 字典陣列。
    /// Result A 以索引查座標，Result B 以座標查索引。
    /// </summary>
    private static GramBorderPointDictPair
      GramBorderPointDictPairFor(this List<GramInPath> grams) {
      var resultA = new Dictionary<int, int>();
      var resultB = new Dictionary<int, int> { [-1] = 0 }; // 防呆
      int cursorCounter = 0;

      for (int gramCounter = 0; gramCounter < grams.Count; gramCounter++) {
        var gram = grams[gramCounter];
        resultA[gramCounter] = cursorCounter;
        foreach (var _ in gram.KeyArray) {
          resultB[cursorCounter] = gramCounter;
          cursorCounter += 1;
        }
      }

      resultA[grams.Count] = cursorCounter;
      resultB[cursorCounter] = grams.Count;
      return new GramBorderPointDictPair { regionCursorMap = resultA, cursorRegionMap = resultB };
    }

    /// <summary>
    /// 返回一個字典，以座標查索引。允許以游標位置查詢其屬於第幾個幅節座標（從 0 開始算）。
    /// </summary>
    public static Dictionary<int, int> CursorRegionMap(this List<GramInPath> grams) =>
      grams.GramBorderPointDictPairFor().cursorRegionMap;

    /// <summary>
    /// 總讀音單元數量。在絕大多數情況下，可視為總幅節長度。
    /// </summary>
    public static int TotalKeyCount(this List<GramInPath> grams) =>
      grams.Select(g => g.KeyArray.Count).Sum();

    /// <summary>
    /// 根據給定的游標，返回其前後最近的節點邊界。
    /// </summary>
    /// <param name="grams">節點陣列</param>
    /// <param name="cursor">給定的游標。</param>
    public static ClosedRange ContextRange(this List<GramInPath> grams, int cursor) {
      if (grams.IsEmpty()) return new ClosedRange(0, 0);

      int frontestSegLength = grams.Last().KeyArray.Count;
      var nilReturn = new ClosedRange(grams.TotalKeyCount() - frontestSegLength, grams.TotalKeyCount());

      if (cursor >= grams.TotalKeyCount()) return nilReturn; // 防呆
      cursor = Math.Max(0, cursor); // 防呆

      var mapPair = grams.GramBorderPointDictPairFor();

      if (!mapPair.cursorRegionMap.TryGetValue(cursor, out int rearNodeID)) return new ClosedRange(cursor, cursor);
      if (!mapPair.regionCursorMap.TryGetValue(rearNodeID, out int rearIndex)) return new ClosedRange(cursor, cursor);
      if (!mapPair.regionCursorMap.TryGetValue(rearNodeID + 1, out int frontIndex))
        return new ClosedRange(cursor, cursor);

      return new ClosedRange(rearIndex, frontIndex);
    }

    /// <summary>
    /// 查找結果資料。
    /// </summary>
    public struct FindGramResult {
      /// <summary>
      /// 找到的 GramInPath 節點。
      /// </summary>
      public GramInPath gram;

      /// <summary>
      /// 節點所在的範圍。
      /// </summary>
      public ClosedRange range;
    }

    /// <summary>
    /// 在陣列內以給定游標位置找出對應的節點。
    /// </summary>
    /// <param name="grams">節點陣列</param>
    /// <param name="cursor">給定游標位置。</param>
    /// <returns>查找結果。</returns>
    public static FindGramResult? FindGram(this List<GramInPath> grams, int cursor) {
      if (grams.IsEmpty()) return null;

      cursor = Math.Max(0, Math.Min(cursor, grams.TotalKeyCount() - 1)); // 防呆
      var range = grams.ContextRange(cursor);

      if (!grams.CursorRegionMap().TryGetValue(cursor, out int rearNodeID)) return null;
      if (grams.Count - 1 < rearNodeID) return null;

      return new FindGramResult { gram = grams[rearNodeID], range = range };
    }

    /// <summary>
    /// 偵測是否出現游標切斷組字區內字元的情況。
    /// </summary>
    /// <remarks>
    /// 此處不需要針對 cursor 做邊界檢查。
    /// </remarks>
    public static bool IsCursorCuttingChar(this List<GramInPath> grams, int cursor) {
      int index = cursor;
      bool isBound = (index == grams.ContextRange(index).Lowerbound);
      if (index == grams.TotalKeyCount()) isBound = true;

      var rawResult = grams.FindGram(index)?.gram.IsReadingMismatched ?? false;
      return !isBound && rawResult;
    }

    /// <summary>
    /// 偵測游標是否切斷區域。
    /// </summary>
    /// <param name="grams">節點陣列。</param>
    /// <param name="cursor">游標位置。</param>
    /// <returns>如果游標切斷區域則返回 true，否則返回 false。</returns>
    public static bool IsCursorCuttingRegion(this List<GramInPath> grams, int cursor) {
      int index = cursor;
      bool isBound = (index == grams.ContextRange(index).Lowerbound);
      if (index == grams.TotalKeyCount()) isBound = true;
      return !isBound;
    }

    /// <summary>
    /// 提供一組逐字的字音配對陣列（不使用 KeyValuePaired 類型），但字音不相符的節點除外。
    /// </summary>
    public static List<KeyValuePair<string, string>> SmashedPairs(this List<GramInPath> grams) {
      var arrData = new List<KeyValuePair<string, string>>();

      foreach (var gram in grams) {
        if (gram.IsReadingMismatched && !string.IsNullOrEmpty(string.Join("", gram.KeyArray))) {
          arrData.Add(new KeyValuePair<string, string>(string.Join("\t", gram.KeyArray), gram.Value));
          continue;
        }

        var arrValueChars = gram.Value.Select(c => c.ToString()).ToList();
        for (int i = 0; i < gram.KeyArray.Count; i++) {
          arrData.Add(new KeyValuePair<string, string>(gram.KeyArray[i], arrValueChars[i]));
        }
      }

      return arrData;
    }

    /// <summary>
    /// 感知結果資料。
    /// </summary>
    public struct PerceptionResult {
      /// <summary>
      /// N-gram 索引鍵。
      /// </summary>
      public string NGramKey;

      /// <summary>
      /// 候選詞。
      /// </summary>
      public string Candidate;

      /// <summary>
      /// 頭部讀音。
      /// </summary>
      public string HeadReading;
    }

    /// <summary>
    /// 生成用以洞察使用者覆寫行為的複元圖索引鍵，最多支援指定長度的 n-gram（預設 3-gram）。
    /// </summary>
    /// <remarks>
    /// 除非有專門指定游標，否則身為 List&lt;GramInPath&gt; 自身的
    /// 「陣列最尾端」（也就是打字方向上最前方）的那個 Gram 會被當成 Head。
    /// </remarks>
    /// <param name="grams">節點陣列。</param>
    /// <param name="cursor">指定用於當作 head 的游標位置；若為 null 則取陣列尾端。</param>
    /// <param name="maxContext">最多向前取用的上下文節點數（含 head），預設 3。</param>
    /// <returns>感知結果資料。</returns>
    public static PerceptionResult?
      GenerateKeyForPerception(this List<GramInPath> grams, int? cursor = null, int maxContext = 3) {
      if (maxContext <= 0) return null;

      int totalKeyCount = grams.TotalKeyCount();
      bool hasHeadInfo = false;
      GramInPath headPair = default;
      ClosedRange headRange = default;
      int resolvedCursor = 0;

      if (cursor.HasValue && cursor.Value >= 0 && cursor.Value < totalKeyCount) {
        var hit = grams.FindGram(cursor.Value);
        if (hit.HasValue) {
          headPair = hit.Value.gram;
          headRange = hit.Value.range;
          resolvedCursor = Math.Max(
            hit.Value.range.Lowerbound,
            Math.Min(cursor.Value, hit.Value.range.Upperbound - 1)
          );
          hasHeadInfo = true;
        }
      }

      if (!hasHeadInfo && grams.Count > 0) {
        headPair = grams[^1];
        int lowerBound = Math.Max(0, totalKeyCount - headPair.KeyArray.Count);
        headRange = new ClosedRange(lowerBound, totalKeyCount);
        resolvedCursor = Math.Max(lowerBound, totalKeyCount - 1);
        hasHeadInfo = true;
      }

      if (!hasHeadInfo) return null;

      int headKeyOffset = Math.Max(0, resolvedCursor - headRange.Lowerbound);
      if (headPair.KeyArray.Count <= headKeyOffset) return null;

      const string placeholder = "()";
      string readingSeparator = Compositor.TheSeparator;

      static bool IsPunctuation(GramInPath pair) {
        if (pair.KeyArray.Count == 0) return false;
        string firstReading = pair.KeyArray[0];
        return !string.IsNullOrEmpty(firstReading) && firstReading[0] == '_';
      }

      string? CombinedString(GramInPath pair) {
        if (string.IsNullOrEmpty(pair.Value)) return null;
        if (pair.KeyArray.Count == 0) return null;
        string reading = pair.JoinedCurrentKey(readingSeparator);
        if (string.IsNullOrEmpty(reading)) return null;
        return $"({reading},{pair.Value})";
      }

      int? headIndex = null;
      for (int idx = grams.Count - 1; idx >= 0; idx--) {
        if (grams[idx].Gram.Id == headPair.Gram.Id) {
          headIndex = idx;
          break;
        }
      }

      if (!headIndex.HasValue) {
        for (int idx = grams.Count - 1; idx >= 0; idx--) {
          if (grams[idx].Equals(headPair)) {
            headIndex = idx;
            break;
          }
        }
      }

      if (!headIndex.HasValue) return null;

      string[] resultCells = Enumerable.Repeat(placeholder, maxContext).ToArray();
      resultCells[^1] = CombinedString(headPair) ?? placeholder;

      int contextSlot = maxContext - 2;
      int currentIndex = headIndex.Value - 1;
      bool encounteredPunctuation = false;

      while (contextSlot >= 0) {
        if (currentIndex < 0) {
          resultCells[contextSlot] = placeholder;
          contextSlot--;
          continue;
        }

        var currentPair = grams[currentIndex];
        currentIndex--;

        if (encounteredPunctuation || IsPunctuation(currentPair)) {
          encounteredPunctuation = true;
          resultCells[contextSlot] = placeholder;
          contextSlot--;
          continue;
        }

        resultCells[contextSlot] = CombinedString(currentPair) ?? placeholder;
        contextSlot--;
      }

      string headReading = headPair.KeyArray[headKeyOffset];
      return new PerceptionResult {
        NGramKey = string.Join("&", resultCells),
        Candidate = headPair.Value,
        HeadReading = headReading
      };
    }
  }
}
