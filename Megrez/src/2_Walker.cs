// CSharpened and further development by (c) 2022 and onwards The vChewing Project (MIT License).
// Was initially rebranded from (c) Lukhnos Liu's C++ library "Gramambular 2" (MIT License).
// Walking algorithm (Dijkstra) implemented by (c) 2025 and onwards The vChewing Project (MIT License).
// ====================
// This code is released under the MIT license (SPDX-License-Identifier: MIT)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Megrez {
public partial struct Compositor {
  /// <summary>
  /// 爬軌函式，會以 Dijkstra 算法更新當前組字器的 walkedNodes。<para/>
  /// 該算法會在圖中尋找具有最高分數的路徑，即最可能的字詞組合。<para/>
  /// 該算法所依賴的 HybridPriorityQueue 針對 Sandy Bridge 經過最佳化處理，
  /// 使得該算法在 Sandy Bridge CPU 的電腦上比 DAG 算法擁有更優的效能。<para/>
  /// </summary>
  /// <returns>爬軌結果（已選字詞陣列）。</returns>
  public List<Node> Walk() {
    WalkedNodes.Clear();
    if (!Spans.Any()) return new();

    // 初期化資料結構。
    HybridPriorityQueue<PrioritizedState> openSet = new(reversed: true);
    HashSet<SearchState> visited = new();
    Dictionary<int, double> bestScore = new();

    // 初期化起始狀態。
    Node leadingNode = new(new() { "$LEADING" }, spanLength: 0, unigrams: new());
    SearchState start = new(node: leadingNode, position: 0, prev: null, distance: 0);
    openSet.Enqueue(new(state: start));
    bestScore[0] = 0;

    // 追蹤最佳結果。
    SearchState? bestFinalState = null;
    double bestFinalScore = double.MinValue;

    // 主要 Dijkstra 迴圈。
    while (!openSet.IsEmpty) {
      if (openSet.Dequeue() is not {} currentPState) break;

      // 如果已經造訪過具有更好分數的狀態，則跳過。
      if (!visited.Add(currentPState.State)) continue;

      // 檢查是否已到達終點。
      if (currentPState.State.Position >= Keys.Count) {
        if (currentPState.State.Distance > bestFinalScore) {
          bestFinalScore = currentPState.State.Distance;
          bestFinalState = currentPState.State;
        }
        continue;
      }

      // 處理下一個可能的節點。
      SpanUnit currentSpan = Spans[currentPState.State.Position];
      foreach (KeyValuePair<int, Node> spanNeta in currentSpan.Nodes) {
        int length = spanNeta.Key;
        Node nextNode = spanNeta.Value;
        int nextPos = currentPState.State.Position + length;

        // 計算新的權重分數。
        double newScore = currentPState.State.Distance + nextNode.Score;

        // 如果該位置已有更優的權重分數，則跳過。
        if (bestScore.TryGetValue(nextPos, out double existingScore) && existingScore >= newScore) continue;

        SearchState nextState = new(node: nextNode, position: nextPos, prev: currentPState.State, distance: newScore);

        bestScore[nextPos] = newScore;
        openSet.Enqueue(new(state: nextState));
      }
    }

    // 從最佳終止狀態重建路徑。
    if (bestFinalState == null) return new();

    List<Node> pathNodes = new();
    SearchState? currentState = bestFinalState;

    while (currentState != null) {
      // 排除起始和結束的虛擬節點。
      if (!ReferenceEquals(currentState.Node, leadingNode)) {
        pathNodes.Insert(0, currentState.Node);
      }
      currentState = currentState.Prev;
      // 備註：此處不需要手動 ASAN，因為沒有參據循環（Retain Cycle）。
    }

    WalkedNodes = pathNodes.Select(n => n.Copy()).ToList();
    return WalkedNodes;
  }

  /// <summary>用於追蹤搜尋過程中的狀態。</summary>
  private class SearchState : IEquatable<SearchState> {
    public Node Node { get; }
    public int Position { get; }
    public SearchState? Prev { get; }
    public double Distance { get; }

    public SearchState(Node node, int position, SearchState? prev, double distance) {
      Node = node;
      Position = position;
      Prev = prev;
      Distance = distance;
    }

    public bool Equals(SearchState? other) {
      return other != null && ReferenceEquals(Node, other.Node) && Position == other.Position;
    }

    public override bool Equals(object? obj) => Equals(obj as SearchState);

    public override int GetHashCode() {
      int[] x = { Node.GetHashCode(), Position.GetHashCode() };
      return x.GetHashCode();
    }
  }

  private record PrioritizedState : IComparable<PrioritizedState> {
    public SearchState State { get; }

    public PrioritizedState(SearchState state) => State = state;

    public int CompareTo(PrioritizedState? other) {
      return other == null ? 1 : State.Distance.CompareTo(other.State.Distance);
    }
  }
}
}
