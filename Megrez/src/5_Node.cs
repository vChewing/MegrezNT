// (c) 2022 and onwards The vChewing Project (LGPL v3.0 License or later).
// ====================
// This code is released under the SPDX-License-Identifier: `LGPL-3.0-or-later`.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Megrez {
  /// <summary>
  /// 組字引擎中的基礎單位節點物件。<para/>
  /// 節點封裝了讀音索引鍵陣列、涵蓋範圍長度、以及對應的單元圖資料。節點的涵蓋長度
  /// 表示該節點在整個讀音序列中佔據的位置數量。組字引擎會根據輸入的讀音序列自動建立
  /// 節點結構。對於多字詞彙，引擎會將相應的多個讀音組合並生成單一的複合索引鍵，
  /// 然後從語言模型中獲取匹配的單元圖集合。例如，一個包含兩個漢字的詞彙對應兩個讀音，
  /// 其組合後的節點涵蓋長度即為 2。
  /// </summary>
  public partial class Node {
    // MARK: - Enums

    /// <summary>
    /// 節點資料覆寫模式的定義。
    /// </summary>
    public enum OverrideType {
      /// <summary>
      /// 預設模式，無任何覆寫操作。
      /// </summary>
      NoOverrides = 0,
      /// <summary>
      /// 採用指定的單元圖詞彙內容進行覆寫，但保留最高權重單元圖的分數值。
      /// 例如，若節點包含單元圖序列 [("甲", -114), ("乙", -514), ("丙", -1919)]，
      /// 選擇覆寫為「丙」時，實際返回結果為 ("丙", -114)。此模式主要應用於
      /// 使用者記憶模組的輔助建議功能。經過覆寫的節點狀態將維持穩定，不會被
      /// 組句演算法自動還原。然而，此模式無法完全阻止其他節點在組句過程中
      /// 產生的影響。如需完全控制，應配合使用 <see cref="OverridingScore"/> 屬性。
      /// </summary>
      TopUnigramScore = 1,
      /// <summary>
      /// 將節點的權重強制設定為 <see cref="OverridingScore"/> 數值，確保組句演算法優先選擇該節點。
      /// </summary>
      HighScore = 2
    }

    // MARK: - Variables

    /// <summary>
    /// 專門用於權重覆寫的高數值常數。此數值的設定足以影響組句演算法的路徑選擇結果。
    /// 雖然理論上使用「0」似乎已經足夠，但實際上可能導致覆寫狀態被組句演算法忽略。
    /// 舉例說明：假設要對讀音序列「a b c」覆寫為「甲 乙 丙」，其中甲乙丙為大寫形式的
    /// 覆寫內容。如果單獨的「c」存在可競爭的詞彙「bc」，可能導致組句演算法計算出
    /// 「甲->bc」的路徑（特別是當甲和乙使用「0」作為覆寫分數時）。在這種情況下，
    /// 「甲-乙」的路徑不一定能獲得演算法的青睞。因此，此處必須使用大於 0 的正數
    /// （例如此處的特殊常數），以確保「丙」能夠單獨被優先選中。
    /// </summary>
    public double OverridingScore = 114514;

    /// <summary>
    /// 讀音索引鍵序列。
    /// </summary>
    public List<string> KeyArray { get; }
    /// <summary>
    /// 節點的涵蓋範圍長度。
    /// </summary>
    public int SegLength { get; }
    /// <summary>
    /// 節點包含的單元圖資料集合。
    /// </summary>
    public List<Unigram> Unigrams { get; private set; }
    /// <summary>
    /// 目前應用於該節點的覆寫模式類型。
    /// </summary>
    public OverrideType CurrentOverrideType { get; private set; }

    private int _currentUnigramIndex;
    /// <summary>
    /// 指向單元圖集合中當前選定項目的索引值。
    /// </summary>
    public int CurrentUnigramIndex {
      get => _currentUnigramIndex;
      set {
        int corrected = Math.Max(Math.Min(Unigrams.Count - 1, value), 0);
        _currentUnigramIndex = corrected;
      }
    }

    // MARK: - Constructor and Other Fundamentals

    /// <summary>
    /// 建立新的節點實例。<para/>
    /// 節點物件整合了讀音索引鍵序列、涵蓋範圍長度、以及相關的單元圖資料。範圍長度
    /// 表示此節點在輸入序列中所佔的位置數量。組字引擎負責根據語言模型資料動態建構
    /// 節點物件。對於包含多個字符的詞條，引擎會整合對應的多個讀音形成複合索引鍵，
    /// 並據此從語言模型中查詢匹配的單元圖集合。舉例而言，雙字詞對應雙讀音，
    /// 其節點的涵蓋範圍長度為 2。
    /// </summary>
    /// <param name="keyArray">輸入的索引鍵序列，不可為空集合。</param>
    /// <param name="segLength">節點涵蓋範圍長度，通常與索引鍵序列元素數量相等。</param>
    /// <param name="unigrams">關聯的單元圖資料集合，不可為空集合。</param>
    public Node(List<string> keyArray, int segLength, List<Unigram> unigrams) {
      _currentUnigramIndex = 0;
      KeyArray = keyArray;
      SegLength = Math.Max(0, segLength);
      Unigrams = unigrams;
      CurrentOverrideType = OverrideType.NoOverrides;
    }

    /// <summary>
    /// 通過複製現有節點來建立新實例。
    /// </summary>
    /// <remarks>
    /// 由於 Node 採用參考型別設計，在組字器複製過程中無法自動執行深層複製。
    /// 這可能導致複製後的組字器中的節點變更影響到原始組字器實例。
    /// 為避免此類非預期的交互影響，特別提供此複製建構函數。
    /// </remarks>
    /// <param name="node">要複製的來源節點實例。</param>
    public Node(Node node) {
      OverridingScore = node.OverridingScore;
      KeyArray = node.KeyArray.ToList();
      SegLength = node.SegLength;
      Unigrams = node.Unigrams.ToList();
      CurrentOverrideType = node.CurrentOverrideType;
      CurrentUnigramIndex = node.CurrentUnigramIndex;
    }

    /// <summary>
    /// 生成自身的拷貝。
    /// </summary>
    /// <remarks>
    /// 因為 Node 不是 Struct，所以會在 Compositor 被拷貝的時候無法被真實複製。
    /// 這樣一來，Compositor 複製品當中的 Node 的變化會被反應到原先的 Compositor 身上。
    /// 這在某些情況下會造成意料之外的混亂情況，所以需要引入一個拷貝用的建構子。
    /// </remarks>
    /// <returns>拷貝。</returns>
    public Node Copy() => new(node: this);

    /// <summary>
    ///
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object obj) {
      return obj is not Node node
                 ? false
                 : OverridingScore == node.OverridingScore && KeyArray.SequenceEqual(node.KeyArray) &&
                       SegLength == node.SegLength && Unigrams.SequenceEqual(node.Unigrams) &&
                       CurrentOverrideType == node.CurrentOverrideType && CurrentUnigramIndex == node.CurrentUnigramIndex;
    }

    /// <summary>
    /// 做為預設雜湊函式。
    /// </summary>
    /// <returns>目前物件的雜湊碼。</returns>
    public override int GetHashCode() {
      unchecked {
        int hash = 17;
        hash = hash * 23 + OverridingScore.GetHashCode();
        hash = hash * 23 + KeyArray.GetHashCode();
        hash = hash * 23 + SegLength.GetHashCode();
        hash = hash * 23 + Unigrams.GetHashCode();
        hash = hash * 23 + CurrentUnigramIndex.GetHashCode();
        hash = hash * 23 + CurrentOverrideType.GetHashCode();
        return hash;
      }
    }

    // MARK: - Dynamic Variables

    /// <summary>
    /// 該節點當前狀態所展示的鍵值配對。
    /// </summary>
    public KeyValuePaired CurrentPair => new(KeyArray, Value);

    /// <summary>
    /// 給出該節點內部單元圖陣列內目前被索引位置所指向的單元圖。
    /// </summary>
    public Unigram CurrentUnigram => Unigrams.IsEmpty() ? new() : Unigrams[CurrentUnigramIndex];

    /// <summary>
    /// 給出該節點內部單元圖陣列內目前被索引位置所指向的單元圖的資料值。
    /// </summary>
    public string Value => CurrentUnigram.Value;

    /// <summary>
    /// 給出目前的最高權重單元圖當中的權重值。該結果可能會受節點覆寫狀態所影響。
    /// </summary>
    public double Score {
      get {
        return Unigrams.IsEmpty() ? 0
                                  : CurrentOverrideType switch {
                                    OverrideType.HighScore => OverridingScore,
                                    OverrideType.TopUnigramScore => Unigrams.First().Score,
                                    _ => CurrentUnigram.Score
                                  };
      }
    }

    /// <summary>
    /// 檢查當前節點是否「讀音字長與候選字字長不一致」。
    /// </summary>
    public bool IsReadingMismatched => KeyArray.Count != Value.LiteralCount();
    /// <summary>
    /// 該節點是否處於被覆寫的狀態。
    /// </summary>
    public bool IsOverridden => CurrentOverrideType != OverrideType.NoOverrides;

    // MARK: - Methods and Functions

    /// <summary>
    /// 將索引鍵按照給定的分隔符銜接成一個字串。
    /// </summary>
    /// <param name="separator">給定的分隔符，預設值為 <see cref="Compositor.TheSeparator"/>。</param>
    /// <returns>已經銜接完畢的字串。</returns>
    public string JoinedKey(string? separator = null) =>
        StringJoinCache.Shared.GetCachedJoin(KeyArray, separator ?? Compositor.TheSeparator);

    /// <summary>
    /// 重設該節點的覆寫狀態、及其內部的單元圖索引位置指向。
    /// </summary>
    public void Reset() {
      _currentUnigramIndex = 0;
      CurrentOverrideType = OverrideType.NoOverrides;
    }

    /// <summary>
    /// 置換掉該節點內的單元圖陣列資料。
    /// 如果此時影響到了 currentUnigramIndex 所指的內容的話，則將其重設為 0。
    /// </summary>
    /// <param name="source">新的單元圖陣列資料，必須不能為空（否則必定崩潰）。</param>
    public void SyncingUnigramsFrom(List<Unigram> source) {
      string oldCurrentValue = Unigrams[CurrentUnigramIndex].Value;
      Unigrams = source;
      CurrentUnigramIndex = _currentUnigramIndex;  // 自動觸發 didSet() 的糾錯過程。
      string newCurrentValue = Unigrams[CurrentUnigramIndex].Value;
      if (oldCurrentValue != newCurrentValue) Reset();
    }

    /// <summary>
    /// 指定要覆寫的單元圖資料值、以及覆寫行為種類。
    /// </summary>
    /// <param name="value">給定的單元圖資料值。</param>
    /// <param name="type">覆寫行為種類。</param>
    /// <returns>操作是否順利完成。</returns>
    public bool SelectOverrideUnigram(string value, OverrideType type) {
      if (type == OverrideType.NoOverrides) return false;
      foreach (EnumeratedItem<Unigram> pair in Unigrams.Enumerated()) {
        int i = pair.Offset;
        Unigram gram = pair.Value;
        if (value != gram.Value) continue;
        CurrentUnigramIndex = i;
        CurrentOverrideType = type;
        return true;
      }
      return false;
    }
  }

  // MARK: - [Node] Implementations.

  /// <summary>
  /// 針對節點陣列的功能擴充。
  /// </summary>
  public static class NodeExtensions {
    /// <summary>
    /// 從一個節點陣列當中取出目前的索引鍵陣列。
    /// </summary>
    /// <param name="self">節點。</param>
    /// <returns>目前的索引鍵陣列。</returns>
    public static List<List<string>> KeyArray(this List<Node> self) => self.Select(x => x.KeyArray).ToList();
    /// <summary>
    /// 從一個節點陣列當中取出目前的選字字串陣列。
    /// </summary>
    /// <param name="self">節點。</param>
    /// <returns>目前的自動選字字串陣列。</returns>
    public static List<string> Values(this List<Node> self) => self.Select(x => x.Value).ToList();
    /// <summary>
    /// 從一個節點陣列當中取出目前的索引鍵陣列。
    /// </summary>
    /// <param name="self">節點。</param>
    /// <param name="separator"></param>
    /// <returns>取出目前的索引鍵陣列。</returns>
    public static List<string> JoinedKeys(this List<Node> self, string? separator) =>
        self.Select(x => x.KeyArray.Joined(separator ?? Compositor.TheSeparator)).ToList();
    /// <summary>
    /// 總讀音單元數量。在絕大多數情況下，可視為總幅節長度。
    /// </summary>
    /// <param name="self">節點。</param>
    /// <returns>總讀音單元數量。</returns>
    public static int TotalKeyCount(this List<Node> self) => self.Select(x => x.KeyArray.Count).Sum();

    /// <summary>
    /// (Result A, Result B) 字典陣列。Result A 以索引查座標，Result B 以座標查索引。
    /// </summary>
    public struct CursorMapPair {
      /// <summary>
      /// 以索引查座標的字典陣列。
      /// </summary>
      public Dictionary<int, int> RegionCursorMap { get; private set; }
      /// <summary>
      /// 以座標查索引的字典陣列。
      /// </summary>
      public Dictionary<int, int> CursorRegionMap { get; private set; }
      /// <summary>
      /// (Result A, Result B) 字典陣列。Result A 以索引查座標，Result B 以座標查索引。
      /// </summary>
      /// <param name="regionCursorMap">以索引查座標的字典陣列。</param>
      /// <param name="cursorRegionMap">以座標查索引的字典陣列。</param>
      public CursorMapPair(Dictionary<int, int> regionCursorMap, Dictionary<int, int> cursorRegionMap) {
        RegionCursorMap = regionCursorMap;
        CursorRegionMap = cursorRegionMap;
      }
    }

    /// <summary>
    /// 返回一連串的節點起點。結果為 (Result A, Result B) 字典陣列。
    /// Result A 以索引查座標，Result B 以座標查索引。
    /// </summary>
    /// <param name="self">節點。</param>
    /// <returns> (Result A, Result B) 字典陣列。Result A 以索引查座標，Result B 以座標查索引。</returns>
    private static CursorMapPair NodeBorderPointDictPair(this List<Node> self) {
      Dictionary<int, int> resultA = new();
      Dictionary<int, int> resultB = new() { [-1] = 0 };  // 防呆
      int cursorCounter = 0;
      foreach (EnumeratedItem<Node> pair in self.Enumerated()) {
        int nodeCounter = pair.Offset;
        Node neta = pair.Value;
        resultA[nodeCounter] = cursorCounter;
        foreach (string _ in neta.KeyArray) {
          resultB[cursorCounter] = nodeCounter;
          cursorCounter += 1;
        }
      }
      resultA[self.Count] = cursorCounter;
      resultB[cursorCounter] = self.Count;
      return new(resultA, resultB);
    }

    /// <summary>
    /// 返回一個字典，以座標查索引。允許以游標位置查詢其屬於第幾個幅節座標（從 0 開始算）。
    /// </summary>
    /// <param name="self">節點。</param>
    /// <returns>一個字典，以座標查索引。允許以游標位置查詢其屬於第幾個幅節座標（從 0 開始算）。</returns>
    public static Dictionary<int, int> CursorRegionMap(this List<Node> self) =>
        self.NodeBorderPointDictPair().CursorRegionMap;

    /// <summary>
    /// 根據給定的游標，返回其前後最近的節點邊界。
    /// </summary>
    /// <param name="self">節點。</param>
    /// <param name="givenCursor">給定的游標。</param>
    /// <returns>前後最近的邊界點。</returns>
    public static BRange ContextRange(this List<Node> self, int givenCursor) {
      if (self.IsEmpty()) return new(0, 0);
      int lastSegLength = self.Last().KeyArray.Count;
      int totalKeyCount = self.TotalKeyCount();
      BRange nilReturn = new(totalKeyCount - lastSegLength, totalKeyCount);
      if (givenCursor >= totalKeyCount) return nilReturn;
      int cursor = Math.Max(0, givenCursor);  // 防呆。
      nilReturn = new(cursor, cursor);
      CursorMapPair dictPair = self.NodeBorderPointDictPair();
      // 下文按道理來講不應該會出現 nilReturn。
      if (!dictPair.CursorRegionMap.TryGetValue(cursor, out int rearNodeID)) return nilReturn;
      if (!dictPair.RegionCursorMap.TryGetValue(rearNodeID, out int rearIndex)) return nilReturn;
      return !dictPair.RegionCursorMap.TryGetValue(rearNodeID + 1, out int frontIndex) ? nilReturn
                                                                                       : new(rearIndex, frontIndex);
    }
    /// <summary>
    /// 在陣列內以給定游標位置找出對應的節點。
    /// </summary>
    /// <param name="self">節點。</param>
    /// <param name="givenCursor">給定游標位置。</param>
    /// <param name="outCursorPastNode">找出的節點的前端位置。</param>
    /// <returns>查找結果。</returns>
    public static Node? FindNodeAt(this List<Node> self, int givenCursor, ref int outCursorPastNode) {
      if (self.IsEmpty()) return null;
      int cursor = Math.Max(0, Math.Min(givenCursor, self.TotalKeyCount() - 1));
      BRange range = self.ContextRange(givenCursor: cursor);
      outCursorPastNode = range.Upperbound;
      CursorMapPair dictPair = self.NodeBorderPointDictPair();
      return !dictPair.CursorRegionMap.TryGetValue(cursor + 1, out int rearNodeID) ? null
             : self.Count - 1 >= rearNodeID ? self[rearNodeID]
                                                                                   : null;
    }
    /// <summary>
    /// 在陣列內以給定游標位置找出對應的節點。
    /// </summary>
    /// <param name="self">節點。</param>
    /// <param name="givenCursor">給定游標位置。</param>
    /// <returns>查找結果。</returns>
    public static Node? FindNodeAt(this List<Node> self, int givenCursor) {
      int mudamuda = 0;  // muda = useless.
      return self.FindNodeAt(givenCursor, ref mudamuda);
    }

    /// <summary>
    /// 提供一組逐字的字音配對陣列（不使用 Megrez 的 KeyValuePaired 類型），但字音不匹配的節點除外。
    /// </summary>
    /// <param name="self">節點。</param>
    /// <returns>一組逐字的字音配對陣列。</returns>
    public static List<KeyValuePair<string, string>> SmashedPairs(this List<Node> self) {
      List<KeyValuePair<string, string>> arrData = new();

      foreach (Node node in self) {
        if (node.IsReadingMismatched && !string.IsNullOrEmpty(node.KeyArray.Joined())) {
          arrData.Add(new(node.KeyArray.Joined("\t"), node.Value));
          continue;
        }
        List<string> arrValueChars = node.Value.LiteralCharComponents();
        foreach (EnumeratedItem<string> pair in node.KeyArray.Enumerated()) {
          arrData.Add(new(pair.Value, arrValueChars[pair.Offset]));
        }
      }
      return arrData;
    }
  }
}  // namespace Megrez
