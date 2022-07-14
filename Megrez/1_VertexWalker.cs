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
using System.Linq;

// NOTE: This file is optional. Its internal functions are not enabled yet and need to be fixed.

namespace Megrez {
/// <summary>
/// 一個「有向無環圖的」的頂點單位。<br />
/// 這是一個可變的數據結構，用於有向無環圖的構建和單源最短路徑的計算。
/// </summary>
class Vertex {
  /// <summary>
  /// 前述頂點。
  /// </summary>
  public Vertex? Prev;
  /// <summary>
  /// 自身屬下的頂點陣列。
  /// </summary>
  public List<Vertex> Edges = new();
  /// <summary>
  /// 該變數用於最短路徑的計算。<br />
  /// 我們實際上是在計算具有最大權重的路徑，因此距離的初始值是負無窮的。<br />
  /// 如果我們要計算最短的權重/距離，我們會將其初期值設為正無窮。
  /// </summary>
  public double Distance = double.MinValue;
  /// <summary>
  /// 在進行進行位相幾何排序時會用到的狀態標記。
  /// </summary>
  public bool TopologicallySorted;
  public Node Node;
  public Vertex(Node node) {
    Node = node;
    TopologicallySorted = false;
  }

  /// <summary>
  /// 卸勁函式。<br />
  /// 「卸勁 (relax)」一詞出自 Cormen 在 2001 年的著作「Introduction to Algorithms」的 585 頁。
  /// </summary>
  /// <param name="u">參照頂點，會在必要時成為 v 的前述頂點。</param>
  /// <param name="v">要影響的頂點。</param>
  public static Vertex Relax(Vertex u, Vertex v) {
    // 從 u 到 w 的距離，也就是 v 的權重。
    double w = v.Node.Score;
    // 這裡計算最大權重：
    // 如果 v 目前的距離值小於「u 的距離值＋w（w 是 u 到 w 的距離，也就是 v 的權重）」，
    // 我們就更新 v 的距離及其前述頂點。
    if (v.Distance < u.Distance + w) {
      v.Distance = u.Distance + w;
      v.Prev = u;
    }
    // C# 的 inout 函數某些情況下會讀不到參數指針，所以這裡直接改成回傳函數。
    return v;
  }

  private struct StateOfSort {
    public int EdgeIter;
    public Vertex Vertex;
    public Vertex EdgeItered => Vertex.Edges[EdgeIter];
    public StateOfSort(Vertex vertex) {
      Vertex = vertex;
      EdgeIter = 0;
    }
  }

  /// <summary>
  /// 對持有單個根頂點的有向無環圖進行位相幾何排序（topological
  /// sort）、且將排序結果以頂點陣列的形式給出。
  /// </summary>
  /// <remarks>
  /// 這裡使用我們自己的堆棧和狀態定義實現了一個非遞迴版本，
  /// 這樣我們就不會受到當前線程的堆棧大小的限制。
  /// <example>以下是等價的原始算法。
  /// <code>
  /// void TopologicalSort(Vertex vertex) {
  ///   (A list of Vertex) result = new();
  ///   foreach (var vertexNode in vertex.Edges) {
  ///     if (!vertexNode.TopologicallySorted) {
  ///       DFS(vertexNode, result);
  ///       vertexNode.TopologicallySorted = true;
  ///     }
  ///     result.Add(v);
  ///   }
  /// }
  /// </code></example>
  /// 至於遞迴版本則類似於 Cormen 在 2001 年的著作「Introduction to Algorithms」當中的樣子。
  /// </remarks>
  /// <param name="root">根頂點。</param>
  /// <returns>排序結果（頂點陣列）。</returns>
  public static List<Vertex> TopologicalSort(Vertex root) {
    List<Vertex> result = new();
    List<StateOfSort> theStack = new() { new(root) };
    while (theStack.Last() is var state) {
      if (state.EdgeIter != state.Vertex.Edges.Count) {
        Vertex nextVertex = state.EdgeItered;
        state.EdgeIter += 1;
        if (!nextVertex.TopologicallySorted) {
          theStack.Add(new(nextVertex));
          continue;
        }
      }
      state.Vertex.TopologicallySorted = true;
      result.Add(state.Vertex);
      theStack.RemoveAt(theStack.Count - 1);
      if (theStack.Count == 0) break;  // C# 需要這句處理，不然一直鬼打牆。
    }
    return result;
  }
}

public partial class Compositor {
  /// <summary>
  /// 爬軌結果。
  /// </summary>
  public struct WalkResult {
    /// <summary>
    /// 爬軌結果。
    /// </summary>
    /// <param name="nodes">爬軌結果內的節點陣列。</param>
    /// <param name="vertices">統計爬軌過程當中牽涉的頂點數量。</param>
    /// <param name="edges">統計爬軌過程當中牽涉的頂點邊界數量。</param>
    public WalkResult(List<Node> nodes, int vertices = 0, int edges = 0) {
      Nodes = nodes;
      Vertices = vertices;
      Edges = edges;
    }
    /// <summary>
    /// 爬軌結果內的節點陣列。
    /// </summary>
    public List<Node> Nodes;
    /// <summary>
    /// 統計爬軌過程當中牽涉的頂點數量。
    /// </summary>
    public int Vertices;
    /// <summary>
    /// 統計爬軌過程當中牽涉的頂點邊界數量。
    /// </summary>
    public int Edges;
    /// <summary>
    /// 獲取爬軌結果當中的資料值陣列。
    /// </summary>
    /// <returns>資料值陣列。</returns>
    public string[] Values() => Nodes.Select(x => x.CurrentPair.Value).ToArray();
    /// <summary>
    /// 獲取爬軌結果當中的索引鍵陣列。
    /// </summary>
    /// <returns>索引鍵陣列。</returns>
    public string[] Keys() => Nodes.Select(x => x.CurrentPair.Key).ToArray();
  }

  /// <summary>
  /// 對已給定的軌格，使用頂點算法，按照給定的位置與條件進行正向爬軌。
  /// </summary>
  /// <remarks>⚠︎ 該方法有已知問題，會無視 fixNodeWithCandidate() 的前置操作效果。</remarks>
  /// <returns>一個包含有效結果的節錨陣列。</returns>
  public List<NodeAnchor> FastWalk() {
    VertexWalk();
    UpdateCursorJumpingTables(WalkedAnchors);
    return WalkedAnchors;
  }

  /// <summary>
  /// 找到軌格陣圖內權重最大的路徑。該路徑代表了可被觀測到的最可能的隱藏事件鏈。
  /// 這裡使用 Cormen 在 2001 年出版的教材當中提出的「有向無環圖的最短路徑」的
  /// 算法來計算這種路徑。不過，這裡不是要計算距離最短的路徑，而是計算距離最長
  /// 的路徑（所以要找最大的權重），因為在對數概率下，較大的數值意味著較大的概率。
  /// 對於 <c>G = (V, E)</c>，該算法的運行次數為 <c>O(|V|+|E|)</c>，其中 <c>G</c> 是一個有向無環圖。
  /// 這意味著，即使軌格很大，也可以用很少的算力就可以爬軌。
  /// </summary>
  /// <returns>爬軌結果＋該過程是否順利執行。</returns>
  private Tuple<WalkResult, bool> VertexWalk() {
    WalkResult result = new(nodes: new(), vertices: 0, edges: 0);
    if (Spans.Count == 0) {
      UpdateWalkedAnchors(new());
      return new(result, true);
    }
    List<List<Vertex>> vertexSpans = Spans
                                         .Select(
                                             _ => new List<Vertex>())
                                         .ToList();

    foreach (var neta in Spans.Select((span, i) => new { i, span })) {
      for (int j = 1; j <= neta.span.MaxLength; j += 1) {
        if (neta.span.NodeOf(j) is not {} p) continue;
        vertexSpans[neta.i].Add(new(node: p));
        result.Vertices += 1;
      }
    }

    Vertex terminal = new(node: new(key: "_TERMINAL_"));

    foreach (var ii in vertexSpans.Select((item, index) => new { index, item })) {
      int i = ii.index;
      List<Vertex> vertexSpan = ii.item;
      foreach (Vertex vertex in vertexSpan) {
        int nextVertexPosition = i + vertex.Node.SpanLength;
        if (nextVertexPosition == vertexSpans.Count) {
          vertex.Edges.Add(terminal);
          continue;
        }
        foreach (Vertex nextVertex in vertexSpans[nextVertexPosition]) {
          vertex.Edges.Add(nextVertex);
          result.Edges += 1;
        }
      }
    }

    Vertex root = new(node: new(key: "_ROOT_")) { Distance = 0 };
    root.Edges.AddRange(vertexSpans[0]);

    List<Vertex> ordered = Vertex.TopologicalSort(root);

    foreach (Vertex t in ordered) {
      for (int k = 0; k < t.Edges.Count; k++) {
        t.Edges[k] = Vertex.Relax(t, t.Edges[k]);
      }
    }

    // 接下來這段處理可能有問題需要修正。
    List<Node> walked = new();
    int totalReadingLength = 0;
    List<Vertex> orderedReversed = ordered;
    orderedReversed.Reverse();
    while (totalReadingLength < Readings.Count + 2 && orderedReversed[totalReadingLength].Edges.Last() is {} lastEdge) {
      if (lastEdge.Prev is {} vertexPrev && !string.IsNullOrEmpty(vertexPrev.Node.CurrentPair.Value))
        walked.Add(vertexPrev.Node);
      else if (!string.IsNullOrEmpty(lastEdge.Node.CurrentPair.Value))
        walked.Add(lastEdge.Node);
      int oldTotalReadingLength = totalReadingLength;
      totalReadingLength += lastEdge.Node.SpanLength;
      if (oldTotalReadingLength == totalReadingLength) {
        break;
      }
    }

    if (totalReadingLength != Readings.Count) {
      Console.WriteLine($"!!! Error A: readingLength: {totalReadingLength}, readingCount: {Readings.Count}");
      UpdateWalkedAnchors(new());
      return new(result, false);
    }

    result.Nodes = walked;
    UpdateWalkedAnchors(result.Nodes);
    return new(result, true);
  }
}
}
