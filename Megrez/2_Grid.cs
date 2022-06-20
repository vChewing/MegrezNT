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
using System.Collections.Generic;

namespace Megrez {
/// <summary>
/// 軌格。
/// </summary>
public class Grid {
  /// <summary>
  /// 幅位陣列。
  /// </summary>
  private List<Span> _spans = new();
  /// <summary>
  /// 初期化轨格。
  /// </summary>
  /// <param name="spanLength">該軌格內可以允許的最大幅位長度。</param>
  public Grid(int spanLength = 10) { MaxBuildSpanLength = spanLength; }
  /// <summary>
  /// 公開：該軌格內可以允許的最大幅位長度。
  /// </summary>
  public int MaxBuildSpanLength { get; }
  /// <summary>
  /// 公開：軌格的寬度，也就是其內的幅位陣列當中的幅位數量。
  /// </summary>
  public int Width => _spans.Count;
  /// <summary>
  /// 公開：軌格是否為空。
  /// </summary>
  public bool IsEmpty => _spans.Count == 0;
  /// <summary>
  /// 自我清空該軌格的內容。
  /// </summary>
  public void Clear() { _spans.Clear(); }
  /// <summary>
  /// 往該軌格的指定位置插入指定幅位長度的指定節點。
  /// </summary>
  /// <param name="node">節點。</param>
  /// <param name="location">位置。</param>
  /// <param name="spanningLength">給定的幅位長度。</param>
  public void InsertNode(Node node, int location, int spanningLength) {
    location = Math.Abs(location);
    spanningLength = Math.Abs(spanningLength);
    if (location >= _spans.Count) {
      int diff = location - _spans.Count + 1;
      for (int I = 0; I < diff; I++) {
        _spans.Add(new());
      }
    }
    Span spanToDealWith = _spans[location];
    spanToDealWith.Insert(node, spanningLength);
    _spans[location] = spanToDealWith;
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
    if (theLocation > _spans.Count) {
      return false;
    }
    Node? n = _spans[theLocation].Node(theSpanningLength);
    return n != null && key == n.Key;
  }
  /// <summary>
  /// 在該軌格的指定位置擴增一個幅位。
  /// </summary>
  /// <param name="location">位置。</param>
  public void ExpandGridByOneAt(int location) {
    int theLocation = Math.Abs(location);
    _spans.Insert(theLocation, new());
    if (theLocation == 0 || theLocation == _spans.Count) return;
    for (int I = 0; I < theLocation; I++) {
      Span theSpan = _spans[I];
      theSpan.RemoveNodeOfLengthGreaterThan(theLocation - I);
      _spans[I] = theSpan;
    }
  }
  /// <summary>
  /// 在該軌格的指定位置減少一個幅位。
  /// </summary>
  /// <param name="location">位置。</param>
  public void ShrinkGridByOneAt(int location) {
    location = Math.Abs(location);
    if (location >= _spans.Count) return;
    _spans.RemoveAt(location);
    for (int I = 0; I < location; I++) {
      Span theSpan = _spans[I];
      theSpan.RemoveNodeOfLengthGreaterThan(location - I);
      _spans[I] = theSpan;
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
    if (theLocation >= _spans.Count) return results;  // 此時 MutSpans 必定不為空
    Span span = _spans[theLocation];
    for (int I = 1; I <= MaxBuildSpanLength; I++) {
      Node? np = span.Node(I);
      if (np != null) results.Add(new(np, theLocation, I));
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
    if (_spans.Count == 0 || theLocation > _spans.Count) return results;
    for (int I = 0; I < theLocation; I++) {
      Span span = _spans[I];
      if (I + span.MaximumLength < theLocation) continue;
      Node? np = span.Node(theLocation - I);
      if (np != null) results.Add(new(np, I, theLocation - I));
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
    if (_spans.Count == 0 || theLocation > _spans.Count) return results;
    for (int I = 0; I < theLocation; I++) {
      Span span = _spans[I];
      if (I + span.MaximumLength < theLocation) continue;
      for (int j = 1; j <= span.MaximumLength; j++) {
        if (I + j < location) continue;
        Node? np = span.Node(j);
        if (np != null) results.Add(new(np, I, theLocation - I));
      }
    }
    return results;
  }
  /// <summary>
  /// 將給定位置的節點的候選字詞改為與給定的字串一致的候選字詞。該函式可以僅用作過程函式。
  /// </summary>
  /// <param name="location">位置。</param>
  /// <param name="value">給定字串。</param>
  /// <returns>一個節錨，內容可能為空。該結果僅用作偵錯用途。</returns>
  public NodeAnchor FixNodeSelectedCandidate(int location, string value) {
    int theLocation = Math.Abs(location);
    NodeAnchor node = new();
    foreach (NodeAnchor nodeAnchor in NodesCrossingOrEndingAt(theLocation)) {
      Node? theNode = nodeAnchor.Node;
      if (theNode == null) continue;
      List<KeyValuePair> candidates = theNode.Candidates;
      // 將該位置的所有節點的候選字詞鎖定狀態全部重設。
      theNode.ResetCandidate();
      int I = 0;
      foreach (KeyValuePair candidate in candidates) {
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
  /// 將給定位置的節點的與給定的字串一致的候選字詞的權重複寫為給定權重數值。
  /// </summary>
  /// <param name="location">位置。</param>
  /// <param name="value">給定字串。</param>
  /// <param name="overridingScore">給定權重數值。</param>
  public void FixNodeSelectedCandidate(int location, string value, double overridingScore) {
    int theLocation = Math.Abs(location);
    foreach (NodeAnchor nodeAnchor in NodesCrossingOrEndingAt(theLocation)) {
      Node? theNode = nodeAnchor.Node;
      if (theNode == null) continue;
      List<KeyValuePair> candidates = theNode.Candidates;
      // 將該位置的所有節點的候選字詞鎖定狀態全部重設。
      theNode.ResetCandidate();
      int I = 0;
      foreach (KeyValuePair candidate in candidates) {
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
  public string DumpDot() {
    string strOutput = "digraph {\ngraph [ rankdir=LR ];\nBOS;\n";
    for (int p = 0; p < _spans.Count; p++) {
      Span span = _spans[p];
      for (int ni = 0; ni <= span.MaximumLength; ni++) {
        if (span.Node(ni) == null) continue;
        Node np = span.Node(ni) ?? new("", new());
        if (p == 0) strOutput += "BOS -> " + np.CurrentKeyValue.Value + ";\n";
        strOutput += np.CurrentKeyValue.Value + ";\n";
        if (p + ni < _spans.Count) {
          Span destinationSpan = _spans[p + ni];
          for (int q = 0; q <= destinationSpan.MaximumLength; q++) {
            if (destinationSpan.Node(q) == null) continue;
            Node dn = destinationSpan.Node(q) ?? new("", new());
            strOutput += np.CurrentKeyValue.Value + " -> " + dn.CurrentKeyValue.Value + ";\n";
          }
        }
        if (p + ni == _spans.Count) strOutput += np.CurrentKeyValue.Value + " -> EOS;\n";
      }
    }
    strOutput += "EOS;\n}\n";
    return strOutput;
  }
}
}