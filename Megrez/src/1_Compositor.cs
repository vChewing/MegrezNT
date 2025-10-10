// (c) 2022 and onwards The vChewing Project (LGPL v3.0 License or later).
// ====================
// This code is released under the SPDX-License-Identifier: `LGPL-3.0-or-later`.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// 天權星NT引擎（MegrezNT Compositor）核心特色：
// - 純 C# 原生實作，完整支援 .NET 6+ 平台環境。
// - API 設計採用陣列導向的索引鍵處理機制，並且在詞彙候選產生階段能夠有效排除跨越游標位置的詞彙項目。
// - 組句算法（Assembling Algorithm）採用 DAG-DP 動態規劃算法，且經過效能最佳化處理、擁有比 DAG-Relax 算法更優的效能。

// 核心概念術語定義：

// Grid: 軌格系統
// Assemble: 文字組句
// Node: 資料節點
// SegLength: 涵蓋範圍
// Segment: 區段單元

namespace Megrez {
  /// <summary>
  /// 智慧組字引擎的核心處理器，專門負責將輸入的索引鍵序列轉換為對應的資料值集合。<para/>
  /// 在輸入法應用場景中，此引擎接收注音符號序列作為索引鍵，並輸出相應的中文詞彙組合。
  /// 同時也支援文章分析功能：以中文字符作為輸入索引鍵，產生語義分析後的詞彙分段結果。
  /// </summary>
  /// <remarks>
  /// 儘管採用了隱馬可夫模型（HMM）的相關術語，但實際的組句過程運用的是更為直接的
  /// 貝氏推論機制：這是因為底層語言模型僅提供一元語法資料。當所有可能的組字單元以節點
  /// 形式載入引擎後，即可透過簡化的有向無環圖處理流程，運用這些隱含的資料值
  /// 計算出最大似然估計的最終結果。
  /// </remarks>
  public partial class Compositor {
    // MARK: - Enums.
    /// <summary>
    /// 基於文字書寫習慣的方向性定義。
    /// </summary>
    public enum TypingDirection {
      /// <summary>
      /// 相對於文字書寫方向的前端位置。
      /// </summary>
      ToFront,

      /// <summary>
      /// 相對於文字書寫方向的後端位置。
      /// </summary>
      ToRear
    }

    /// <summary>
    /// 軌格增減行為。
    /// </summary>
    public enum ResizeBehavior {
      /// <summary>
      /// 軌格增減行為：增加。
      /// </summary>
      Expand,

      /// <summary>
      /// 軌格增減行為：減少。
      /// </summary>
      Shrink
    }

    // MARK: - Variables.

    /// <summary>
    /// 組字器的組態設定。
    /// </summary>
    public CompositorConfig Config;

    /// <summary>
    /// 一個幅節單元內所能接受的最長的節點幅節長度。
    /// </summary>
    public int MaxSegLength {
      get => Config.MaxSegLength;
      set => Config.MaxSegLength = value;
    }

    /// <summary>
    /// 公用變數，在生成索引鍵字串時用來分割每個索引鍵單位。最好是鍵盤無法直接敲的 ASCII 字元。
    /// </summary>
    /// <remarks>
    /// 每次初期化一個新的組字器的時候，該公用變數都會被覆寫。
    /// </remarks>
    public static string TheSeparator = "-";

    /// <summary>
    /// 在生成索引鍵字串時用來分割每個索引鍵單位。最好是鍵盤無法直接敲的 ASCII 字元。
    /// /// </summary>
    /// <remarks>
    /// 更新該內容會同步更新 <see cref="TheSeparator"/>；每次初期化一個新的組字器的時候都會覆寫之。
    /// </remarks>
    public string Separator {
      get => Config.Separator;
      set => Config.Separator = value;
    }

    /// <summary>
    /// 該組字器的敲字游標位置。
    /// </summary>
    public int Cursor {
      get => Config.Cursor;
      set => Config.Cursor = value;
    }

    /// <summary>
    /// 該組字器的標記器（副游標）位置。
    /// </summary>
    public int Marker {
      get => Config.Marker;
      set => Config.Marker = value;
    }

    /// <summary>
    /// 公開：該組字器的長度。<para/>
    /// 組字器內已經插入的單筆索引鍵的數量，也就是內建漢字讀音的數量（唯讀）。
    /// </summary>
    /// <remarks>
    /// 理論上而言，segments.count 也是這個數。
    /// 但是，為了防止萬一，就用了目前的方法來計算。
    /// </remarks>
    public int Length => Keys.Count;

    /// <summary>
    /// 最近一次組句結果。
    /// </summary>
    public List<GramInPath> AssembledSentence {
      get => Config.AssembledSentence;
      set => Config.AssembledSentence = value;
    }

    /// <summary>
    /// 組字器是否為空。
    /// </summary>
    public bool IsEmpty => Segments.IsEmpty() && Keys.IsEmpty();

    /// <summary>
    /// 該組字器已經插入的的索引鍵，以陣列的形式存放。
    /// </summary>
    public List<string> Keys {
      get => Config.Keys;
      set => Config.Keys = value;
    }

    /// <summary>
    /// 該組字器的幅節單元陣列。
    /// </summary>
    public List<Segment> Segments {
      get => Config.Segments;
      set => Config.Segments = value;
    }

    private LangModelProtocol _theLangModel;

    /// <summary>
    /// 該組字器所使用的語言模型（被 LangModelRanked 所封裝）。
    /// </summary>
    public LangModelProtocol TheLangModel {
      get => _theLangModel;
      set {
        _theLangModel = value;
        Clear();
      }
    }

    /// <summary>
    /// 初期化一個組字器。<para/>
    /// 一個組字器用來在給定一系列的索引鍵的情況下（藉由一系列的觀測行為）返回一套資料值。
    /// 用於輸入法的話，給定的索引鍵可以是注音、且返回的資料值都是漢語字詞組合。該組字器
    /// 還可以用來對文章做分節處理：此時的索引鍵為漢字，返回的資料值則是漢語字詞分節組合。
    /// </summary>
    /// <remarks>
    /// 雖然這裡用了隱性 Markov 模型（HMM）的術語，但實際上在組句時用到的則是更
    /// 簡單的貝氏推論：因為底層的語言模組只會提供單元圖資料。一旦將所有可以組字的單元圖
    /// 作為節點塞到組字器內，就可以用一個簡單的有向無環圖組句過程、來利用這些隱性資料值
    /// 算出最大相似估算結果。
    /// </remarks>
    /// <param name="langModel">要對接的語言模組。</param>
    /// <param name="separator">多字讀音鍵當中用以分割漢字讀音的記號，預設為「-」。詳見 <see cref="Separator"/>。</param>
    public Compositor(LangModelProtocol langModel, string separator = "-") {
      _theLangModel = langModel;
      Config = new(separator: separator);
      TheSeparator = separator;
    }

    /// <summary>
    /// 以指定組字器生成拷貝。
    /// </summary>
    /// <remarks>
    /// 因為 Node 不是 Struct，所以會在 Compositor 被拷貝的時候無法被真實複製。
    /// 這樣一來，Compositor 複製品當中的 Node 的變化會被反應到原先的 Compositor 身上。
    /// 這在某些情況下會造成意料之外的混亂情況，所以需要引入一個拷貝用的建構子。
    /// </remarks>
    /// <param name="compositor"></param>
    public Compositor(Compositor compositor) {
      _theLangModel = compositor.TheLangModel;
      Config = compositor.Config.HardCopy();
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
    public Compositor Copy() => new(compositor: this);

    /// <summary>
    /// 創建所有節點的覆寫狀態鏡照。
    /// </summary>
    /// <returns>包含所有節點 ID 到覆寫狀態映射的字典。</returns>
    public Dictionary<Guid, NodeOverrideStatus> CreateNodeOverrideStatusMirror() =>
      Config.CreateNodeOverrideStatusMirror();

    /// <summary>
    /// 從節點覆寫狀態鏡照恢復所有節點的覆寫狀態。
    /// </summary>
    /// <param name="mirror">包含節點 ID 到覆寫狀態映射的字典。</param>
    public void RestoreFromNodeOverrideStatusMirror(Dictionary<Guid, NodeOverrideStatus> mirror) =>
      Config.RestoreFromNodeOverrideStatusMirror(mirror);

    /// <summary>
    /// 重置包括游標在內的各項參數，且清空各種由組字器生成的內部資料。<para/>
    /// 且將已經被插入的索引鍵陣列與幅節單元陣列（包括其內的節點）全部清空。
    /// 最近一次的組句結果陣列也會被清空。游標跳轉換算表也會被清空。
    /// </summary>
    public void Clear() { Config.Clear(); }

    /// <summary>
    /// 在游標位置插入給定的索引鍵。
    /// </summary>
    /// <param name="key">要插入的索引鍵。</param>
    /// <returns>該操作是否成功執行。</returns>
    public bool InsertKey(string key) {
      if (string.IsNullOrEmpty(key) || key == Separator) return false;
      if (!TheLangModel.HasUnigramsFor(new() { key })) return false;
      Keys.Insert(Cursor, key);
      List<Segment> gridBackup = Segments.Select(x => x.HardCopy()).ToList();
      ResizeGridAt(Cursor, ResizeBehavior.Expand);
      int nodesInserted = AssignNodes();
      // 用來在 langModel.HasUnigramsFor() 結果不準確的時候防呆、恢復被搞壞的 Segments。
      if (nodesInserted == 0) {
        Segments = gridBackup;
        return false;
      }

      Cursor += 1; // 游標必須得在執行 update() 之後才可以變動。
      return true;
    }

    /// <summary>
    /// 朝著指定方向砍掉一個與游標相鄰的讀音。
    /// </summary>
    /// <remarks>
    /// 在威注音的術語體系當中，「與文字輸入方向相反的方向」為向後（Rear），反之則為向前（Front）。
    /// 如果是朝著與文字輸入方向相反的方向砍的話，游標位置會自動遞減。
    /// </remarks>
    /// <param name="direction">指定方向（相對於文字輸入方向而言）</param>
    /// <returns>該操作是否成功執行。</returns>
    public bool DropKey(TypingDirection direction) {
      bool isBksp = direction == TypingDirection.ToRear;
      if (Cursor == (isBksp ? 0 : Keys.Count)) return false;
      Keys.RemoveAt(Cursor - (isBksp ? 1 : 0));
      Cursor -= isBksp ? 1 : 0;
      ResizeGridAt(Cursor, ResizeBehavior.Shrink);
      AssignNodes();
      return true;
    }

    /// <summary>
    /// 獲取當前標記範圍，並返回符合 C# 標準語意的半開區間。
    /// </summary>
    /// <returns>當前標記範圍對應的 <see cref="Range"/>。</returns>
    public Range CurrentMarkedRange() =>
      new(Math.Min(Cursor, Marker), Math.Max(Cursor, Marker));

    /// <summary>
    /// 偵測是否出現游標切斷組字區內字元的情況。
    /// </summary>
    /// <param name="isMarker">是否檢查副游標。</param>
    /// <returns>是否發生游標切斷字元的情況。</returns>
    public bool IsCursorCuttingChar(bool isMarker = false) {
      int index = isMarker ? Marker : Cursor;
      return AssembledSentence.IsCursorCuttingChar(index);
    }

    /// <summary>
    /// 判斷游標是否可以繼續沿著給定方向移動。
    /// </summary>
    /// <param name="direction">指定方向（相對於文字輸入方向而言）。</param>
    /// <param name="isMarker">是否為標記游標。</param>
    /// <returns>游標是否已經抵達邊界。</returns>
    public bool IsCursorAtEdge(TypingDirection direction, bool isMarker = false) {
      int pos = isMarker ? Marker : Cursor;
      return direction switch {
        TypingDirection.ToFront => pos == Length,
        TypingDirection.ToRear => pos == 0,
        _ => false
      };
    }

    /// <summary>
    /// 按步移動游標。如果遇到游標切斷組字區內字元的情況，則改為按幅節移動直到該情況消失。
    /// </summary>
    /// <param name="direction">指定方向（相對於文字輸入方向而言）。</param>
    /// <param name="isMarker">是否為標記游標。</param>
    /// <returns>是否成功移動。</returns>
    public bool MoveCursorStepwise(TypingDirection direction, bool isMarker = false) {
      int delta = direction switch {
        TypingDirection.ToFront => 1,
        TypingDirection.ToRear => -1,
        _ => 0
      };
      if (IsCursorAtEdge(direction, isMarker)) return false;
      if (isMarker) {
        Marker += delta;
      } else {
        Cursor += delta;
      }

      if (IsCursorCuttingChar(isMarker)) {
        return JumpCursorBySegment(direction, isMarker);
      }

      return true;
    }

    /// <summary>
    /// 按幅節來前後移動游標。
    /// </summary>
    /// <remarks>
    /// 在威注音的術語體系當中，「與文字輸入方向相反的方向」為向後（Rear），反之則為向前（Front）。
    /// </remarks>
    /// <param name="direction">指定移動方向（相對於文字輸入方向而言）。</param>
    /// <param name="isMarker">
    /// 要移動的是否為作為選擇標記的副游標（而非打字用的主游標）。
    /// 具體用法可以是這樣：你在標記模式下，
    /// 如果出現了「副游標切了某個字音數量不相等的節點」的情況的話，
    /// 則直接用這個函式將副游標往前推到接下來的正常的位置上。
    /// // 該特性不適用於小麥注音，除非小麥注音重新設計 InputState 且修改 KeyHandler、
    /// 將標記游標交給敝引擎來管理。屆時，NSStringUtils 將徹底卸任。
    /// </param>
    /// <returns>該操作是否成功執行。</returns>
    public bool JumpCursorBySegment(TypingDirection direction, bool isMarker = false) {
      int target = isMarker ? Marker : Cursor;
      switch (direction) {
        case TypingDirection.ToFront:
          if (target == Length) return false;
          break;
        case TypingDirection.ToRear:
          if (target == 0) return false;
          break;
      }

      if (!AssembledSentence.CursorRegionMap().TryGetValue(key: target, out int currentRegion)) return false;
      int guardedCurrentRegion = Math.Min(AssembledSentence.Count - 1, currentRegion);
      int aRegionForward = Math.Max(currentRegion - 1, 0);
      int currentRegionBorderRear = AssembledSentence.GetRange(0, currentRegion).Select(x => x.SegLength).Sum();

      if (target == currentRegionBorderRear) {
        target = direction switch {
          TypingDirection.ToFront =>
            currentRegion > AssembledSentence.Count
              ? Keys.Count
              : AssembledSentence.GetRange(0, guardedCurrentRegion + 1).Select(x => x.SegLength).Sum(),
          TypingDirection.ToRear => AssembledSentence.GetRange(0, aRegionForward).Select(x => x.SegLength).Sum(),
          _ => target
        };
      } else {
        target = direction switch {
          TypingDirection.ToFront => currentRegionBorderRear
                                     + AssembledSentence[guardedCurrentRegion].SegLength,
          TypingDirection.ToRear => currentRegionBorderRear,
          _ => target
        };
      }

      switch (isMarker) {
        case false:
          Cursor = target;
          break;
        case true:
          Marker = target;
          break;
      }

      return true;
    }

    /// <summary>
    /// 生成用以交給 GraphViz 診斷的資料檔案內容，純文字。
    /// </summary>
    /// <returns>生成的資料。</returns>
    public string DumpDOT() {
      // C# StringBuilder 與 Swift NSMutableString 能提供爆發性的效能。
      StringBuilder strOutput = new();
      strOutput.Append("digraph {\ngraph [ rankdir=LR ];\nBOS;\n");
      for (int p = 0; p < Segments.Count; p++) {
        Segment segment = Segments[p];
        for (int ni = 0; ni <= segment.MaxLength; ni++) {
          if (segment.NodeOf(ni) is not { } np) continue;
          if (p == 0) strOutput.Append("BOS -> " + np.CurrentPair.Value + ";\n");
          strOutput.Append(np.CurrentPair.Value + ";\n");
          if (p + ni < Segments.Count) {
            Segment destinationSegment = Segments[p + ni];
            for (int q = 0; q <= destinationSegment.MaxLength; q++) {
              if (destinationSegment.NodeOf(q) is not { } dn) continue;
              strOutput.Append(np.CurrentPair.Value + " -> " + dn.CurrentPair.Value + ";\n");
            }
          }

          if (p + ni == Segments.Count) strOutput.Append(np.CurrentPair.Value + " -> EOS;\n");
        }
      }

      strOutput.Append("EOS;\n}\n");
      return strOutput.ToString();
    }

    // MARK: - Internal Methods (Maybe Public)

    /// <summary>
    /// 在該軌格的指定幅節座標擴增或減少一個幅節單元。
    /// </summary>
    /// <param name="location">給定的幅節座標。</param>
    /// <param name="action">指定是擴張還是縮減一個幅節。</param>
    internal void ResizeGridAt(int location, ResizeBehavior action) {
      location = Math.Max(Math.Min(location, Segments.Count), 0); // 防呆。
      switch (action) {
        case ResizeBehavior.Expand:
          Segments.Insert(location, new());
          if (location == 0 || location == Segments.Count) return;
          break;
        case ResizeBehavior.Shrink:
          if (Segments.Count == location) return;
          Segments.RemoveAt(location);
          break;
      }

      DropWreckedNodesAt(location);
    }

    /// <summary>
    /// 扔掉所有被 ResizeGrid() 損毀的節點。
    ///
    /// 拿新增幅節來打比方的話，在擴增幅節之前：
    /// <code>
    /// Segment Index 0   1   2   3
    ///                (---)
    ///                (-------)
    ///            (-----------)
    /// </code>
    /// 在幅節座標 2 (SegmentIndex = 2) 的位置擴增一個幅節之後:
    /// <code>
    /// Segment Index 0   1   2   3   4
    ///                (---)
    ///                (XXX?   ?XXX) ←被扯爛的節點
    ///            (XXXXXXX?   ?XXX) ←被扯爛的節點
    /// </code>
    /// 拿縮減幅節來打比方的話，在縮減幅節之前：
    /// <code>
    /// Segment Index 0   1   2   3
    ///                (---)
    ///                (-------)
    ///            (-----------)
    /// </code>
    /// 在幅節座標 2 的位置就地砍掉一個幅節之後:
    /// <code>
    /// Segment Index 0   1   2   3   4
    ///                (---)
    ///                (XXX? ←被砍爛的節點
    ///            (XXXXXXX? ←被砍爛的節點
    /// </code>
    /// </summary>
    /// <param name="location">給定的幅節座標。</param>
    internal void DropWreckedNodesAt(int location) {
      location = Math.Max(Math.Min(location, Segments.Count), 0); // 防呆。
      if (Segments.IsEmpty()) return;
      int affectedLength = MaxSegLength - 1;
      int begin = Math.Max(0, location - affectedLength);
      if (location < begin) return;
      foreach (int delta in new ClosedRange(begin, location)) {
        int lowestLength = location - delta + 1;
        if (lowestLength > MaxSegLength) break;
        foreach (int theLength in new ClosedRange(lowestLength, MaxSegLength)) {
          if (delta >= 0 && delta < Segments.Count) {
            Segments[delta].Nodes.Remove(theLength);
          }
        }
      }
    }

    /// <summary>
    /// 自索引鍵陣列獲取指定範圍的資料。
    /// </summary>
    /// <param name="range">指定範圍。</param>
    /// <returns>拿到的資料。</returns>
    private List<string> GetJoinedKeyArray(ClosedRange range) =>
      range.Upperbound <= Keys.Count && range.Lowerbound >= 0
        ? Keys.GetRange(range.Lowerbound, range.Upperbound - range.Lowerbound)
        : new();

    /// <summary>
    /// 在指定位置（以指定索引鍵陣列和指定幅節長度）拿取節點。
    /// </summary>
    /// <param name="location">指定游標位置。</param>
    /// <param name="length">指定幅節長度。</param>
    /// <param name="keyArray">指定索引鍵陣列。</param>
    /// <returns>拿取的節點。拿不到的話就會是 null。</returns>
    private Node? GetNodeAt(int location, int length, List<string> keyArray) {
      // 這個函式是個語法糖，僅存在於 C# 版當中。
      location = Math.Max(Math.Min(location, Segments.Count - 1), 0); // 防呆。
      if (Segments[location].NodeOf(length) is not { } node) return null;
      return node.KeyArray.SequenceEqual(keyArray) ? node : null;
    }

    /// <summary>
    /// 根據當前狀況更新整個組字器的節點文脈。
    /// </summary>
    /// <param name="updateExisting">
    /// 是否根據目前的語言模型的資料狀態來對既有節點更新其內部的單元圖陣列資料。
    /// 該特性可以用於「在選字窗內屏蔽了某個詞之後，立刻生效」這樣的軟體功能需求的實現。
    /// </param>
    /// <returns>新增或影響了多少個節點。如果返回「0」則表示可能發生了錯誤。 </returns>
    public int AssignNodes(bool updateExisting = false) {
      int lowerboundPos = updateExisting ? 0 : Math.Max(0, Cursor - MaxSegLength);
      int upperboundPos = updateExisting ? Segments.Count : Math.Min(Cursor + MaxSegLength, Keys.Count);
      ClosedRange rangeOfPos = new(lowerboundPos, upperboundPos);
      int nodesChangedCounter = 0;
      Dictionary<string, List<Unigram>> queryBuffer = new();
      foreach (int position in rangeOfPos) {
        int maxLengthWithinRange = Math.Min(MaxSegLength, rangeOfPos.Upperbound - position);
        foreach (int theLength in new ClosedRange(1, maxLengthWithinRange)) {
          if (position + theLength > Keys.Count || position < 0) continue;
          List<string> joinedKeyArray = GetJoinedKeyArray(new(position, position + theLength));
          ClosedRange safeLocationRange = new(0, Segments.Count);
          Node? theNode = safeLocationRange.Contains(position) ? GetNodeAt(position, theLength, joinedKeyArray) : null;
          if (theNode is { }) {
            if (!updateExisting) continue;
            List<Unigram> unigramsExisting = GetSortedUnigrams(joinedKeyArray, ref queryBuffer);
            // 自動銷毀無效的節點。
            if (unigramsExisting.IsEmpty()) {
              if (theNode.KeyArray.Count == 1) continue;
              Segments[position].Nodes.Remove(theNode.SegLength);
            } else {
              theNode.SyncingUnigramsFrom(unigramsExisting);
            }

            nodesChangedCounter += 1;
            continue;
          }

          List<Unigram> unigramsNew = GetSortedUnigrams(joinedKeyArray, ref queryBuffer);
          if (unigramsNew.IsEmpty()) continue;
          // Position 始終不小於 0。
          if (position >= Segments.Count) continue;
          Segments[position].Nodes[theLength] = new(joinedKeyArray, theLength, unigramsNew);
          nodesChangedCounter += 1;
        }
      }

      queryBuffer.Clear(); // 手動清理，免得 GC 拖時間。
      if (nodesChangedCounter > 0) {
        Assemble();
      }

      return nodesChangedCounter;
    }

    private List<Unigram> GetSortedUnigrams(List<string> keyArray, ref Dictionary<string, List<Unigram>> cache) {
      string cacheKey = keyArray.Joined("\u001F");
      if (cache.TryGetValue(cacheKey, out List<Unigram>? cached)) {
        return cached.Select(x => x.Copy()).ToList();
      }

      List<Unigram> canonical = TheLangModel
                                .UnigramsFor(keyArray)
                                .Select(source => {
                                  if (source.KeyArray.SequenceEqual(keyArray)) {
                                    return source.Copy();
                                  }

                                  return source.Copy(keyArray);
                                })
                                .OrderByDescending(x => x.Score)
                                .ToList();
      cache[cacheKey] = canonical;
      return canonical.Select(x => x.Copy()).ToList();
    }
  }

  /// <summary>
  /// 組字器的組態設定專用記錄物件。
  /// </summary>
  public struct CompositorConfig : IEquatable<CompositorConfig> {
    /// <summary>
    /// 初期化一套組字器組態設定。
    /// </summary>
    public CompositorConfig(
      List<GramInPath>? assembledSentence = null,
      List<string>? keys = null,
      List<Compositor.Segment>? segments = null,
      int cursor = 0,
      int maxSegLength = 10,
      int marker = 0,
      string separator = ""
    ) {
      AssembledSentence = assembledSentence ?? new List<GramInPath>();
      Keys = keys ?? new List<string>();
      Segments = segments ?? new List<Compositor.Segment>();
      _cursor = cursor;
      _maxSegLength = Math.Max(6, maxSegLength);
      _marker = marker;
      Separator = separator;
    }

    /// <summary>
    /// 最近一次組句結果。
    /// </summary>
    public List<GramInPath> AssembledSentence { get; set; }

    /// <summary>
    /// 該組字器已經插入的的索引鍵，以陣列的形式存放。
    /// </summary>
    public List<string> Keys { get; set; }

    /// <summary>
    /// 該組字器的幅節單元陣列。
    /// </summary>
    public List<Compositor.Segment> Segments { get; set; }

    private int _cursor = 0;

    /// <summary>
    /// 該組字器的敲字游標位置。
    /// </summary>
    public int Cursor {
      get => _cursor;
      set {
        _cursor = Math.Max(0, Math.Min(value, Length));
        _marker = Cursor; // 同步当前游标至标记器。
      }
    }

    private int _maxSegLength = 10;

    /// <summary>
    /// 一個幅節單元內所能接受的最長的節點幅節長度。
    /// </summary>
    public int MaxSegLength {
      get => _maxSegLength;
      set {
        _maxSegLength = Math.Max(6, value);
        DropNodesBeyondMaxSegLength();
      }
    }

    private int _marker = 0;

    /// <summary>
    /// 該組字器的標記器（副游標）位置。
    /// </summary>
    public int Marker {
      get => _marker;
      set => _marker = Math.Max(0, Math.Min(value, Length));
    }

    private string _separator = Compositor.TheSeparator;

    /// <summary>
    /// 在生成索引鍵字串時用來分割每個索引鍵單位。最好是鍵盤無法直接敲的 ASCII 字元。
    /// </summary>
    /// <remarks>
    /// 更新該內容會同步更新 <see cref="Compositor.TheSeparator"/>；每次初期化一個新的組字器的時候都會覆寫之。
    /// </remarks>
    public string Separator {
      readonly get => _separator;
      set {
        _separator = value;
        Compositor.TheSeparator = _separator;
      }
    }

    /// <summary>
    /// 公開：該組字器的長度。<para/>
    /// 組字器內已經插入的單筆索引鍵的數量，也就是內建漢字讀音的數量（唯讀）。
    /// </summary>
    /// <remarks>
    /// 理論上而言，segments.count 也是這個數。
    /// 但是，為了防止萬一，就用了目前的方法來計算。
    /// </remarks>
    public int Length => Keys.Count;

    /// <summary>
    /// 生成自身的拷貝。
    /// </summary>
    /// <remarks>
    /// 因為 Node 不是 Struct，所以會在 Compositor 被拷貝的時候無法被真實複製。
    /// 這樣一來，Compositor 複製品當中的 Node 的變化會被反應到原先的 Compositor 身上。
    /// 這在某些情況下會造成意料之外的混亂情況，所以需要引入一個拷貝用的建構子。
    /// </remarks>
    /// <returns>拷貝。</returns>
    public CompositorConfig HardCopy() {
      CompositorConfig config = this;
      config.Keys = Keys.ToList(); // List 是 class，需要單獨複製。
      config.Segments = Segments.Select(x => x.HardCopy()).ToList();
      config.AssembledSentence = AssembledSentence.ToList();
      config.Separator = Separator;
      config.MaxSegLength = MaxSegLength;
      config.Cursor = Cursor;
      config.Marker = Marker;
      return config;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj) {
      if (obj is not CompositorConfig other) return false;
      return Cursor == other.Cursor && Marker == other.Marker && MaxSegLength == other.MaxSegLength &&
             Separator == other.Separator && Keys.SequenceEqual(other.Keys) && Segments.SequenceEqual(other.Segments) &&
             AssembledSentence.SequenceEqual(other.AssembledSentence);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode() {
      HashCode hash = new();
      hash.Add(_cursor);
      hash.Add(_marker);
      hash.Add(_maxSegLength);
      hash.Add(_separator);

      foreach (string key in Keys) {
        hash.Add(key);
      }

      foreach (Compositor.Segment segment in Segments) {
        hash.Add(segment);
      }

      foreach (GramInPath gram in AssembledSentence) {
        hash.Add(gram);
      }

      return hash.ToHashCode();
    }

    /// <summary>
    /// 重置包括游標在內的各項參數，且清空各種由組字器生成的內部資料。<para/>
    /// 且將已經被插入的索引鍵陣列與幅節單元陣列（包括其內的節點）全部清空。
    /// 最近一次的組句結果陣列也會被清空。游標跳轉換算表也會被清空。
    /// </summary>
    public void Clear() {
      Keys = new();
      Segments = new();
      AssembledSentence = new();
      Cursor = 0;
      Marker = 0;
    }

    /// <summary>
    /// 清除所有幅長超過 MaxSegLength 的節點。
    /// </summary>
    public void DropNodesBeyondMaxSegLength() {
      if (Segments.IsEmpty()) return;
      List<int> indicesOfPositions = new ClosedRange(0, Segments.Count - 1).ToList();
      foreach (int currentPos in indicesOfPositions) {
        foreach (int currentSegLength in Segments[currentPos].Nodes.Keys) {
          if (currentSegLength > MaxSegLength) {
            Segments[currentPos].Nodes.Remove(currentSegLength);
          }
        }
      }
    }

    /// <summary>
    /// 創建所有節點的覆寫狀態鏡照。
    /// </summary>
    /// <returns>包含所有節點 ID 到覆寫狀態映射的字典。</returns>
    public Dictionary<Guid, NodeOverrideStatus> CreateNodeOverrideStatusMirror() {
      var mirror = new Dictionary<Guid, NodeOverrideStatus>();
      foreach (var segment in Segments) {
        foreach (var node in segment.Nodes.Values) {
          mirror[node.Id] = node.OverrideStatus;
        }
      }

      return mirror;
    }

    /// <summary>
    /// 從節點覆寫狀態鏡照恢復所有節點的覆寫狀態。
    /// </summary>
    /// <param name="mirror">包含節點 ID 到覆寫狀態映射的字典。</param>
    public void RestoreFromNodeOverrideStatusMirror(Dictionary<Guid, NodeOverrideStatus> mirror) {
      foreach (var segment in Segments) {
        foreach (var node in segment.Nodes.Values) {
          if (mirror.TryGetValue(node.Id, out var status)) {
            node.OverrideStatus = status;
          }
        }
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(CompositorConfig left, CompositorConfig right) {
      return left.Equals(right);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(CompositorConfig left, CompositorConfig right) {
      return !(left == right);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(CompositorConfig other) {
      return _cursor == other._cursor && _maxSegLength == other._maxSegLength && _marker == other._marker &&
             _separator == other._separator && Keys.SequenceEqual(other.Keys) &&
             Segments.SequenceEqual(other.Segments) &&
             AssembledSentence.SequenceEqual(other.AssembledSentence);
    }
  }
} // namespace Megrez
