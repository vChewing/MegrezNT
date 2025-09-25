// (c) 2022 and onwards The vChewing Project (LGPL v3.0 License or later).
// ====================
// This code is released under the SPDX-License-Identifier: `LGPL-3.0-or-later`.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Megrez {
  public partial class Compositor {
    /// <summary>
    /// 文字組句處理函式，採用 Dijkstra 路徑搜尋演算法更新當前組字器的 assembledNodes 結果。<para/>
    /// 此演算法在有向圖結構中搜尋具有最優評分的路徑，從而確定最合適的詞彙組合。<para/>
    /// 演算法所依賴的 HybridPriorityQueue 資料結構經過針對 Sandy Bridge 架構的特殊最佳化處理，
    /// 使得該算法在 Sandy Bridge CPU 的電腦上比 DAG 算法擁有更優的效能。<para/>
    /// </summary>
    /// <returns>組句結果（已選字詞陣列）。</returns>
    public List<Node> Assemble() {
      var assembledNodes = AssembledNodes;
      new PathFinder(Config, ref assembledNodes);
      AssembledNodes = assembledNodes;
      return AssembledNodes;
    }
  }
}

namespace Megrez {
  public partial class Compositor {
    /// <summary>
    /// 組句工具，會以 Dijkstra 演算法更新當前組字器的 assembledNodes。
    /// 該演算法會在圖中尋找具有最高分數的路徑，即最可能的字詞組合。
    /// 演算法所依賴的 HybridPriorityQueue 針對 Sandy Bridge 經過最佳化處理，
    /// 使得該演算法在 Sandy Bridge CPU 的電腦上比 DAG 演算法擁有更優的效能。
    /// </summary>
    public class PathFinder {
      // SearchState 記憶體追蹤
      private int searchStateCreatedCount = 0;
      private int searchStateDestroyedCount = 0;
      /// <summary>
      /// 建立 PathFinder 執行個體並執行路徑尋找演算法。
      /// </summary>
      /// <param name="config">組字器組態設定。</param>
      /// <param name="assembledNodes">要更新的組合節點清單。</param>
      public PathFinder(CompositorConfig config, ref List<Node> assembledNodes) {
        List<Node> newAssembledNodes = new();
        try {
          assembledNodes = newAssembledNodes;
          if (!config.Segments.Any()) return;

          // 初期化資料結構。
          HybridPriorityQueue<PrioritizedState> openSet = new(reversed: true);
          HashSet<SearchState> visited = new();
          // 使用陣列提升快取效能並預配置合理容量
          double[] bestScore = new double[config.Keys.Count + 1];
          for (int i = 0; i < bestScore.Length; i++) {
            bestScore[i] = double.MinValue;
          }

          List<Action> stateCleaningTasks = new();
          try {
            // 初期化起始狀態。
            Node leadingNode = new(new() { "$LEADING" }, segLength: 0, unigrams: new());
            SearchState start = new(node: leadingNode, position: 0, prev: null, distance: 0, cleaningTaskRegister: stateCleaningTasks, pathFinder: this);
            openSet.Enqueue(new(state: start));
            if (bestScore.Length > 0) {
              bestScore[0] = 0;
            }

            // 追蹤最佳結果。
            SearchState? bestFinalState = null;
            double bestFinalScore = double.MinValue;

            // 主要 Dijkstra 迴圈。
            while (!openSet.IsEmpty) {
              if (openSet.Dequeue() is not { } currentPState) break;
              stateCleaningTasks.Add(() => currentPState.State.CleanChainRecursively());

              // 如果已經造訪過具有更好分數的狀態，則跳過。
              if (!visited.Add(currentPState.State)) continue;

              // 檢查是否已到達終點。
              if (currentPState.State.Position >= config.Keys.Count) {
                if (currentPState.State.Distance > bestFinalScore) {
                  bestFinalScore = currentPState.State.Distance;
                  bestFinalState = currentPState.State;
                }
                continue;
              }

              // 處理下一個可能的節點。
              Segment currentSegment = config.Segments[currentPState.State.Position];
              foreach (KeyValuePair<int, Node> segmentNeta in currentSegment.Nodes) {
                int length = segmentNeta.Key;
                Node nextNode = segmentNeta.Value;

                // 早期無效性檢查：確保節點有有效的單元圖
                if (nextNode.Unigrams.Count == 0) continue;

                int nextPos = currentPState.State.Position + length;

                // 計算新的權重分數。
                double newScore = currentPState.State.Distance + nextNode.Score;

                // 如果該位置已有更優的權重分數，則跳過。
                if (nextPos < bestScore.Length && bestScore[nextPos] >= newScore) continue;

                SearchState nextState = new(node: nextNode, position: nextPos, prev: currentPState.State, distance: newScore, cleaningTaskRegister: stateCleaningTasks, pathFinder: this);

                if (nextPos < bestScore.Length) {
                  bestScore[nextPos] = newScore;
                }
                openSet.Enqueue(new(state: nextState));
              }
            }

            // 從最佳終止狀態重建路徑。
            if (bestFinalState == null) {
              return;
            }

            List<Node> pathNodes = new(config.Keys.Count); // 預配置合理容量
            SearchState? currentState = bestFinalState;

            while (currentState != null) {
              var nextState = currentState.Prev;
              try {
                // 排除起始和結束的虛擬節點。
                if (currentState.Node != null && !ReferenceEquals(currentState.Node, leadingNode)) {
                  pathNodes.Insert(0, currentState.Node);
                }
              } finally {
                // 清理當前狀態以避免記憶體洩漏
                currentState.Prev = null;
                currentState.Node = null;
              }
              currentState = nextState;
              // 備註：此處不需要手動 ASAN，因為沒有參據循環（Reference Cycle）。
            }

            // 先清理舊的 newAssembledNodes（如果有的話）再賦值新內容
            newAssembledNodes.Clear();
            newAssembledNodes.AddRange(pathNodes.Select(n => n.Copy()));

            // 清理路徑重建過程中的臨時陣列
            pathNodes.Clear();
          } finally {
            // 執行所有清理任務
            foreach (Action cleaningTask in stateCleaningTasks) {
              cleaningTask();
            }
            stateCleaningTasks.Clear();
            // 確保所有資料結構都被清理
            visited.Clear();
            Array.Clear(bestScore, 0, bestScore.Length);
          }
        } finally {
          assembledNodes = newAssembledNodes;
        }
      }

      /// <summary>
      /// PathFinder 解構函式，用於記憶體洩漏檢測。
      /// </summary>
      ~PathFinder() {
#if DEBUG
        if (searchStateCreatedCount != searchStateDestroyedCount) {
          System.Console.WriteLine($"PathFinder 記憶體洩漏檢測: 建立了 {searchStateCreatedCount} 個 SearchState，但只析構了 {searchStateDestroyedCount} 個");
        }
#endif
      }

      /// <summary>
      /// 用於追蹤搜尋過程中的狀態。
      /// 採用弱引用設計以最佳化記憶體使用。
      /// </summary>
      private class SearchState : IEquatable<SearchState> {
        private WeakReference? _nodeRef;
        private WeakReference? _prevRef;

        public Node? Node {
          get => _nodeRef?.Target as Node;
          set => _nodeRef = value != null ? new WeakReference(value) : null;
        }

        public int Position { get; }

        public SearchState? Prev {
          get => _prevRef?.Target as SearchState;
          set => _prevRef = value != null ? new WeakReference(value) : null;
        }

        public double Distance { get; }

        // PathFinder 弱引用
        private WeakReference? _pathFinderRef;
        public PathFinder? PathFinder {
          get => _pathFinderRef?.Target as PathFinder;
          set => _pathFinderRef = value != null ? new WeakReference(value) : null;
        }

        // 用於穩定 hash 計算的不可變參據
        private readonly WeakReference? originalNodeRef; // 原始節點參據（不可變，弱引用）
        private readonly int stableHashCode; // 預計算的穩定 hash 值

        public SearchState(Node? node, int position, SearchState? prev, double distance, List<Action> cleaningTaskRegister, PathFinder pathFinder) {
          Node = node;
          Position = position;
          Prev = prev;
          Distance = distance;
          PathFinder = pathFinder;
          // 保存原始參據用於穩定的 hash 計算（使用弱引用）
          originalNodeRef = node != null ? new WeakReference(node) : null;
          stableHashCode = ComputeStableHashCode(node, position);
          // 註冊清理任務
          cleaningTaskRegister.Add(CleanChainRecursively);
          // 更新建立計數器
          pathFinder.searchStateCreatedCount++;
#if DEBUG
          // 移除個別的建立訊息
#endif
        }

        ~SearchState() {
          // 更新析構計數器
          if (PathFinder != null) {
            PathFinder.searchStateDestroyedCount++;
          }
          Node = null;
          Prev = null;
          PathFinder = null;
#if DEBUG
          // 移除個別的析構訊息
#endif
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
        /// 清理整個 SearchState 鏈條，從當前節點開始向後遞歸清理
        /// 採用深度優先遍歷策略，確保每個節點都被清理
        /// </summary>
        public void CleanChainRecursively() {
          HashSet<int> visited = new();
          Stack<SearchState> stack = new();
          stack.Push(this);

          while (stack.Count > 0) {
            SearchState current = stack.Pop();
            int currentId = current.GetHashCode();

            // 避免重複清理和無限循環
            if (visited.Contains(currentId)) continue;
            visited.Add(currentId);

            // 在清理前，將 prev 加入堆疊（如果存在）
            if (current.Prev != null) {
              stack.Push(current.Prev);
            }

            // 清理當前節點
            current.Node = null;
            current.Prev?.CleanChainRecursively();
            current.Prev = null;
          }
        }

        public bool Equals(SearchState? other) {
          if (other == null) return false;
          if (Position != other.Position) return false;

          // 比較弱引用指向的對象
          var thisNode = originalNodeRef?.Target as Node;
          var otherNode = other.originalNodeRef?.Target as Node;
          return ReferenceEquals(thisNode, otherNode);
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
}
