// CSharpened and further development by (c) 2022 and onwards The vChewing Project (MIT License).
// Was initially rebranded from (c) Lukhnos Liu's C++ library "Gramambular 2" (MIT License).
// ====================
// This code is released under the MIT license (SPDX-License-Identifier: MIT)

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// 著作權聲明：
// 除了 Megrez 專有的修改與實作以外，該套件所有程式邏輯來自於 Gramambular、算法歸 Lukhnos Liu 所有。
// 天權星引擎（Megrez Compositor）僅僅是將 Gramambular 用 Swift 重寫之後繼續開發的結果而已。

// 術語：

// Grid: 節軌
// Walk: 爬軌
// Node: 節點
// SpanLength: 節幅
// Span: 幅位

namespace Megrez {
/// <summary>
/// 一個組字器用來在給定一系列的索引鍵的情況下（藉由一系列的觀測行為）返回一套資料值。<para/>
/// 用於輸入法的話，給定的索引鍵可以是注音、且返回的資料值都是漢語字詞組合。該組字器
/// 還可以用來對文章做分節處理：此時的索引鍵為漢字，返回的資料值則是漢語字詞分節組合。
/// </summary>
/// <remarks>
/// 雖然這裡用了隱性 Markov 模型（HMM）的術語，但實際上在爬軌時用到的則是更
/// 簡單的貝氏推論：因為底層的語言模組只會提供單元圖資料。一旦將所有可以組字的單元圖
/// 作為節點塞到組字器內，就可以用一個簡單的有向無環圖爬軌過程、來利用這些隱性資料值
/// 算出最大相似估算結果。
/// </remarks>
public partial struct Compositor {
  // MARK: - Enums.
  /// <summary>
  /// 就文字輸入方向而言的方向。
  /// </summary>
  public enum TypingDirection {
    /// <summary>
    /// 就文字輸入方向而言的前方。
    /// </summary>
    ToFront,
    /// <summary>
    /// 就文字輸入方向而言的後方。
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

  private static int _maxSpanLength = 10;
  /// <summary>
  /// 一個幅位單元內所能接受的最長的節點幅位長度。
  /// </summary>
  public static int MaxSpanLength {
    get => _maxSpanLength;
    set => _maxSpanLength = Math.Max(6, value);
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
  /// </summary>
  /// <remarks>
  /// 更新該內容會同步更新 <see cref="TheSeparator"/>；每次初期化一個新的組字器的時候都會覆寫之。
  /// </remarks>
  public string Separator {
    get => TheSeparator;
    set => TheSeparator = value;
  }

  private int _cursor = 0;
  /// <summary>
  /// 該組字器的敲字游標位置。
  /// </summary>
  public int Cursor {
    get => _cursor;
    set {
      _cursor = Math.Max(0, Math.Min(value, Length));
      _marker = Cursor;
    }
  }

  private int _marker = 0;
  /// <summary>
  /// 該組字器的標記器（副游標）位置。
  /// </summary>
  public int Marker {
    get => _cursor;
    set => _marker = Math.Max(0, Math.Min(value, Length));
  }

  /// <summary>
  /// 公開：該組字器的長度。<para/>
  /// 組字器內已經插入的單筆索引鍵的數量，也就是內建漢字讀音的數量（唯讀）。
  /// </summary>
  /// <remarks>
  /// 理論上而言，spans.count 也是這個數。
  /// 但是，為了防止萬一，就用了目前的方法來計算。
  /// </remarks>
  public int Length => Keys.Count;
  /// <summary>
  /// 最近一次爬軌結果。
  /// </summary>
  public List<Node> WalkedNodes = new();
  /// <summary>
  /// 組字器是否為空。
  /// </summary>
  public bool IsEmpty => Spans.IsEmpty() && Keys.IsEmpty();
  /// <summary>
  /// 該組字器已經插入的的索引鍵，以陣列的形式存放。
  /// </summary>
  public List<string> Keys { get; }
  /// <summary>
  /// 該組字器的幅位單元陣列。
  /// </summary>
  public List<SpanUnit> Spans { get; private set; }

  private LangModelRanked _theLangModel;
  /// <summary>
  /// 該組字器所使用的語言模型（被 LangModelRanked 所封裝）。
  /// </summary>
  public LangModelRanked TheLangModel {
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
  /// 雖然這裡用了隱性 Markov 模型（HMM）的術語，但實際上在爬軌時用到的則是更
  /// 簡單的貝氏推論：因為底層的語言模組只會提供單元圖資料。一旦將所有可以組字的單元圖
  /// 作為節點塞到組字器內，就可以用一個簡單的有向無環圖爬軌過程、來利用這些隱性資料值
  /// 算出最大相似估算結果。
  /// </remarks>
  /// <param name="langModel">要對接的語言模組。</param>
  /// <param name="separator">多字讀音鍵當中用以分割漢字讀音的記號，預設為「-」。詳見 <see cref="Separator"/>。</param>
  public Compositor(LangModelProtocol langModel, string separator = "-") {
    _theLangModel = new(ref langModel);
    TheSeparator = separator;
    Keys = new();
    Spans = new();
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
    _cursor = compositor.Cursor;
    _marker = compositor.Marker;
    Keys = compositor.Keys;
    Spans = new();
    Separator = compositor.Separator;
    foreach (Node walkedNode in compositor.WalkedNodes) WalkedNodes.Add(walkedNode.Copy());
    foreach (SpanUnit span in compositor.Spans) Spans.Add(span.HardCopy());
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
  public Compositor HardCopy() => new(compositor: this);

  /// <summary>
  /// 重置包括游標在內的各項參數，且清空各種由組字器生成的內部資料。<para/>
  /// 且將已經被插入的索引鍵陣列與幅位單元陣列（包括其內的節點）全部清空。
  /// 最近一次的爬軌結果陣列也會被清空。游標跳轉換算表也會被清空。
  /// </summary>
  public void Clear() {
    _cursor = 0;
    _marker = 0;
    Keys.Clear();
    Spans.Clear();
    WalkedNodes.Clear();
  }

  /// <summary>
  /// 在游標位置插入給定的索引鍵。
  /// </summary>
  /// <param name="key">要插入的索引鍵。</param>
  /// <returns>該操作是否成功執行。</returns>
  public bool InsertKey(string key) {
    if (string.IsNullOrEmpty(key) || key == Separator) return false;
    if (!TheLangModel.HasUnigramsFor(new() { key })) return false;
    Keys.Insert(Cursor, key);
    List<SpanUnit> gridBackup = Spans;
    ResizeGridAt(Cursor, ResizeBehavior.Expand);
    int nodesInserted = Update();
    // 用來在 langModel.HasUnigramsFor() 結果不準確的時候防呆、恢復被搞壞的 Spans。
    if (nodesInserted == 0) {
      Spans = gridBackup;
      return false;
    }
    Cursor += 1;  // 游標必須得在執行 update() 之後才可以變動。
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
    Update();
    return true;
  }

  /// <summary>
  /// 按幅位來前後移動游標。
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
  public bool JumpCursorBySpan(TypingDirection direction, bool isMarker = false) {
    int target = isMarker ? Marker : Cursor;
    switch (direction) {
      case TypingDirection.ToFront:
        if (target == Length) return false;
        break;
      case TypingDirection.ToRear:
        if (target == 0) return false;
        break;
    }
    if (!WalkedNodes.CursorRegionMap().TryGetValue(key: target, out int currentRegion)) return false;
    int guardedCurrentRegion = Math.Min(WalkedNodes.Count - 1, currentRegion);
    int aRegionForward = Math.Max(currentRegion - 1, 0);
    int currentRegionBorderRear = WalkedNodes.GetRange(0, currentRegion).Select(x => x.SpanLength).Sum();

    if (target == currentRegionBorderRear) {
      target = direction switch {
        TypingDirection.ToFront =>
            currentRegion > WalkedNodes.Count
                ? Keys.Count
                : WalkedNodes.GetRange(0, guardedCurrentRegion + 1).Select(x => x.SpanLength).Sum(),
        TypingDirection.ToRear => WalkedNodes.GetRange(0, aRegionForward).Select(x => x.SpanLength).Sum(),
        _ => target
      };
    } else {
      target = direction switch { TypingDirection.ToFront =>
                                      currentRegionBorderRear + WalkedNodes[guardedCurrentRegion].SpanLength,
                                  TypingDirection.ToRear => currentRegionBorderRear,
                                  _ => target };
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
    for (int p = 0; p < Spans.Count; p++) {
      SpanUnit span = Spans[p];
      for (int ni = 0; ni <= span.MaxLength; ni++) {
        if (span.NodeOf(ni) is not {} np) continue;
        if (p == 0) strOutput.Append("BOS -> " + np.CurrentPair.Value + ";\n");
        strOutput.Append(np.CurrentPair.Value + ";\n");
        if (p + ni < Spans.Count) {
          SpanUnit destinationSpan = Spans[p + ni];
          for (int q = 0; q <= destinationSpan.MaxLength; q++) {
            if (destinationSpan.NodeOf(q) is not {} dn) continue;
            strOutput.Append(np.CurrentPair.Value + " -> " + dn.CurrentPair.Value + ";\n");
          }
        }
        if (p + ni == Spans.Count) strOutput.Append(np.CurrentPair.Value + " -> EOS;\n");
      }
    }
    strOutput.Append("EOS;\n}\n");
    return strOutput.ToString();
  }

  // MARK: - Internal Methods (Maybe Public)

  /// <summary>
  /// 在該軌格的指定幅位座標擴增或減少一個幅位單元。
  /// </summary>
  /// <param name="location">給定的幅位座標。</param>
  /// <param name="action">指定是擴張還是縮減一個幅位。</param>
  internal void ResizeGridAt(int location, ResizeBehavior action) {
    location = Math.Max(Math.Min(location, Spans.Count), 0);  // 防呆。
    switch (action) {
      case ResizeBehavior.Expand:
        Spans.Insert(location, new());
        if (location == 0 || location == Spans.Count) return;
        break;
      case ResizeBehavior.Shrink:
        if (Spans.Count == location) return;
        Spans.RemoveAt(location);
        break;
    }
    DropWreckedNodesAt(location);
  }

  /// <summary>
  /// 扔掉所有被 ResizeGrid() 損毀的節點。
  ///
  /// 拿新增幅位來打比方的話，在擴增幅位之前：
  /// <code>
  /// Span Index 0   1   2   3
  ///                (---)
  ///                (-------)
  ///            (-----------)
  /// </code>
  /// 在幅位座標 2 (SpanIndex = 2) 的位置擴增一個幅位之後:
  /// <code>
  /// Span Index 0   1   2   3   4
  ///                (---)
  ///                (XXX?   ?XXX) ←被扯爛的節點
  ///            (XXXXXXX?   ?XXX) ←被扯爛的節點
  /// </code>
  /// 拿縮減幅位來打比方的話，在縮減幅位之前：
  /// <code>
  /// Span Index 0   1   2   3
  ///                (---)
  ///                (-------)
  ///            (-----------)
  /// </code>
  /// 在幅位座標 2 的位置就地砍掉一個幅位之後:
  /// <code>
  /// Span Index 0   1   2   3   4
  ///                (---)
  ///                (XXX? ←被砍爛的節點
  ///            (XXXXXXX? ←被砍爛的節點
  /// </code>
  /// </summary>
  /// <param name="location">給定的幅位座標。</param>
  internal void DropWreckedNodesAt(int location) {
    location = Math.Max(Math.Min(location, Spans.Count), 0);  // 防呆。
    if (Spans.IsEmpty()) return;
    int affectedLength = MaxSpanLength - 1;
    int begin = Math.Max(0, location - affectedLength);
    if (location < begin) return;
    foreach (int i in new BRange(begin, location)) {
      Spans[i].DropNodesOfOrBeyond(location - i + 1);
    }
  }

  /// <summary>
  /// 自索引鍵陣列獲取指定範圍的資料。
  /// </summary>
  /// <param name="range">指定範圍。</param>
  /// <returns>拿到的資料。</returns>
  private List<string> GetJoinedKeyArray(BRange range) =>
      range.Upperbound <= Keys.Count && range.Lowerbound >= 0
          ? Keys.GetRange(range.Lowerbound, range.Upperbound - range.Lowerbound).ToList()
          : new();

  /// <summary>
  /// 在指定位置（以指定索引鍵陣列和指定幅位長度）拿取節點。
  /// </summary>
  /// <param name="location">指定游標位置。</param>
  /// <param name="length">指定幅位長度。</param>
  /// <param name="keyArray">指定索引鍵陣列。</param>
  /// <returns>拿取的節點。拿不到的話就會是 null。</returns>
  private Node? GetNodeAt(int location, int length, List<string> keyArray) {
    location = Math.Max(Math.Min(location, Spans.Count - 1), 0);  // 防呆。
    if (Spans[location].NodeOf(length) is not {} node) return null;
    return (node.KeyArray.SequenceEqual(keyArray)) ? node : null;
  }

  /// <summary>
  /// 根據當前狀況更新整個組字器的節點文脈。
  /// </summary>
  /// <param name="updateExisting">
  /// 是否根據目前的語言模型的資料狀態來對既有節點更新其內部的單元圖陣列資料。
  /// 該特性可以用於「在選字窗內屏蔽了某個詞之後，立刻生效」這樣的軟體功能需求的實現。
  /// </param>
  /// <returns>新增或影響了多少個節點。如果返回「0」則表示可能發生了錯誤。 </returns>
  public int Update(bool updateExisting = false) {
    BRange range = new(Math.Max(0, Cursor - MaxSpanLength), Math.Min(Cursor + MaxSpanLength, Keys.Count));
    int nodesChanged = 0;
    foreach (int position in range) {
      foreach (int theLength in new BRange(1, Math.Min(MaxSpanLength, range.Upperbound - position) + 1)) {
        List<string> joinedKeyArray = GetJoinedKeyArray(new(position, position + theLength));
        BRange safeLocationRange = new(0, Spans.Count);
        Node? theNode = safeLocationRange.Contains(position) ? GetNodeAt(position, theLength, joinedKeyArray) : null;
        if (theNode is {}) {
          if (!updateExisting) continue;
          List<Unigram> unigramsA = TheLangModel.UnigramsFor(joinedKeyArray);
          if (unigramsA.IsEmpty()) {
            if (theNode.KeyArray.Count == 1) continue;
            Spans[position].Nullify(givenNode: theNode);
          } else {
            theNode.SyncingUnigramsFrom(unigramsA);
          }
          nodesChanged += 1;
          continue;
        }
        List<Unigram> unigramsB = TheLangModel.UnigramsFor(joinedKeyArray);
        if (unigramsB.IsEmpty()) continue;
        Spans[position].Append(new(joinedKeyArray, theLength, unigramsB));
        nodesChanged += 1;
      }
    }
    return nodesChanged;
  }
}
}  // namespace Megrez
