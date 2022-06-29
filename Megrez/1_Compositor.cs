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
public class Compositor {
  /// <summary>
  /// 給被丟掉的節點路徑施加的負權重。
  /// </summary>
  private const double ConDroppedPathScore = -999;
  /// <summary>
  /// 該組字器的游標位置。
  /// </summary>
  private int _cursorIndex;
  /// <summary>
  /// 該組字器所使用的語言模型。
  /// </summary>
  private LanguageModel _langModel;
  /// <summary>
  /// 公開：該組字器內可以允許的最大詞長。
  /// </summary>
  public int MaxBuildSpanLength => Grid.MaxBuildSpanLength;
  /// <summary>
  /// 公開：多字讀音鍵當中用以分割漢字讀音的記號，預設為空。
  /// </summary>
  public string JoinSeparator;
  /// <summary>
  /// 公開：該組字器的游標位置。
  /// </summary>
  public int CursorIndex {
    get => _cursorIndex;
    set => _cursorIndex = value < 0 ? 0 : Math.Min(value, Readings.Count);
  }
  /// <summary>
  /// 公開：該組字器是否為空。
  /// </summary>
  public bool IsEmpty => Grid.IsEmpty;
  /// <summary>
  /// 公開：該組字器的軌格（唯讀）。
  /// </summary>
  public Grid Grid { get; }
  /// <summary>
  /// 公開：該組字器的長度，也就是內建漢字讀音的數量（唯讀）。
  /// </summary>
  public int Length => Readings.Count;
  /// <summary>
  /// 公開：該組字器的讀音陣列（唯讀）。
  /// </summary>
  public List<string> Readings { get; } = new();
  /// <summary>
  /// 組字器。
  /// </summary>
  /// <param name="lm">語言模型。可以是任何基於 Megrez.LanguageModel 的衍生型別。</param>
  /// <param name="length">指定該組字器內可以允許的最大詞長，預設為 10 字。</param>
  /// <param name="separator">多字讀音鍵當中用以分割漢字讀音的記號，預設為空。</param>
  public Compositor(LanguageModel lm, int length = 10, string separator = "") {
    _langModel = lm;
    Grid = new(Math.Abs(length));
    JoinSeparator = separator;
  }
  /// <summary>
  /// 組字器自我清空專用函式。
  /// </summary>
  public void Clear() {
    _cursorIndex = 0;
    Readings.Clear();
    Grid.Clear();
  }
  /// <summary>
  /// 在游標位置插入給定的讀音。
  /// </summary>
  /// <param name="reading">要插入的讀音。</param>
  public void InsertReadingAtCursor(string reading) {
    Readings.Insert(_cursorIndex, reading);
    Grid.ExpandGridByOneAt(_cursorIndex);
    Build();
    _cursorIndex += 1;
  }
  /// <summary>
  /// 朝著與文字輸入方向相反的方向、砍掉一個與游標相鄰的讀音。<br />
  /// 在威注音的術語體系當中，「與文字輸入方向相反的方向」為向後（Rear）。
  /// </summary>
  /// <returns>結果是否成功執行。</returns>
  public bool DeleteReadingAtTheRearOfCursor() {
    if (_cursorIndex == 0) return false;
    Readings.RemoveAt(_cursorIndex - 1);
    _cursorIndex -= 1;
    Grid.ShrinkGridByOneAt(_cursorIndex);
    Build();
    return true;
  }
  /// <summary>
  /// 朝著往文字輸入方向、砍掉一個與游標相鄰的讀音。<br />
  /// 在威注音的術語體系當中，「文字輸入方向」為向前（Front）。
  /// </summary>
  /// <returns>結果是否成功執行。</returns>
  public bool DeleteReadingToTheFrontOfCursor() {
    if (_cursorIndex == Readings.Count) return false;
    Readings.RemoveAt(_cursorIndex);
    Grid.ShrinkGridByOneAt(_cursorIndex);
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
      if (_cursorIndex > 0) _cursorIndex -= 1;
      if (Readings.Count > 0) {
        Readings.RemoveAt(0);
        Grid.ShrinkGridByOneAt(0);
      }
      Build();
    }
    return true;
  }
  /// <summary>
  /// 對已給定的軌格按照給定的位置與條件進行正向爬軌。
  /// </summary>
  /// <param name="location">開始爬軌的位置。</param>
  /// <param name="accumulatedScore">給定累計權重，非必填參數。預設值為 0。</param>
  /// <param name="joinedPhrase">用以統計累計長詞的內部參數，請勿主動使用。</param>
  /// <param name="longPhrases">用以統計累計長詞的內部參數，請勿主動使用。</param>
  /// <returns>均有節點的節錨陣列。</returns>
  public List<NodeAnchor> Walk(int location = 0, double accumulatedScore = 0.0, string joinedPhrase = "",
                               List<string>? longPhrases = default(List<string>)) {
    int newLocation = Grid.Width - Math.Abs(location);
    List<NodeAnchor> result = ReverseWalk(newLocation, accumulatedScore, joinedPhrase, longPhrases);
    result.Reverse();
    return result;
  }
  /// <summary>
  /// 對已給定的軌格按照給定的位置與條件進行反向爬軌。
  /// </summary>
  /// <param name="location">開始爬軌的位置。</param>
  /// <param name="accumulatedScore">給定累計權重，非必填參數。預設值為 0。</param>
  /// <param name="joinedPhrase">用以統計累計長詞的內部參數，請勿主動使用。</param>
  /// <param name="longPhrases">用以統計累計長詞的內部參數，請勿主動使用。</param>
  /// <returns>均有節點的節錨陣列。</returns>
  public List<NodeAnchor> ReverseWalk(int location, double accumulatedScore = 0.0, string joinedPhrase = "",
                                      List<string>? longPhrases = default(List<string>)) {
    longPhrases ??= new();
    location = Math.Abs(location);
    if (location == 0 || location > Grid.Width) return new();
    List<List<NodeAnchor>> paths = new();
    List<NodeAnchor> nodes = Grid.NodesEndingAt(location);
    nodes = nodes.OrderByDescending(a => a.ScoreForSort).ToList();
    if (nodes.FirstOrDefault().Node == null) return new();

    Node zeroNode = nodes.FirstOrDefault().Node ?? new("", new());

    if (zeroNode.Score >= Node.ConSelectedCandidateScore) {
      NodeAnchor zeroAnchor = nodes.FirstOrDefault();
      zeroAnchor.AccumulatedScore = accumulatedScore + zeroNode.Score;
      List<NodeAnchor> path = ReverseWalk(location - zeroAnchor.SpanningLength, zeroAnchor.AccumulatedScore);
      path.Insert(0, zeroAnchor);
      paths.Add(path);
    } else if (longPhrases.Count > 0) {
      List<NodeAnchor> path = new();
      foreach (NodeAnchor T in nodes) {
        NodeAnchor theAnchor = T;
        if (theAnchor.Node == null) continue;
        Node theNode = theAnchor.Node;
        string joinedValue = theNode.CurrentKeyValue + joinedPhrase;
        // 如果只是一堆單漢字的節點組成了同樣的長詞的話，直接棄用這個節點路徑。
        // 打比方說「八/月/中/秋/山/林/涼」與「八月/中秋/山林/涼」在使用者來看
        // 是「結果等價」的，那就扔掉前者。
        if (longPhrases.Contains(joinedValue)) {
          theAnchor.AccumulatedScore = ConDroppedPathScore;
          path.Insert(0, theAnchor);
          paths.Add(path);
          continue;
        }
        theAnchor.AccumulatedScore = accumulatedScore + theNode.Score;
        path = ReverseWalk(location - theAnchor.SpanningLength, theAnchor.AccumulatedScore,
                           joinedValue.Length >= (longPhrases.FirstOrDefault() ?? "").Length ? "" : joinedValue,
                           longPhrases);
        path.Insert(0, theAnchor);
        paths.Add(path);
      }
    } else {
      // 看看當前格位有沒有更長的候選字詞。
      longPhrases.Clear();
      longPhrases.AddRange(from theAnchor in nodes where theAnchor.Node != null let theNode = theAnchor.Node where theAnchor.SpanningLength > 1 select theNode.CurrentKeyValue.Value);
      longPhrases = longPhrases.OrderByDescending(a => a.Length).ToList();
      foreach (NodeAnchor T in nodes) {
        NodeAnchor theAnchor = T;
        if (theAnchor.Node == null) continue;
        Node theNode = theAnchor.Node;
        theAnchor.AccumulatedScore = accumulatedScore + theNode.Score;
        List<NodeAnchor> path =
            ReverseWalk(location - theAnchor.SpanningLength, theAnchor.AccumulatedScore,
                        theAnchor.SpanningLength > 1 ? "" : theNode.CurrentKeyValue.Value, longPhrases);
        path.Insert(0, theAnchor);
        paths.Add(path);
      }
    }

    List<NodeAnchor> result = paths.FirstOrDefault() ?? new();
    foreach (List<NodeAnchor> neta in paths.Where(neta => neta.LastOrDefault().AccumulatedScore >
                                                          result.LastOrDefault().AccumulatedScore)) {
      result = neta;
    }

    return result;
  }
  // MARK: - Private Functions
  private void Build() {
    int itrBegin = _cursorIndex < MaxBuildSpanLength ? 0 : _cursorIndex - MaxBuildSpanLength;
    int itrEnd = Math.Min(_cursorIndex + MaxBuildSpanLength, Readings.Count);
    for (int p = itrBegin; p < itrEnd; p++) {
      for (int q = 1; q < MaxBuildSpanLength; q++) {
        if (p + q > itrEnd) break;
        List<string> arrSlice = Readings.GetRange(p, q);
        string combinedReading = Join(arrSlice, JoinSeparator);
        if (Grid.HasMatchedNode(p, q, combinedReading)) continue;
        List<Unigram> unigrams = _langModel.UnigramsFor(combinedReading);
        if (unigrams.Count == 0) continue;
        Node n = new(combinedReading, unigrams);
        Grid.InsertNode(n, p, q);
      }
    }
  }
  private static string Join(IEnumerable<string> slice, string separator) => string.Join(separator, slice.ToList());
}
}