// CSharpened and further development by (c) 2022 and onwards The vChewing Project (MIT License).
// Was initially rebranded from (c) Lukhnos Liu's C++ library "Gramambular 2" (MIT License).
// ====================
// This code is released under the MIT license (SPDX-License-Identifier: MIT)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Megrez {
public partial struct Compositor {
  /// <summary>
  /// 爬軌函式，會更新當前組字器的 <see cref="WalkedNodes"/>。<para/>
  /// 找到軌格陣圖內權重最大的路徑。該路徑代表了可被觀測到的最可能的隱藏事件鏈。
  /// 這裡使用 Cormen 在 2001 年出版的教材當中提出的「有向無環圖的最短路徑」的
  /// 算法來計算這種路徑。不過，這裡不是要計算距離最短的路徑，而是計算距離最長
  /// 的路徑（所以要找最大的權重），因為在對數概率下，較大的數值意味著較大的概率。
  /// 對於 <c>G = (V, E)</c>，該算法的運行次數為 <c>O(|V|+|E|)</c>，其中 <c>G</c>
  /// 是一個有向無環圖。這意味著，即使軌格很大，也可以用很少的算力就可以爬軌。
  /// </summary>
  /// <returns>爬軌結果＋該過程是否順利執行。</returns>
  public (List<Node> WalkedNodes, bool Succeeded) Walk() {
    List<Node> result = new();
    try {
      if (Spans.IsEmpty()) return (result, true);
      List<List<Vertex>> vertexSpans = new();
      foreach (SpanUnit _ in Spans) vertexSpans.Add(new());
      foreach ((int i, SpanUnit span) in Spans.Enumerated()) {
        foreach (int j in new BRange(1, Math.Max(1, span.MaxLength) + 1)) {
          if (span.NodeOf(j) is not {} theNode) continue;
          vertexSpans[i].Add(new(theNode));
        }
      }

      Vertex terminal = new(node: new(new() { "_TERMINAL_" }, spanLength: 0, unigrams: new()));

      foreach ((int i, List<Vertex> vertexSpan) in vertexSpans.Enumerated()) {
        foreach (Vertex vertex in vertexSpan) {
          int nextVertexPosition = i + vertex.Node.SpanLength;
          if (nextVertexPosition == vertexSpans.Count) {
            vertex.Edges.Add(terminal);
            continue;
          }
          foreach (Vertex nextVertex in vertexSpans[nextVertexPosition]) {
            vertex.Edges.Add(nextVertex);
          }
        }
      }

      Vertex root = new(node: new(new() { "_ROOT_" }, spanLength: 0, unigrams: new())) { Distance = 0 };
      root.Edges.AddRange(vertexSpans.First());

      List<Vertex> ordered = TopoSort(ref root);
      foreach ((int j, Vertex neta) in ordered.Reversed().Enumerated()) {
        foreach ((int k, _) in neta.Edges.Enumerated()) {
          if (neta.Edges[k] is {} relaxV) Relax(neta, ref relaxV);
        }
        ordered[j] = neta;
      }

      List<Node> walked = new();
      int totalLengthOfKeys = 0;
      Vertex iterated = terminal;
      while (iterated.Prev is {} itPrev) {
        walked.Add(itPrev.Node);
        iterated = itPrev;
        totalLengthOfKeys += iterated.Node.SpanLength;
      }

      if (totalLengthOfKeys != Keys.Count) {
        Console.WriteLine("!!! MEGREZ ERROR A.");
        return (result, false);
      }

      if (walked.Count < 2) {
        Console.WriteLine("!!! MEGREZ ERROR B.");
        return (result, false);
      }

      walked.Remove(walked.Last());
      result = walked.Reversed();
      return (result, true);
    } finally {
      WalkedNodes = result;
    }
  }
}
}