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
  public class Node {
    // MARK: - Enums

    /// <summary>
    /// 節點資料覆寫模式的定義。
    /// </summary>
    public enum OverrideType {
      /// <summary>
      /// 專用於雙元圖的自動選取功能，但效力低於 withSpecified 模式。
      /// 該覆寫行為無法防止其它節點被組句函式所支配。
      /// 這種情況下就需要用到 <see cref="OverridingScore"/> 屬性。
      /// </summary>
      TopUnigramScore = 1,

      /// <summary>
      /// 強制覆寫節點權重為 <see cref="OverridingScore"/>
      /// 確保組句函式優先選擇該節點且不受其他節點影響。
      /// 但是這個選項也可以允許搭配過低的 overridingScore 來起到 demote 的效果。
      /// </summary>
      Specified = 2
    }

    // MARK: - Variables

    /// <summary>
    /// 節點的唯一識別符。
    /// </summary>
    public Guid Id { get; }

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
    /// 目前應用於該節點的覆寫模式類型。為 <c>null</c> 時表示無覆寫行為。
    /// </summary>
    public OverrideType? CurrentOverrideType { get; private set; }

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
    /// 建立新的節點副本。<para/>
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
      Id = Guid.NewGuid();
      _currentUnigramIndex = 0;
      KeyArray = keyArray;
      SegLength = Math.Max(0, segLength);
      Unigrams = unigrams;
      CurrentOverrideType = null;
    }

    /// <summary>
    /// 通過複製現有節點來建立新副本。
    /// </summary>
    /// <remarks>
    /// 由於 Node 採用參考型別設計，在組字器複製過程中無法自動執行深層複製。
    /// 這可能導致複製後的組字器中的節點變更影響到原始組字器副本。
    /// 為避免此類非預期的交互影響，特別提供此複製建構函數。
    /// </remarks>
    /// <param name="node">要複製的來源節點副本。</param>
    public Node(Node node) {
      Id = Guid.NewGuid();
      OverridingScore = node.OverridingScore;
      KeyArray = node.KeyArray.ToList();
      SegLength = node.SegLength;
      Unigrams = node.Unigrams.Select(x => x.Copy()).ToList();
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
    /// 基於功能內容的等價性比較，排除 ID 字段
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj) {
      return obj is Node node && Math.Abs(OverridingScore - node.OverridingScore) < 0.000_000_000_001 &&
             KeyArray.SequenceEqual(node.KeyArray) &&
             SegLength == node.SegLength && Unigrams.SequenceEqual(node.Unigrams) &&
             CurrentOverrideType == node.CurrentOverrideType && CurrentUnigramIndex == node.CurrentUnigramIndex;
    }

    /// <summary>
    /// 做為預設雜湊函式。
    /// </summary>
    /// <returns>目前物件的雜湊碼。</returns>
    public override int GetHashCode() {
      // ReSharper disable NonReadonlyFieldInGetHashCode
      // ReSharper disable NonReadonlyMemberInGetHashCode
      HashCode hash = new();
      hash.Add(Math.Round(OverridingScore, 12));
      hash.Add(SegLength);
      hash.Add(CurrentUnigramIndex);
      hash.Add(CurrentOverrideType);

      foreach (string key in KeyArray) {
        hash.Add(key);
      }

      foreach (Unigram unigram in Unigrams) {
        hash.Add(unigram);
      }

      return hash.ToHashCode();
      // ReSharper enable NonReadonlyFieldInGetHashCode
      // ReSharper enable NonReadonlyMemberInGetHashCode
    }

    // MARK: - Dynamic Variables

    /// <summary>
    /// 該節點當前狀態所展示的鍵值配對。
    /// </summary>
    public KeyValuePaired CurrentPair => new(KeyArray, Value);

    /// <summary>
    /// 給出該節點內部單元圖陣列內目前被索引位置所指向的單元圖。
    /// </summary>
    public Unigram CurrentUnigram => Unigrams.IsEmpty() ? Unigram.Empty : Unigrams[CurrentUnigramIndex];

    /// <summary>
    /// 給出該節點內部單元圖陣列內目前被索引位置所指向的單元圖的資料值。
    /// </summary>
    public string Value => CurrentUnigram.Value;

    /// <summary>
    /// 給出目前的最高權重單元圖當中的權重值。該結果可能會受節點覆寫狀態所影響。
    /// </summary>
    public double Score {
      get {
        return Unigrams.IsEmpty()
          ? 0
          : CurrentOverrideType switch {
            OverrideType.Specified => OverridingScore,
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
    public bool IsOverridden => CurrentOverrideType.HasValue;

    /// <summary>
    /// 節點覆寫狀態的動態屬性，允許直接讀取和設定覆寫狀態。
    /// </summary>
    public NodeOverrideStatus OverrideStatus {
      get => new(OverridingScore, CurrentOverrideType, CurrentUnigramIndex);
      set {
        OverridingScore = value.OverridingScore;
        // 防範 UnigramIndex 溢出，如果溢出則重設覆寫狀態
        if (value.CurrentUnigramIndex >= 0 && value.CurrentUnigramIndex < Unigrams.Count) {
          CurrentOverrideType = value.CurrentOverrideType;
          CurrentUnigramIndex = value.CurrentUnigramIndex;
        } else {
          Reset();
        }
      }
    }

    // MARK: - Methods and Functions

    /// <summary>
    /// 將索引鍵按照給定的分隔符銜接成一個字串。
    /// </summary>
    /// <param name="separator">給定的分隔符，預設值為 <see cref="Compositor.TheSeparator"/>。</param>
    /// <returns>已經銜接完畢的字串。</returns>
    public string JoinedKey(string? separator = null) =>
      string.Join(separator ?? Compositor.TheSeparator, KeyArray);

    /// <summary>
    /// 重設該節點的覆寫狀態、及其內部的單元圖索引位置指向。
    /// </summary>
    public void Reset() {
      _currentUnigramIndex = 0;
      CurrentOverrideType = null;
    }

    /// <summary>
    /// 置換掉該節點內的單元圖陣列資料。
    /// 如果此時影響到了 currentUnigramIndex 所指的內容的話，則將其重設為 0。
    /// </summary>
    /// <param name="source">新的單元圖陣列資料，必須不能為空（否則必定崩潰）。</param>
    public void SyncingUnigramsFrom(List<Unigram> source) {
      string oldCurrentValue = Unigrams[CurrentUnigramIndex].Value;
      Unigrams = source;
      CurrentUnigramIndex = _currentUnigramIndex; // 自動觸發 didSet() 的糾錯過程。
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
      foreach (EnumeratedItem<Unigram> pair in Unigrams.Enumerated()) {
        int i = pair.Offset;
        Unigram gram = pair.Value;
        if (value != gram.Value) continue;
        CurrentUnigramIndex = i;
        CurrentOverrideType = type;
        if (OverridingScore < 114_514) OverridingScore = 114_514;
        return true;
      }

      return false;
    }
  }

  /// <summary>
  /// 節點覆寫狀態封裝結構，用於記錄 Node 的覆寫相關狀態。
  /// 這個結構體允許輕量級地複製和恢復節點狀態，避免完整複製整個 Compositor。
  /// </summary>
  public struct NodeOverrideStatus : IEquatable<NodeOverrideStatus> {
    /// <summary>
    /// 覆寫權重數值
    /// </summary>
    public double OverridingScore { get; set; }

    /// <summary>
    /// 當前覆寫狀態種類
    /// </summary>
    public Node.OverrideType? CurrentOverrideType { get; set; }

    /// <summary>
    /// 當前單元圖索引位置
    /// </summary>
    public int CurrentUnigramIndex { get; set; }

    /// <summary>
    /// 初始化一個節點覆寫狀態
    /// </summary>
    /// <param name="overridingScore">覆寫權重數值</param>
    /// <param name="currentOverrideType">當前覆寫狀態種類</param>
    /// <param name="currentUnigramIndex">當前單元圖索引位置</param>
    public NodeOverrideStatus(
      double overridingScore = 114514,
      Node.OverrideType? currentOverrideType = null,
      int currentUnigramIndex = 0
    ) {
      OverridingScore = overridingScore;
      CurrentOverrideType = currentOverrideType;
      CurrentUnigramIndex = currentUnigramIndex;
    }

    /// <summary>
    /// 判斷指定的 NodeOverrideStatus 是否等同於目前的 NodeOverrideStatus。
    /// </summary>
    /// <param name="other">要與目前 NodeOverrideStatus 比較的目標。</param>
    /// <returns>若兩者相同則為 true，否則為 false。</returns>
    public bool Equals(NodeOverrideStatus other) {
      return OverridingScore.Equals(other.OverridingScore) &&
             CurrentOverrideType == other.CurrentOverrideType &&
             CurrentUnigramIndex == other.CurrentUnigramIndex;
    }

    /// <summary>
    /// 判斷指定物件是否等同於目前的 NodeOverrideStatus。
    /// </summary>
    /// <param name="obj">要與目前 NodeOverrideStatus 比較的物件。</param>
    /// <returns>若兩者相同則為 true，否則為 false。</returns>
    public override bool Equals(object? obj) {
      return obj is NodeOverrideStatus other && Equals(other);
    }

    /// <summary>
    /// 傳回此 NodeOverrideStatus 的雜湊碼。
    /// </summary>
    /// <returns>32 位元帶正負號的整數雜湊值。</returns>
    // ReSharper disable once NonReadonlyMemberInGetHashCode
    public override int GetHashCode() {
      unchecked {
        int hash = 17;
        hash = hash * 23 + OverridingScore.GetHashCode();
        hash = hash * 23 + CurrentOverrideType.GetHashCode();
        hash = hash * 23 + CurrentUnigramIndex.GetHashCode();
        return hash;
      }
    }

    /// <summary>
    /// 判斷兩個 NodeOverrideStatus 實例是否相等。
    /// </summary>
    /// <param name="left">第一個待比較的 NodeOverrideStatus。</param>
    /// <param name="right">第二個待比較的 NodeOverrideStatus。</param>
    /// <returns>若兩個實例相等則為 true，否則為 false。</returns>
    public static bool operator ==(NodeOverrideStatus left, NodeOverrideStatus right) => left.Equals(right);

    /// <summary>
    /// 判斷兩個 NodeOverrideStatus 實例是否不相等。
    /// </summary>
    /// <param name="left">第一個待比較的 NodeOverrideStatus。</param>
    /// <param name="right">第二個待比較的 NodeOverrideStatus。</param>
    /// <returns>若兩個實例不相等則為 true，否則為 false。</returns>
    public static bool operator !=(NodeOverrideStatus left, NodeOverrideStatus right) => !left.Equals(right);
  }
} // namespace Megrez
