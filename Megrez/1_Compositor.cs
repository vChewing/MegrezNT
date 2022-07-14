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
using System.Linq;
using System.Collections.Generic;

namespace Megrez {
/// <summary>
/// 組字器。
/// </summary>
public partial class Compositor : Grid {
  /// <summary>
  /// 就文字輸入方向而言的方向。
  /// </summary>
  public enum TypingDirection {
    /// <summary>
    /// 前方。
    /// </summary>
    ToFront,
    /// <summary>
    /// 後方。
    /// </summary>
    ToRear
  }
  /// <summary>
  /// 給被丟掉的節點路徑施加的負權重。
  /// </summary>
  private const double ConDroppedPathScore = -999;
  /// <summary>
  /// 該組字器的游標位置。
  /// </summary>
  private int _cursor;
  /// <summary>
  /// 該組字器所使用的語言模型。
  /// </summary>
  private LangModelProtocol _langModel;
  /// <summary>
  /// 公開：多字讀音鍵當中用以分割漢字讀音的記號，預設為空。
  /// </summary>
  public string JoinSeparator;
  /// <summary>
  /// 公開：該組字器的游標位置。
  /// </summary>
  public int Cursor {
    get => _cursor;
    set => _cursor = Math.Max(0, Math.Min(value, Readings.Count));
  }
  /// <summary>
  /// 允許查詢當前游標位置屬於第幾個幅位座標（從 0 開始算）。
  /// </summary>
  public Dictionary<int, int> CursorRegionMap { get; private set; } = new();
  /// <summary>
  /// 公開：該組字器的長度，也就是內建漢字讀音的數量（唯讀）。
  /// </summary>
  public int Length => Readings.Count;
  /// <summary>
  /// 公開：該組字器的讀音陣列（唯讀）。
  /// </summary>
  public List<string> Readings { get; } = new();

  /// <summary>
  /// 用以記錄爬過的節錨的陣列。
  /// </summary>
  public List<NodeAnchor> WalkedAnchors { get; private set; } = new();
  /// <summary>
  /// 該函式用以更新爬過的節錨的陣列。
  /// </summary>
  /// <param name="nodes">傳入的節點陣列。</param>
  public void UpdateWalkedAnchors(List<Node> nodes) {
    WalkedAnchors = new(from node in nodes select new NodeAnchor(node));
  }

  /// <summary>
  /// 按幅位來前後移動游標。
  /// </summary>
  /// <param name="direction">移動方向。</param>
  /// <returns>該操作是否順利完成。</returns>
  public bool JumpCursorBySpan(TypingDirection direction) {
    switch (direction) {
      case TypingDirection.ToFront:
        if (_cursor == Width) return false;
        break;
      case TypingDirection.ToRear:
        if (_cursor == 0) return false;
        break;
    }
    if (!CursorRegionMap.ContainsKey(_cursor)) return false;
    int currentRegion = CursorRegionMap[_cursor];

    int aRegionForward = Math.Max(currentRegion - 1, 0);
    int currentRegionBorderRear = WalkedAnchors.Take(currentRegion).Sum(x => x.SpanLength);
    if (_cursor == currentRegionBorderRear) {
      switch (direction) {
        case TypingDirection.ToFront:
          _cursor = currentRegion > WalkedAnchors.Count ? Readings.Count
                                                        : WalkedAnchors.Take(currentRegion + 1).Sum(x => x.SpanLength);
          break;
        case TypingDirection.ToRear:
          _cursor = WalkedAnchors.Take(aRegionForward).Sum(a => a.SpanLength);
          break;
      }
    } else {
      switch (direction) {
        case TypingDirection.ToFront:
          _cursor = currentRegionBorderRear + WalkedAnchors[currentRegion].SpanLength;
          break;
        case TypingDirection.ToRear:
          _cursor = currentRegionBorderRear;
          break;
      }
    }
    return true;
  }

  /// <summary>
  /// 組字器。
  /// </summary>
  /// <param name="lm">語言模型。可以是任何基於 Megrez.LangModelProtocol 的衍生型別。</param>
  /// <param name="length">指定該組字器內可以允許的最大詞長，預設為 10 字。</param>
  /// <param name="separator">多字讀音鍵當中用以分割漢字讀音的記號，預設為空。</param>
  public Compositor(LangModelProtocol lm, int length = 10, string separator = "") : base(length) {
    _langModel = lm;
    JoinSeparator = separator;
  }
  /// <summary>
  /// 組字器自我清空專用函式。
  /// </summary>
  public override void Clear() {
    base.Clear();
    _cursor = 0;
    Readings.Clear();
    WalkedAnchors.Clear();
  }
  /// <summary>
  /// 在游標位置插入給定的讀音。
  /// </summary>
  /// <param name="reading">要插入的讀音。</param>
  public bool InsertReading(string reading) {
    if (string.IsNullOrEmpty(reading) || !_langModel.HasUnigramsFor(reading)) return false;
    Readings.Insert(_cursor, reading);
    ResizeGridByOneAt(_cursor, ResizeBehavior.Expand);
    Build();
    _cursor += 1;
    return true;
  }
  /// <summary>
  /// 朝著指定方向砍掉一個與游標相鄰的讀音。<br />
  /// 在威注音的術語體系當中，「與文字輸入方向相反的方向」為向後（Rear），反之則為向前（Front）。
  /// <param name="direction">指定方向。</param>
  /// </summary>
  /// <returns>結果是否成功執行。</returns>
  public bool DropReading(TypingDirection direction) {
    bool isBackSpace = direction == TypingDirection.ToRear;
    if (Cursor == (isBackSpace ? 0 : Readings.Count)) return false;
    Readings.RemoveAt(Cursor - (isBackSpace ? 1 : 0));
    Cursor -= isBackSpace ? 1 : 0;
    ResizeGridByOneAt(Cursor, ResizeBehavior.Shrink);
    Build();
    return true;
  }
  /// <summary>
  /// 移除該組字器最先被輸入的第 X 個讀音單元。<br />
  /// 用於輸入法組字區長度上限處理：<br />
  /// 將該位置要溢出的敲字內容遞交之後、再執行這個函式。
  /// </summary>
  /// <param name="count">要移除的讀音單位數量。</param>
  /// <returns>結果是否成功執行。</returns>
  public bool RemoveHeadReadings(int count) {
    int theCount = Math.Abs(count);
    if (theCount > Length) return false;
    for (int I = 0; I < theCount; I++) {
      if (_cursor > 0) _cursor -= 1;
      if (Readings.Count > 0) {
        Readings.RemoveAt(0);
        ResizeGridByOneAt(0, ResizeBehavior.Shrink);
      }
      Build();
    }
    return true;
  }
  /// <summary>
  /// 對已給定的軌格按照給定的位置與條件進行正向爬軌。
  /// </summary>
  /// <returns>均有節點的節錨陣列。</returns>
  public List<NodeAnchor> Walk() {
    int newLocation = Width;
    WalkedAnchors = ReverseWalk(newLocation);
    WalkedAnchors.Reverse();
    WalkedAnchors = WalkedAnchors.Where(anchor => !anchor.IsEmpty).ToList();
    UpdateCursorJumpingTables(WalkedAnchors);
    return WalkedAnchors;
  }

  // MARK: - Private Functions

  /// <summary>
  /// 內部專用反芻函式，對已給定的軌格按照給定的位置與條件進行反向爬軌。
  /// </summary>
  /// <param name="location">開始爬軌的位置。</param>
  /// <param name="mass">給定累計權重，非必填參數。預設值為 0。</param>
  /// <param name="joinedPhrase">用以統計累計長詞的內部參數，請勿主動使用。</param>
  /// <param name="longPhrases">用以統計累計長詞的內部參數，請勿主動使用。</param>
  /// <returns>均有節點的節錨陣列。</returns>
  private List<NodeAnchor> ReverseWalk(int location, double mass = 0.0, string joinedPhrase = "",
                                       List<string>? longPhrases = default(List<string>)) {
    longPhrases ??= new();
    location = Math.Abs(location);
    if (location == 0 || location > Width) return new();
    List<List<NodeAnchor>> paths = new();
    List<NodeAnchor> nodes = NodesEndingAt(location).OrderByDescending(a => a.ScoreForSort).ToList();
    if (nodes.Count == 0) return new();
    Node zeroNode = nodes.FirstOrDefault().Node;

    if (zeroNode.Score >= Node.ConSelectedCandidateScore) {
      NodeAnchor zeroAnchor = nodes.FirstOrDefault();
      zeroAnchor.Mass = mass + zeroNode.Score;
      List<NodeAnchor> path = ReverseWalk(location - zeroAnchor.SpanLength, zeroAnchor.Mass);
      path.Insert(0, zeroAnchor);
      paths.Add(path);
    } else if (longPhrases.Count > 0) {
      List<NodeAnchor> path = new();
      foreach (NodeAnchor T in nodes) {
        NodeAnchor theAnchor = T;
        Node theNode = theAnchor.Node;
        string joinedValue = theNode.CurrentPair + joinedPhrase;
        // 如果只是一堆單漢字的節點組成了同樣的長詞的話，直接棄用這個節點路徑。
        // 打比方說「八/月/中/秋/山/林/涼」與「八月/中秋/山林/涼」在使用者來看
        // 是「結果等價」的，那就扔掉前者。
        if (longPhrases.Contains(joinedValue)) {
          theAnchor.Mass = ConDroppedPathScore;
          path.Insert(0, theAnchor);
          paths.Add(path);
          continue;
        }
        theAnchor.Mass = mass + theNode.Score;
        path = ReverseWalk(location - theAnchor.SpanLength, theAnchor.Mass,
                           joinedValue.Length >= (longPhrases.FirstOrDefault() ?? "").Length ? "" : joinedValue,
                           longPhrases);
        path.Insert(0, theAnchor);
        paths.Add(path);
      }
    } else {
      // 看看當前格位有沒有更長的候選字詞。
      longPhrases.Clear();
      longPhrases.AddRange(from theAnchor in nodes where theAnchor.Node != null let theNode = theAnchor.Node where theAnchor.SpanLength > 1 select theNode.CurrentPair.Value);
      longPhrases = longPhrases.OrderByDescending(a => a.Length).ToList();
      foreach (NodeAnchor T in nodes) {
        NodeAnchor theAnchor = T;
        Node theNode = theAnchor.Node;
        theAnchor.Mass = mass + theNode.Score;
        List<NodeAnchor> path = ReverseWalk(location - theAnchor.SpanLength, theAnchor.Mass,
                                            theAnchor.SpanLength > 1 ? "" : theNode.CurrentPair.Value, longPhrases);
        path.Insert(0, theAnchor);
        paths.Add(path);
      }
    }

    List<NodeAnchor> result = paths.FirstOrDefault() ?? new();
    foreach (List<NodeAnchor> neta in paths.Where(neta => neta.LastOrDefault().Mass > result.LastOrDefault().Mass)) {
      result = neta;
    }

    return result;  // 空節點過濾的步驟交給 Walk() 這個對外函式，以避免重複執行清理步驟。
  }

  private void Build() {
    int itrBegin = _cursor < MaxBuildSpanLength ? 0 : _cursor - MaxBuildSpanLength;
    int itrEnd = Math.Min(_cursor + MaxBuildSpanLength, Readings.Count);
    for (int p = itrBegin; p < itrEnd; p++) {
      for (int q = 1; q < MaxBuildSpanLength; q++) {
        if (p + q > itrEnd) break;
        List<string> arrSlice = Readings.GetRange(p, q);
        string combinedReading = Join(arrSlice, JoinSeparator);
        if (HasMatchedNode(p, q, combinedReading)) continue;
        List<Unigram> unigrams = _langModel.UnigramsFor(combinedReading);
        if (unigrams.Count == 0) continue;
        Node n = new(key: combinedReading, spanLength: q, unigrams: unigrams);
        InsertNode(n, p, q);
      }
    }
  }

  private void UpdateCursorJumpingTables(List<NodeAnchor> anchors) {
    Dictionary<int, int> cursorRegionMapDict = new();
    cursorRegionMapDict[-1] = 0;  // 防呆
    int counter = 0;
    foreach ((NodeAnchor item, int index)anchor in anchors.Select((item, index) => (item, index))) {
      for (int j = 0; j < anchor.item.SpanLength; j++) {
        cursorRegionMapDict[counter] = anchor.index;
        counter += 1;
      }
    }
    cursorRegionMapDict[counter] = anchors.Count;
    CursorRegionMap = cursorRegionMapDict;
  }

  private static string Join(IEnumerable<string> slice, string separator) => string.Join(separator, slice.ToList());
}
}