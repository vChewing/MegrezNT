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
      List<Dictionary<int, Vertex>> vertexSpans = Spans.Select(span => span.ToVertexSpan()).ToList();

      Vertex terminal = new(node: new(new() { "_TERMINAL_" }, spanLength: 0, unigrams: new()));
      Vertex root = new(node: new(new() { "_ROOT_" }, spanLength: 0, unigrams: new())) { Distance = 0 };

      vertexSpans.Enumerated().ToList().ForEach(spanNeta => {
        (int location, Dictionary<int, Vertex> vertexSpan) = spanNeta;
        vertexSpan.Values.ToList().ForEach(vertex => {
          int nextVertexPosition = location + vertex.Node.SpanLength;
          if (nextVertexPosition == vertexSpans.Count) {
            vertex.Edges.Add(terminal);
            return;
          }
          vertexSpans[nextVertexPosition].Values.ToList().ForEach(nextVertex => { vertex.Edges.Add(nextVertex); });
        });
      });

      root.Edges.AddRange(vertexSpans.First().Values);

      TopoSort(ref root).Reversed().ForEach(neta => {
        neta.Edges.Enumerated().ToList().ForEach(edge => {
          if (neta.Edges[edge.index] is {} relaxV) neta.Relax(ref relaxV);
        });
      });

      Vertex iterated = terminal;
      List<Node> walked = new();
      int totalLengthOfKeys = 0;

      while (iterated.Prev is {} itPrev) {
        walked.Add(itPrev.Node);
        iterated = itPrev;
        totalLengthOfKeys += iterated.Node.SpanLength;
      }

      // 清理內容，否則會有記憶體洩漏。
      vertexSpans.Clear();
      iterated.Destroy();
      root.Destroy();
      terminal.Destroy();

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

  public partial struct SpanUnit {
    /// <summary>
    /// 將當前幅位單元由節點辭典轉為頂點辭典。
    /// </summary>
    /// <returns>當前幅位單元（頂點辭典）。</returns>
    internal Dictionary<int, Vertex> ToVertexSpan() {
      Dictionary<int, Vertex> result = new();
      Nodes.ToList().ForEach(neta => { result[neta.Key] = new(node: neta.Value); });
      return result;
    }
  }
}
}