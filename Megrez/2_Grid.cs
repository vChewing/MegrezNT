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
  private List<Span> MutSpans = new();
  /// <summary>
  /// 初期化轨格。
  /// </summary>
  /// <param name="SpanLength">該軌格內可以允許的最大幅位長度。</param>
  public Grid(int SpanLength = 10) { MutMaxBuildSpanLength = SpanLength; }
  /// <summary>
  /// 該幅位內可以允許的最大詞長。
  /// </summary>
  private int MutMaxBuildSpanLength;
  /// <summary>
  /// 公開：該軌格內可以允許的最大幅位長度。
  /// </summary>
  public int MaxBuildSpanLength => MutMaxBuildSpanLength;
  /// <summary>
  /// 公開：軌格的寬度，也就是其內的幅位陣列當中的幅位數量。
  /// </summary>
  public int Width => MutSpans.Count;
  /// <summary>
  /// 公開：軌格是否為空。
  /// </summary>
  public bool IsEmpty => MutSpans.Count == 0;
  /// <summary>
  /// 自我清空該軌格的內容。
  /// </summary>
  public void Clear() { MutSpans.Clear(); }
  /// <summary>
  /// 往該軌格的指定位置插入指定幅位長度的指定節點。
  /// </summary>
  /// <param name="Node">節點。</param>
  /// <param name="Location">位置。</param>
  /// <param name="SpanningLength">給定的幅位長度。</param>
  public void InsertNode(Node Node, int Location, int SpanningLength) {
    Location = Math.Abs(Location);
    SpanningLength = Math.Abs(SpanningLength);
    if (Location >= MutSpans.Count) {
      int Diff = Location - MutSpans.Count + 1;
      for (int I = 0; I < Diff; I++) {
        MutSpans.Add(new());
      }
    }
    Span SpanToDealWith = MutSpans[Location];
    SpanToDealWith.Insert(Node, SpanningLength);
    MutSpans[Location] = SpanToDealWith;
  }
  /// <summary>
  /// 給定索引鍵、位置、幅位長度，在該軌格內確認是否有對應的節點存在。
  /// </summary>
  /// <param name="Location">位置。</param>
  /// <param name="SpanningLength">給定的幅位長度。</param>
  /// <param name="Key">索引鍵。</param>
  /// <returns>回報存無狀態：存則真，無則假。</returns>
  public bool HasMatchedNode(int Location, int SpanningLength, string Key) {
    int TheLocation = Math.Abs(Location);
    int TheSpanningLength = Math.Abs(SpanningLength);
    if (TheLocation > MutSpans.Count) {
      return false;
    }
    Node? N = MutSpans[TheLocation].Node(TheSpanningLength);
    return (N != null) && (Key == N.Key);
  }
  /// <summary>
  /// 在該軌格的指定位置擴增一個幅位。
  /// </summary>
  /// <param name="Location">位置。</param>
  public void ExpandGridByOneAt(int Location) {
    int TheLocation = Math.Abs(Location);
    MutSpans.Insert(TheLocation, new());
    if ((TheLocation != 0) && (TheLocation != MutSpans.Count))
      for (int I = 0; I < TheLocation; I++) {
        Span TheSpan = MutSpans[I];
        TheSpan.RemoveNodeOfLengthGreaterThan(TheLocation - I);
        MutSpans[I] = TheSpan;
      }
  }
  /// <summary>
  /// 在該軌格的指定位置減少一個幅位。
  /// </summary>
  /// <param name="Location">位置。</param>
  public void ShrinkGridByOneAt(int Location) {
    Location = Math.Abs(Location);
    if (Location >= MutSpans.Count) return;
    MutSpans.RemoveAt(Location);
    for (int I = 0; I < Location; I++) {
      Span TheSpan = MutSpans[I];
      TheSpan.RemoveNodeOfLengthGreaterThan(Location - I);
      MutSpans[I] = TheSpan;
    }
  }
  /// <summary>
  /// 給定位置，枚舉出所有在這個位置開始的節點。
  /// </summary>
  /// <param name="Location">位置。</param>
  /// <returns>均有節點的節錨陣列。</returns>
  public List<NodeAnchor> NodesBeginningAt(int Location) {
    int TheLocation = Math.Abs(Location);
    List<NodeAnchor> Results = new();
    if (TheLocation < MutSpans.Count) {  // 此時 MutSpans 必定不為空
      Span Span = MutSpans[TheLocation];
      for (int I = 1; I <= MaxBuildSpanLength; I++) {
        Node? NP = Span.Node(I);
        if (NP != null) Results.Add(new(NP, TheLocation, I));
      }
    }
    return Results;
  }
  /// <summary>
  /// 給定位置，枚舉出所有在這個位置結尾的節點。
  /// </summary>
  /// <param name="Location">位置。</param>
  /// <returns>均有節點的節錨陣列。</returns>
  public List<NodeAnchor> NodesEndingAt(int Location) {
    int TheLocation = Math.Abs(Location);
    List<NodeAnchor> Results = new();
    if (MutSpans.Count != 0 && TheLocation <= MutSpans.Count) {
      for (int I = 0; I < TheLocation; I++) {
        Span Span = MutSpans[I];
        if (I + Span.MaximumLength >= TheLocation) {
          Node? NP = Span.Node(TheLocation - I);
          if (NP != null) Results.Add(new(NP, I, TheLocation - I));
        }
      }
    }
    return Results;
  }
  /// <summary>
  /// 給定位置，枚舉出所有在這個位置結尾、或者橫跨該位置的節點。
  /// </summary>
  /// <param name="Location">位置。</param>
  /// <returns>均有節點的節錨陣列。</returns>
  public List<NodeAnchor> NodesCrossingOrEndingAt(int Location) {
    int TheLocation = Math.Abs(Location);
    List<NodeAnchor> Results = new();
    if (MutSpans.Count != 0 && TheLocation <= MutSpans.Count) {
      for (int I = 0; I < TheLocation; I++) {
        Span Span = MutSpans[I];
        if (I + Span.MaximumLength >= TheLocation) {
          for (int J = 1; J <= Span.MaximumLength; J++) {
            if (I + J < Location) continue;
            Node? NP = Span.Node(J);
            if (NP != null) Results.Add(new(NP, I, TheLocation - I));
          }
        }
      }
    }
    return Results;
  }
  /// <summary>
  /// 將給定位置的節點的候選字詞改為與給定的字串一致的候選字詞。該函式可以僅用作過程函式。
  /// </summary>
  /// <param name="Location">位置。</param>
  /// <param name="Value">給定字串。</param>
  /// <returns>一個節錨，內容可能為空。該結果僅用作偵錯用途。</returns>
  public NodeAnchor FixNodeSelectedCandidate(int Location, string Value) {
    int TheLocation = Math.Abs(Location);
    NodeAnchor Node = new();
    foreach (NodeAnchor NodeAnchor in NodesCrossingOrEndingAt(TheLocation)) {
      Node? TheNode = NodeAnchor.Node;
      if (TheNode == null) continue;
      List<KeyValuePair> Candidates = TheNode.Candidates;
      // 將該位置的所有節點的候選字詞鎖定狀態全部重設。
      TheNode.ResetCandidate();
      int I = 0;
      foreach (KeyValuePair Candidate in Candidates) {
        if (Candidate.Value == Value) {
          TheNode.SelectCandidateAt(I);
          Node = NodeAnchor;
          break;
        }
        I += 1;
      }
    }
    return Node;
  }
  /// <summary>
  /// 將給定位置的節點的與給定的字串一致的候選字詞的權重複寫為給定權重數值。
  /// </summary>
  /// <param name="Location">位置。</param>
  /// <param name="Value">給定字串。</param>
  /// <param name="OverridingScore">給定權重數值。</param>
  public void FixNodeSelectedCandidate(int Location, string Value, double OverridingScore) {
    int TheLocation = Math.Abs(Location);
    foreach (NodeAnchor NodeAnchor in NodesCrossingOrEndingAt(TheLocation)) {
      Node? TheNode = NodeAnchor.Node;
      if (TheNode == null) continue;
      List<KeyValuePair> Candidates = TheNode.Candidates;
      // 將該位置的所有節點的候選字詞鎖定狀態全部重設。
      TheNode.ResetCandidate();
      int I = 0;
      foreach (KeyValuePair Candidate in Candidates) {
        if (Candidate.Value == Value) {
          TheNode.SelectFloatingCandidateAt(I, OverridingScore);
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
    string StrOutput = "digraph {\ngraph [ rankdir=LR ];\nBOS;\n";
    for (int P = 0; P < MutSpans.Count; P++) {
      Span Span = MutSpans[P];
      for (int NI = 0; NI <= Span.MaximumLength; NI++) {
        if (Span.Node(NI) == null) continue;
        Node NP = Span.Node(NI) ?? new("", new());
        if (P == 0) StrOutput += "BOS -> " + NP.CurrentKeyValue.Value + ";\n";
        StrOutput += NP.CurrentKeyValue.Value + ";\n";
        if ((P + NI) < MutSpans.Count) {
          Span DestinationSpan = MutSpans[P + NI];
          for (int Q = 0; Q <= DestinationSpan.MaximumLength; Q++) {
            if (DestinationSpan.Node(Q) != null) {
              Node DN = DestinationSpan.Node(Q) ?? new("", new());
              StrOutput += NP.CurrentKeyValue.Value + " -> " + DN.CurrentKeyValue.Value + ";\n";
            }
          }
        }
        if ((P + NI) == MutSpans.Count) StrOutput += NP.CurrentKeyValue.Value + " -> EOS;\n";
      }
    }
    StrOutput += "EOS;\n}\n";
    return StrOutput;
  }
}
}