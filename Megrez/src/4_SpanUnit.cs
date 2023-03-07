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
  /// 幅位單元乃指一組共享起點的節點。
  /// </summary>
  public class SpanUnit {
    /// <summary>
    /// 節點陣列。每個位置上的節點可能是 null。
    /// </summary>
    public List<Node?> Nodes = new();
    /// <summary>
    /// 該幅位單元內的所有節點當中持有最長幅位的節點長度。
    /// 該變數受該幅位的自身操作函式而被動更新。
    /// </summary>
    public int MaxLength { get; private set; }
    /// <summary>
    /// 該幅位單元內的節點的幅位長度上限。
    /// </summary>
    private List<int> AllowedLengths => new BRange(1, MaxSpanLength + 1).ToList();

    /// <summary>
    /// 幅位乃指一組共享起點的節點。
    /// </summary>
    public SpanUnit() {
      Clear();  // 該函式會自動給 MaxLength 賦值。
    }

    /// <summary>
    /// 清除該幅位單元內的全部的節點，且重設最長節點長度為 0，然後再在節點陣列內預留空位。
    /// </summary>
    public void Clear() {
      Nodes.Clear();
      foreach (int _ in new BRange(0, MaxSpanLength)) Nodes.Add(null);
      MaxLength = 0;
    }

    /// <summary>
    /// 丟掉任何與給定節點完全雷同的節點。
    /// </summary>
    /// <remarks>
    /// Swift 不像 C# 那樣有容量鎖定型陣列，
    /// 對某個位置的內容的刪除行為都可能會導致其它內容錯位、繼發其它不可知故障。
    /// 於是就提供了這個專門的工具函式。
    /// </remarks>
    /// <param name="givenNode">要參照的節點。</param>
    public void Nullify(Node givenNode) {
      BRange theRange = new(lowerbound: 0, upperbound: Nodes.Count);
      foreach (int theIndex in theRange) {
        Node? currentNode = Nodes[theIndex];
        if (!Equals(currentNode, givenNode)) continue;
        Nodes[theIndex] = null;
      }
    }

    /// <summary>
    /// 往該幅位塞入一個節點。
    /// </summary>
    /// <param name="node">要塞入的節點。</param>
    /// <returns>該操作是否成功執行。</returns>
    public bool Add(Node node) {
      if (!AllowedLengths.Contains(node.SpanLength)) return false;
      Nodes[node.SpanLength - 1] = node;
      MaxLength = Math.Max(MaxLength, node.SpanLength);
      return true;
    }

    /// <summary>
    /// 丟掉任何不小於給定幅位長度的節點。
    /// </summary>
    /// <param name="length">給定的幅位長度。</param>
    /// <returns>該操作是否成功執行。</returns>
    public bool DropNodesOfOrBeyond(int length) {
      if (!AllowedLengths.Contains(length)) return false;
      foreach (int i in new BRange(length, MaxSpanLength + 1)) {
        if (i > Nodes.Count) continue;  // 防呆
        Nodes[i - 1] = null;
      }
      MaxLength = 0;
      if (length <= 1) return false;
      int maxR = length - 2;
      foreach (int i in new BRange(0, maxR + 1)) {
        BRange countRange = new(0, maxR - i + 1);
        if (!countRange.Contains(maxR - i)) continue;  // 防呆
        MaxLength = maxR - i + 1;
        break;
      }
      return true;
    }

    /// <summary>
    /// 以給定的幅位長度，在當前幅位單元內找出對應的節點。
    /// </summary>
    /// <param name="length">給定的幅位長度。</param>
    /// <returns>查詢結果。</returns>
    public Node? NodeOf(int length) => AllowedLengths.Contains(length) ? Nodes[length - 1] : null;
  }

  // MARK: - Internal Implementations.

  /// <summary>
  /// 找出所有與該位置重疊的節點。其返回值為一個節錨陣列（包含節點、以及其起始位置）。
  /// </summary>
  /// <param name="location">游標位置。</param>
  /// <returns>一個包含所有與該位置重疊的節點的陣列。</returns>
  internal List<NodeAnchor> FetchOverlappingNodesAt(int location) {
    List<NodeAnchor> results = new();
    if (Spans.IsEmpty() || location >= Spans.Count) return results;

    // 先獲取該位置的所有單字節點。
    foreach (int theLocation in new BRange(1, Spans[location].MaxLength + 1)) {
      if (Spans[location].NodeOf(theLocation) is not {} node) continue;
      results.Add(new(node, location));
    }

    // 再獲取以當前位置結尾或開頭的節點。
    int begin = location - Math.Min(location, MaxSpanLength - 1);
    foreach (int theLocation in new BRange(begin, location)) {
      (int alpha, int bravo) = (location - theLocation + 1, Spans[theLocation].MaxLength);
      if (alpha > bravo) continue;
      foreach (int theLength in new BRange(alpha, bravo + 1)) {
        if (Spans[theLocation].NodeOf(theLength) is not {} node) continue;
        results.Add(new(node, theLocation));
      }
    }
    return results;
  }
}
}  // namespace Megrez