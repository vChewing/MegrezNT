// CSharpened and further development by (c) 2022 and onwards The vChewing Project (MIT License).
// Was initially rebranded from (c) Lukhnos Liu's C++ library "Gramambular 2" (MIT License).
// ====================
// This code is released under the MIT license (SPDX-License-Identifier: MIT)

using System.Collections.Generic;
using System.Linq;

namespace Megrez {
public partial struct Compositor {
  /// <summary>
  /// 一個「有向無環圖的」的頂點單位。<para/>
  /// 這是一個可變的數據結構，用於有向無環圖的構建和單源最短路徑的計算。
  /// </summary>
  internal class Vertex {
    /// <summary>
    /// 前述頂點。
    /// </summary>
    public Vertex? Prev;
    /// <summary>
    /// 自身屬下的頂點陣列。
    /// </summary>
    public List<Vertex> Edges = new();
    /// <summary>
    /// 該變數用於最短路徑的計算。<para/>
    /// 我們實際上是在計算具有最大權重的路徑，因此距離的初始值是負無窮的。
    /// 如果我們要計算最短的權重/距離，我們會將其初期值設為正無窮。
    /// </summary>
    public double Distance = double.NegativeInfinity;
    /// <summary>
    /// 在進行進行位相幾何排序時會用到的狀態標記。
    /// </summary>
    public bool TopoSorted;
    /// <summary>
    /// 字詞節點。
    /// </summary>
    public Node Node;

    /// <summary>
    /// 初期化一個「有向無環圖的」的頂點單位。<para/>
    /// 這是一個可變的數據結構，用於有向無環圖的構建和單源最短路徑的計算。
    /// </summary>
    /// <param name="node">字詞節點。</param>
    public Vertex(Node node) { Node = node; }

    /// <summary>
    /// 讓一個 Vertex 順藤摸瓜地將自己的所有的連帶的 Vertex 都摧毀，再摧毀自己。
    /// 此過程必須在一套 Vertex 全部使用完畢之後執行一次，可防止記憶體洩漏。
    /// </summary>
    public void Destroy() {
      while (Prev?.Prev is not null) Prev?.Destroy();
      Prev = null;
      Edges.ForEach(delegate(Vertex edge) { edge.Destroy(); });
      Edges.Clear();
      Node = new(keyArray: new List<string>(), unigrams: new List<Unigram>(), spanLength: 0);
    }
  }

  /// <summary>
  /// 卸勁函式。<para/>
  /// 「卸勁 (relax)」一詞出自 Cormen 在 2001 年的著作「Introduction to Algorithms」的 585 頁。
  /// </summary>
  /// <param name="u">參照頂點，會在必要時成為 v 的前述頂點。</param>
  /// <param name="v">要影響的頂點。</param>
  internal void Relax(Vertex u, ref Vertex v) {
    // 從 u 到 w 的距離，也就是 v 的權重。
    double w = v.Node.Score;
    // 這裡計算最大權重：
    // 如果 v 目前的距離值小於「u 的距離值＋w（w 是 u 到 w 的距離，也就是 v 的權重）」，
    // 我們就更新 v 的距離及其前述頂點。
    if (v.Distance >= u.Distance + w) return;
    v.Distance = u.Distance + w;
    v.Prev = u;
  }

  /// <summary>
  /// 位相幾何排序處理時的處理狀態。
  /// </summary>
  internal class TopoSortState {
    public int IterIndex { get; set; }
    public Vertex Vertex { get; }
    public TopoSortState(Vertex vertex, int iterIndex = 0) {
      Vertex = vertex;
      IterIndex = iterIndex;
    }
  }

  /// <summary>
  /// 對持有單個根頂點的有向無環圖進行位相幾何排序（topological
  /// sort）、且將排序結果以頂點陣列的形式給出。<para/>
  /// 這裡使用我們自己的堆棧和狀態定義實現了一個非遞迴版本，
  /// 這樣我們就不會受到當前線程的堆棧大小的限制。以下是等價的原始算法。
  /// <code>
  ///  void TopoSort(vertex: Vertex) {
  ///    foreach (Vertex vertexNode in vertex.Edges) {
  ///      if (!vertexNode.TopoSorted) {
  ///        DFS(vertexNode, result);
  ///        vertexNode.TopoSorted = true;
  ///      }
  ///      result.Add(vertexNode);
  ///    }
  ///  }
  /// </code>
  /// 至於其遞迴版本，則類似於 Cormen 在 2001 年的著作「Introduction to Algorithms」當中的樣子。
  /// </summary>
  /// <param name="root">根頂點。</param>
  /// <returns>排序結果（頂點陣列）。</returns>
  internal List<Vertex> TopoSort(ref Vertex root) {
    List<Vertex> result = new();
    List<TopoSortState> stack = new();
    stack.Add(new(root));
    while (!stack.IsEmpty()) {
      TopoSortState state = stack.Last();
      Vertex theVertex = state.Vertex;
      if (state.IterIndex < state.Vertex.Edges.Count) {
        Vertex newVertex = state.Vertex.Edges[state.IterIndex];
        state.IterIndex += 1;
        if (!newVertex.TopoSorted) {
          stack.Add(new(newVertex));
          continue;
        }
      }
      theVertex.TopoSorted = true;
      result.Add(theVertex);
      stack.Remove(stack.Last());
    }
    return result;
  }
}
}  // namespace Megrez