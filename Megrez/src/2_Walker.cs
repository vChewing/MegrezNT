// (c) 2022 and onwards The vChewing Project (LGPL v3.0 License or later).
// ====================
// This code is released under the SPDX-License-Identifier: `LGPL-3.0-or-later`.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Megrez {
  public partial class Compositor {
    /// <summary>
    /// 批次清理所有 SearchState 物件以防止記憶體洩漏
    /// </summary>
    /// <param name="visited">已訪問的狀態集合</param>
    /// <param name="openSet">優先序列中剩餘的狀態</param>
    /// <param name="leadingState">初始狀態</param>
    private static void BatchCleanAllSearchStates(
      HashSet<SearchState> visited,
      HybridPriorityQueue<PrioritizedState> openSet,
      SearchState leadingState
    ) {
      // 收集所有需要清理的 SearchState 物件
      HashSet<SearchState> allStates = new();

      // 1. 新增 visited set 中的所有狀態
      foreach (SearchState state in visited) {
        allStates.Add(state);
      }

      // 2. 新增 openSet 中剩餘的所有狀態
      while (!openSet.IsEmpty) {
        if (openSet.Dequeue() is { } prioritizedState) {
          allStates.Add(prioritizedState.State);
        }
      }

      // 3. 新增 leadingState（如果還沒被包含）
      allStates.Add(leadingState);

      // 4. 對所有狀態進行清理，使用任一狀態作為起點來清理整個網路
      // 由於所有狀態都可能互相參照，我們選擇任一狀態作為起點進行全域清理
      if (allStates.FirstOrDefault() is { } anyState) {
        anyState.BatchCleanSearchStateTree(allStates);
      }
    }

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
        if (openSet.Dequeue() is not { } currentPState) break;

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
      if (bestFinalState == null) {
        // 即使沒有找到最佳狀態，也需要清理所有建立的 SearchState 物件
        BatchCleanAllSearchStates(visited, openSet, start);
        return new();
      }

      List<Node> pathNodes = new();
      SearchState? currentState = bestFinalState;

      while (currentState != null) {
        // 排除起始和結束的虛擬節點。
        if (currentState.Node != null && !ReferenceEquals(currentState.Node, leadingNode)) {
          pathNodes.Insert(0, currentState.Node);
        }
        currentState = currentState.Prev;
        // 備註：此處不需要手動 ASAN，因為沒有參據循環（Retain Cycle）。
      }

      // 手動 ASAN：批次清理所有 SearchState 物件以防止記憶體洩漏
      // 包括 visited set 中的所有狀態、openSet 中剩餘的狀態，以及 leadingState
      BatchCleanAllSearchStates(visited, openSet, start);

      WalkedNodes = pathNodes.Select(n => n.Copy()).ToList();
      return WalkedNodes;
    }

    /// <summary>用於追蹤搜尋過程中的狀態。</summary>
    private class SearchState : IEquatable<SearchState> {
      public Node? Node { get; set; }
      public int Position { get; }
      public SearchState? Prev { get; set; }
      public double Distance { get; }

      // 用於穩定 hash 計算的不可變參據
      private readonly Node? originalNodeRef; // 原始節點參據（不可變）
      private readonly int stableHashCode; // 預計算的穩定 hash 值

      public SearchState(Node? node, int position, SearchState? prev, double distance) {
        Node = node;
        Position = position;
        Prev = prev;
        Distance = distance;
        // 保存原始參據用於穩定的 hash 計算
        originalNodeRef = node;
        stableHashCode = ComputeStableHashCode(node, position);
      }

      private static int ComputeStableHashCode(Node? node, int position) {
        unchecked {
          int hash = 17;
          hash = hash * 23 + (node?.GetHashCode() ?? 0);
          hash = hash * 23 + position.GetHashCode();
          return hash;
        }
      }

      /// <summary>
      /// 手動位址清理：對整個 SearchState 樹進行批次清理
      /// 使用頂點方法清理所有 Node 和 Prev 參據以防止記憶體洩漏
      /// </summary>
      /// <param name="allStates">所有需要清理的狀態集合，如果提供則直接清理這些狀態</param>
      public void BatchCleanSearchStateTree(HashSet<SearchState>? allStates = null) {
        if (allStates != null) {
          // 清理所有提供的狀態
          foreach (SearchState state in allStates) {
            state.Node = null;
            state.Prev = null;
          }
        } else {
          // 原有的樹狀清理邏輯（向下相容）
          HashSet<SearchState> visited = new();
          Stack<SearchState> stack = new();
          stack.Push(this);

          while (stack.Count > 0) {
            SearchState current = stack.Pop();

            // 避免重複造訪同一個節點
            if (!visited.Add(current)) continue;

            // 將前一個狀態加入堆疊以進行清理
            if (current.Prev != null) {
              stack.Push(current.Prev);
            }

            // 清理當前狀態的參據
            current.Node = null;
            current.Prev = null;
          }
        }
      }

      public bool Equals(SearchState? other) {
        return other != null && ReferenceEquals(originalNodeRef, other.originalNodeRef) && Position == other.Position;
      }

      public override bool Equals(object? obj) => Equals(obj as SearchState);

      public override int GetHashCode() {
        return stableHashCode;
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
