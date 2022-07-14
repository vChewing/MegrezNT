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
/// 軌格，會被組字器作為原始型別來繼承。
/// </summary>
public abstract class Grid {
  /// <summary>
  /// 軌格增減行為。
  /// </summary>
  public enum ResizeBehavior {
    /// <summary>
    /// 擴增。
    /// </summary>
    Expand,
    /// <summary>
    /// 縮減。
    /// </summary>
    Shrink
  }
  /// <summary>
  /// 幅位陣列。
  /// </summary>
  public List<SpanUnit> Spans { get; private set; } = new();
  /// <summary>
  /// 初期化轨格。
  /// </summary>
  /// <param name="spanLengthLimit">該軌格內可以允許的最大幅位長度。</param>
  public Grid(int spanLengthLimit = 10) { MaxBuildSpanLength = spanLengthLimit; }
  /// <summary>
  /// 公開：該組字器軌格內可以允許的最大幅位長度。
  /// </summary>
  public int MaxBuildSpanLength { get; }
  /// <summary>
  /// 公開：軌格的寬度，也就是其內的幅位陣列當中的幅位數量。
  /// </summary>
  public int Width => Spans.Count;
  /// <summary>
  /// 公開：該組字器軌格是否為空。
  /// </summary>
  public bool IsEmpty => Spans.Count == 0;
  /// <summary>
  /// 自我清空該軌格的內容。
  /// </summary>
  public virtual void Clear() { Spans.Clear(); }
  /// <summary>
  /// 往該軌格的指定位置插入指定幅位長度的指定節點。
  /// </summary>
  /// <param name="node">節點。</param>
  /// <param name="location">位置。</param>
  /// <param name="spanLength">給定的幅位長度。</param>
  public void InsertNode(Node node, int location, int spanLength) {
    location = Math.Abs(location);
    spanLength = Math.Abs(spanLength);
    if (location >= Spans.Count) {
      int diff = location - Spans.Count + 1;
      for (int I = 0; I < diff; I++) {
        Spans.Add(new());
      }
    }
    SpanUnit spanToDealWith = Spans[location];
    spanToDealWith.Insert(node, spanLength);
    Spans[location] = spanToDealWith;
  }
  /// <summary>
  /// 給定索引鍵、位置、幅位長度，在該軌格內確認是否有對應的節點存在。
  /// </summary>
  /// <param name="location">位置。</param>
  /// <param name="spanningLength">給定的幅位長度。</param>
  /// <param name="key">索引鍵。</param>
  /// <returns>回報存無狀態：存則真，無則假。</returns>
  public bool HasMatchedNode(int location, int spanningLength, string key) {
    int theLocation = Math.Abs(location);
    int theSpanningLength = Math.Abs(spanningLength);
    if (theLocation > Spans.Count) {
      return false;
    }
    Node? n = Spans[theLocation].NodeOf(theSpanningLength);
    return n != null && key == n.Key;
  }
  /// <summary>
  /// 在該軌格的指定位置擴增或減少一個幅位。
  /// </summary>
  /// <param name="location">位置。</param>
  /// <param name="behavior">決定是新增還是減少一個幅位。</param>
  public void ResizeGridByOneAt(int location, ResizeBehavior behavior) {
    location = Math.Max(0, Math.Min(Width, location));
    switch (behavior) {
      case ResizeBehavior.Expand:
        Spans.Insert(location, new());
        if (location == 0 || location == Spans.Count) return;
        break;
      case ResizeBehavior.Shrink:
        if (location >= Spans.Count) return;
        Spans.RemoveAt(location);
        break;
    }
    for (int i = 0; i < location; i++) {
      // 處理掉被損毀的或者重複的幅位。
      SpanUnit theSpan = Spans[i];
      theSpan.DropNodesBeyond(location - i);
      Spans[i] = theSpan;
    }
  }

  /// <summary>
  /// 給定位置，枚舉出所有在這個位置開始的節點。
  /// </summary>
  /// <param name="location">位置。</param>
  /// <returns>均有節點的節錨陣列。</returns>
  public List<NodeAnchor> NodesBeginningAt(int location) {
    int theLocation = Math.Abs(location);
    List<NodeAnchor> results = new();
    if (theLocation >= Spans.Count) return results;  // 此時 MutSpans 必定不為空
    SpanUnit span = Spans[theLocation];
    for (int I = 1; I <= MaxBuildSpanLength; I++) {
      Node? np = span.NodeOf(I);
      if (np != null) results.Add(new(np));
    }
    return results;
  }
  /// <summary>
  /// 給定位置，枚舉出所有在這個位置結尾的節點。
  /// </summary>
  /// <param name="location">位置。</param>
  /// <returns>均有節點的節錨陣列。</returns>
  public List<NodeAnchor> NodesEndingAt(int location) {
    int theLocation = Math.Abs(location);
    List<NodeAnchor> results = new();
    if (Spans.Count == 0 || theLocation > Spans.Count) return results;
    for (int I = 0; I < theLocation; I++) {
      SpanUnit span = Spans[I];
      if (I + span.MaxLength < theLocation) continue;
      Node? np = span.NodeOf(theLocation - I);
      if (np != null) results.Add(new(np));
    }
    return results;
  }
  /// <summary>
  /// 給定位置，枚舉出所有在這個位置結尾、或者橫跨該位置的節點。
  /// </summary>
  /// <param name="location">位置。</param>
  /// <returns>均有節點的節錨陣列。</returns>
  public List<NodeAnchor> NodesCrossingOrEndingAt(int location) {
    int theLocation = Math.Abs(location);
    List<NodeAnchor> results = new();
    if (Spans.Count == 0 || theLocation > Spans.Count) return results;
    for (int I = 0; I < theLocation; I++) {
      SpanUnit span = Spans[I];
      if (I + span.MaxLength < theLocation) continue;
      for (int j = 1; j <= span.MaxLength; j++) {
        if (I + j < location) continue;
        Node? np = span.NodeOf(j);
        if (np != null) results.Add(new(np));
      }
    }
    return results;
  }
  /// <summary>
  /// 給定位置，枚舉出所有在這個位置結尾或開頭或者橫跨該位置的節點。<br />
  /// ⚠︎ 注意：排序可能失真。
  /// </summary>
  /// <param name="location">位置。</param>
  /// <returns>均有節點的節錨陣列。</returns>
  public List<NodeAnchor> NodesOverlappedAt(int location) {
    List<NodeAnchor> x = NodesBeginningAt(location);
    x.AddRange(NodesCrossingOrEndingAt(location));
    return x.Distinct().ToList();
  }
  /// <summary>
  /// 將給定位置的節點的候選字詞改為與給定的字串一致的候選字詞。該函式可以僅用作過程函式。
  /// </summary>
  /// <param name="location">位置。</param>
  /// <param name="value">給定字串。</param>
  /// <returns>一個節錨，內容可能為空。該結果僅用作偵錯用途。</returns>
  public NodeAnchor FixNodeWithCandidateLiteral(string value, int location) {
    int theLocation = Math.Abs(location);
    NodeAnchor node = new();
    foreach (NodeAnchor nodeAnchor in NodesCrossingOrEndingAt(theLocation)) {
      Node theNode = nodeAnchor.Node;
      List<KeyValuePaired> candidates = theNode.Candidates;
      // 將該位置的所有節點的候選字詞鎖定狀態全部重設。
      theNode.ResetCandidate();
      int I = 0;
      foreach (KeyValuePaired candidate in candidates) {
        if (candidate.Value == value) {
          theNode.SelectCandidateAt(I);
          node = nodeAnchor;
          break;
        }
        I += 1;
      }
    }
    return node;
  }
  /// <summary>
  /// 將給定位置的節點的候選字詞改為與給定的字串一致的候選字詞。該函式可以僅用作過程函式。
  /// </summary>
  /// <param name="location">位置。</param>
  /// <param name="value">給定字串。</param>
  /// <returns>一個節錨，內容可能為空。該結果僅用作偵錯用途。</returns>
  public NodeAnchor FixNodeWithCandidate(KeyValuePaired value, int location) {
    int theLocation = Math.Abs(location);
    NodeAnchor node = new();
    foreach (NodeAnchor nodeAnchor in NodesCrossingOrEndingAt(theLocation)) {
      Node theNode = nodeAnchor.Node;
      List<KeyValuePaired> candidates = theNode.Candidates;
      // 將該位置的所有節點的候選字詞鎖定狀態全部重設。
      theNode.ResetCandidate();
      int I = 0;
      foreach (KeyValuePaired candidate in candidates) {
        if (candidate == value) {
          theNode.SelectCandidateAt(I);
          node = nodeAnchor;
          break;
        }
        I += 1;
      }
    }
    return node;
  }
  /// <summary>
  /// 將給定位置的節點的與給定的字串一致的候選字詞的權重複寫為給定權重數值。
  /// </summary>
  /// <param name="location">位置。</param>
  /// <param name="value">給定字串。</param>
  /// <param name="overridingScore">給定權重數值。</param>
  public void OverrideNodeScoreForSelectedCandidate(int location, string value, double overridingScore) {
    int theLocation = Math.Abs(location);
    foreach (NodeAnchor nodeAnchor in NodesCrossingOrEndingAt(theLocation)) {
      Node theNode = nodeAnchor.Node;
      List<KeyValuePaired> candidates = theNode.Candidates;
      // 將該位置的所有節點的候選字詞鎖定狀態全部重設。
      theNode.ResetCandidate();
      int I = 0;
      foreach (KeyValuePaired candidate in candidates) {
        if (candidate.Value == value) {
          theNode.SelectFloatingCandidateAt(I, overridingScore);
          break;
        }
        I += 1;
      }
    }
  }
  /// <summary>
  /// 生成用以交給 GraphViz 診斷的資料。
  /// </summary>
  /// <returns>GraphViz 檔案內容，純文字。</returns>
  public string DumpDOT() {
    string strOutput = "digraph {\ngraph [ rankdir=LR ];\nBOS;\n";
    for (int p = 0; p < Spans.Count; p++) {
      SpanUnit span = Spans[p];
      for (int ni = 0; ni <= span.MaxLength; ni++) {
        if (span.NodeOf(ni) == null) continue;
        Node np = span.NodeOf(ni) ?? new();
        if (p == 0) strOutput += "BOS -> " + np.CurrentPair.Value + ";\n";
        strOutput += np.CurrentPair.Value + ";\n";
        if (p + ni < Spans.Count) {
          SpanUnit destinationSpan = Spans[p + ni];
          for (int q = 0; q <= destinationSpan.MaxLength; q++) {
            if (destinationSpan.NodeOf(q) == null) continue;
            Node dn = destinationSpan.NodeOf(q) ?? new();
            strOutput += np.CurrentPair.Value + " -> " + dn.CurrentPair.Value + ";\n";
          }
        }
        if (p + ni == Spans.Count) strOutput += np.CurrentPair.Value + " -> EOS;\n";
      }
    }
    strOutput += "EOS;\n}\n";
    return strOutput;
  }
}
}