// (c) 2022 and onwards The vChewing Project (LGPL v3.0 License or later).
// ====================
// This code is released under the SPDX-License-Identifier: `LGPL-3.0-or-later`.

using System.Collections.Generic;
using System.Linq;

namespace Megrez {
  public partial class Compositor {
    /// <summary>
    /// 文字組句處理函式，採用 DAG (Directed Acyclic Graph) 動態規劃演算法更新當前組字器的 assembledSentence 結果。<para/>
    /// 此演算法使用動態規劃在有向無環圖中尋找具有最優評分的路徑，從而確定最合適的詞彙組合。<para/>
    /// DAG 演算法相對於 Dijkstra 演算法更簡潔，記憶體使用量更少。<para/>
    /// </summary>
    /// <returns>組句結果（已選字詞陣列）。</returns>
    public List<GramInPath> Assemble() {
      AssembledSentence = PathFinder.Assemble(Config);
      return AssembledSentence;
    }
  }
}

namespace Megrez {
  public partial class Compositor {
    /// <summary>
    /// 組句工具，會以 DAG 動態規劃演算法更新當前組字器的 assembledSentence。
    /// 該演算法使用動態規劃在有向無環圖中尋找具有最高分數的路徑，即最可能的字詞組合。
    /// DAG 演算法相對簡潔，記憶體使用量較少。
    /// </summary>
    public static class PathFinder {
      /// <summary>
      /// 執行 DAG 動態規劃演算法以更新組合語句清單。
      /// </summary>
      /// <param name="config">組字器組態設定。</param>
      /// <returns>組句結果。</returns>
      public static List<GramInPath> Assemble(CompositorConfig config) {
        List<GramInPath> newAssembledSentence = new();
        if (!config.Segments.Any()) return newAssembledSentence;

        int keyCount = config.Keys.Count;

        // 動態規劃陣列：dp[i] 表示到位置 i 的最佳分數
        double[] dp = new double[keyCount + 1];
        // 回溯陣列：parent[i] 記錄到達位置 i 的最佳前驅節點
        Node?[] parent = new Node?[keyCount + 1];

        // 初期化
        for (int i = 0; i < dp.Length; i++) {
          dp[i] = double.MinValue;
        }

        // 起始狀態
        dp[0] = 0;

        // DAG 動態規劃主循環
        for (int i = 0; i < keyCount; i++) {
          if (dp[i] <= double.MinValue) continue; // 只處理可達的位置

          // 遍歷從位置 i 開始的所有可能節點
          Segment currentSegment = config.Segments[i];
          foreach (KeyValuePair<int, Node> pair in currentSegment.Nodes) {
            int length = pair.Key;
            Node node = pair.Value;
            if (node.Unigrams.Count == 0) continue;

            int nextPos = i + length;
            if (nextPos > keyCount) continue;

            double newScore = dp[i] + node.Score;

            // 如果找到更好的路徑，更新 dp 和 parent
            if (newScore > dp[nextPos]) {
              dp[nextPos] = newScore;
              parent[nextPos] = node;
            }
          }
        }

        // 回溯構建最佳路徑
        int currentPos = keyCount;
        List<GramInPath> reversedSentence = new();

        // 從終點開始回溯
        while (currentPos > 0) {
          Node? node = parent[currentPos];
          if (node == null) break;

          GramInPath insertable = new(node.CurrentUnigram, node.IsOverridden);
          reversedSentence.Add(insertable);
          currentPos -= node.KeyArray.Count;
        }

        if (reversedSentence.Count > 0) {
          reversedSentence.Reverse();
          newAssembledSentence = reversedSentence;
        }

        return newAssembledSentence;
      }
    }
  }
}
