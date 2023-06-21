// CSharpened and further development by (c) 2022 and onwards The vChewing Project (MIT License).
// Was initially rebranded from (c) Lukhnos Liu's C++ library "Gramambular 2" (MIT License).
// ====================
// This code is released under the MIT license (SPDX-License-Identifier: MIT)

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
  /// <remarks>
  /// 利用該數學方法進行輸入法智能組句的（已知可考的）最開始的案例是郭家寶（ByVoid）
  /// 的《<a href="https://byvoid.com/zht/blog/slm_based_pinyin_ime/">基於統計語言模型的拼音輸入法</a>》；
  /// 再後來則是 2022 年中時期劉燈的 Gramambular 2 組字引擎。
  /// </remarks>
  /// </summary>
  /// <returns>爬軌結果＋該過程是否順利執行。</returns>
  public List<Node> Walk() {
    List<Node> result = new();
    try {
      WalkedNodes.Clear();
      SortAndRelax();
      if (Spans.IsEmpty()) return result;
      Node iterated = Node.LeadingNode;
      while (iterated.Prev is {} itPrev) {
        WalkedNodes.Insert(0, itPrev.Copy());
        iterated = itPrev;
      }
      iterated.DestroyVertex();
      WalkedNodes.RemoveAt(0);
      return WalkedNodes;
    } finally {
      ReinitVertexNetwork();
    }
  }

  /// 先進行位相幾何排序、再卸勁。
  internal void SortAndRelax() {
    ReinitVertexNetwork();
    List<SpanUnit> theSpans = Spans;
    if (theSpans.IsEmpty()) return;
    Node.TrailingNode.Distance = 0;
    theSpans.Enumerated().ToList().ForEach(spanNeta => {
      (int location, SpanUnit vertexSpan) = spanNeta;
      vertexSpan.Nodes.Values.ToList().ForEach(node => {
        int nextVertexPosition = location + node.SpanLength;
        if (nextVertexPosition == theSpans.Count) {
          node.Edges.Add(Node.LeadingNode);
          return;
        }
        theSpans[nextVertexPosition].Nodes.Values.ToList().ForEach(nextVertex => { node.Edges.Add(nextVertex); });
      });
    });

    Node.TrailingNode.Edges.AddRange(Spans.First().Nodes.Values);

    TopoSort().Reversed().ForEach(neta => {
      neta.Edges.Enumerated().ToList().ForEach(edge => {
        if (neta.Edges[edge.index] is {} relaxV) Relax(neta, ref relaxV);
      });
    });
  }

  /// <summary>
  /// 摧毀所有與共用起始虛擬節點有牽涉的節點自身的 Vertex 特性資料。
  /// </summary>
  internal static void ReinitVertexNetwork() {
    Node.TrailingNode.DestroyVertex();
    Node.LeadingNode.DestroyVertex();
  }

  /// <summary>
  /// 位相幾何排序處理時的處理狀態。
  /// </summary>
  private class TopoSortState {
    public int IterIndex { get; set; }
    public Node Node { get; }
    public TopoSortState(Node node, int iterIndex = 0) {
      Node = node;
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
  ///    vertex.Edges.ForEach ((x) => {
  ///      if (!vertexNode.TopoSorted) {
  ///        DFS(vertexNode, result);
  ///        vertexNode.TopoSorted = true;
  ///      }
  ///      result.Add(vertexNode);
  ///    });
  ///  }
  /// </code>
  /// 至於其遞迴版本，則類似於 Cormen 在 2001 年的著作「Introduction to Algorithms」當中的樣子。
  /// </summary>
  /// <returns>排序結果（頂點陣列）。</returns>
  private List<Node> TopoSort() {
    List<Node> result = new();
    List<TopoSortState> stack = new();
    stack.Add(new(Node.TrailingNode));
    while (!stack.IsEmpty()) {
      TopoSortState state = stack.Last();
      Node theNode = state.Node;
      if (state.IterIndex < state.Node.Edges.Count) {
        Node newNode = state.Node.Edges[state.IterIndex];
        state.IterIndex += 1;
        if (!newNode.TopoSorted) {
          stack.Add(new(newNode));
          continue;
        }
      }
      theNode.TopoSorted = true;
      result.Add(theNode);
      stack.Remove(stack.Last());
    }
    return result;
  }

  /// <summary>
  /// 卸勁函式。<para/>
  /// 「卸勁 (relax)」一詞出自 Cormen 在 2001 年的著作「Introduction to Algorithms」的 585 頁。
  /// </summary>
  /// <remarks>自己就是參照頂點 (u)，會在必要時成為 v (v) 的前述頂點。</remarks>
  /// <param name="u">基準頂點。</param>
  /// <param name="v">要影響的頂點。</param>
  private static void Relax(Node u, ref Node v) {
    // 從 u 到 w 的距離，也就是 v 的權重。
    double w = v.Score;
    // 這裡計算最大權重：
    // 如果 v 目前的距離值小於「u 的距離值＋w（w 是 u 到 w 的距離，也就是 v 的權重）」，
    // 我們就更新 v 的距離及其前述頂點。
    if (v.Distance >= u.Distance + w) return;
    v.Distance = u.Distance + w;
    v.Prev = u;
  }
}
}