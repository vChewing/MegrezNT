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
  /// <remarks>
  /// 其實就是個 [int: Node] 形態的辭典。然而，C# 沒有 Swift 那樣的 TypeAlias 機制，所以還是不要精簡了。
  /// </remarks>
  public struct SpanUnit {
    /// <summary>
    /// 節點陣列。每個位置上的節點可能是 null。
    /// </summary>
    public Dictionary<int, Node> Nodes = new();
    /// <summary>
    /// 該幅位單元內的所有節點當中持有最長幅位的節點長度。
    /// 該變數受該幅位的自身操作函式而被動更新。
    /// </summary>
    public int MaxLength => Nodes.Keys.Count > 0? Nodes.Keys.Max() : 0;
    /// <summary>
    /// 該幅位單元內的節點的幅位長度上限。
    /// </summary>
    private List<int> AllowedLengths => new BRange(1, MaxSpanLength + 1).ToList();

    /// <summary>
    /// 幅位乃指一組共享起點的節點。
    /// </summary>
    /// <remarks>
    /// 其實就是個 [int: Node] 形態的辭典。然而，C# 沒有 Swift 那樣的 TypeAlias 機制，所以還是不要精簡了。
    /// </remarks>
    public SpanUnit() {}

    /// <summary>
    /// 幅位乃指一組共享起點的節點。該建構子用來拿給定的既有幅位單元製作硬拷貝。
    /// </summary>
    /// <remarks>
    /// 其實就是個 [int: Node] 形態的辭典。然而，C# 沒有 Swift 那樣的 TypeAlias 機制，所以還是不要精簡了。
    /// </remarks>
    public SpanUnit(SpanUnit spanUnit) {
      foreach (int spanLength in spanUnit.Nodes.Keys) Nodes[spanLength] = spanUnit.Nodes[spanLength].Copy();
    }

    /// <summary>
    /// 生成自身的拷貝。
    /// </summary>
    /// <remarks>
    /// 因為 Node 不是 Struct，所以會在 Compositor 被拷貝的時候無法被真實複製。
    /// 這樣一來，Compositor 複製品當中的 Node 的變化會被反應到原先的 Compositor 身上。
    /// 這在某些情況下會造成意料之外的混亂情況，所以需要引入一個拷貝用的建構子。
    /// </remarks>
    /// <returns>拷貝。</returns>
    public SpanUnit HardCopy() => new(spanUnit: this);

    /// <summary>
    /// 清除該幅位單元內的全部的節點。
    /// </summary>
    public void Clear() => Nodes.Clear();

    /// <summary>
    /// 往該幅位塞入一個節點。
    /// </summary>
    /// <param name="node">要塞入的節點。</param>
    /// <returns>該操作是否成功執行。</returns>
    public bool Append(Node node) {
      if (!AllowedLengths.Contains(node.SpanLength)) return false;
      Nodes[node.SpanLength] = node;
      return true;
    }

    /// <summary>
    /// 丟掉任何與給定節點完全雷同的節點。
    /// </summary>
    /// <param name="givenNode">要參照的節點。</param>
    public void Nullify(Node givenNode) => Nodes.Remove(givenNode.SpanLength);

    /// <summary>
    /// 丟掉任何不小於給定幅位長度的節點。
    /// </summary>
    /// <param name="length">給定的幅位長度。</param>
    /// <returns>該操作是否成功執行。</returns>
    public bool DropNodesOfOrBeyond(int length) {
      if (!AllowedLengths.Contains(length)) return false;
      length = Math.Min(length, MaxSpanLength);
      foreach (int i in new BRange(length, MaxSpanLength + 1)) {
        Nodes.Remove(i);
      }
      return true;
    }

    /// <summary>
    /// 以給定的幅位長度，在當前幅位單元內找出對應的節點。
    /// </summary>
    /// <param name="length">給定的幅位長度。</param>
    /// <returns>查詢結果。</returns>
    public Node? NodeOf(int length) => Nodes.ContainsKey(length) ? Nodes[length] : null;
  }

  // MARK: - Internal Implementations.

  /// <summary>
  /// 找出所有與該位置重疊的節點。其返回值為一個節錨陣列（包含節點、以及其起始位置）。
  /// </summary>
  /// <param name="givenLocation">游標位置。</param>
  /// <param name="filter">指定內容保留類型（是在游標前方還是在後方、還是包含交叉節點在內的全部結果）。</param>
  /// <returns>一個包含所有與該位置重疊的節點的陣列。</returns>
  private List<NodeAnchor> FetchOverlappingNodesAt(int givenLocation,
                                                   CandidateFetchFilter filter = CandidateFetchFilter.All) {
    HashSet<NodeAnchor> resultsOfSingleAt = new();
    HashSet<NodeAnchor> resultsBeginAt = new();
    HashSet<NodeAnchor> resultsEndAt = new();
    HashSet<NodeAnchor> resultsCrossingAt = new();
    BRange rangeOfValidLocations = new(lowerbound: 0, upperbound: Spans.Count);
    if (Spans.IsEmpty() || !rangeOfValidLocations.Contains(givenLocation)) return new();
    foreach (int spanLength in new BRange(1, Spans[givenLocation].MaxLength + 1)) {
      if (Spans[givenLocation].NodeOf(spanLength) is not {} node) continue;
      if (node.KeyArray.IsEmpty() || string.IsNullOrEmpty(node.KeyArray.Joined())) continue;
      if (node.SpanLength == 1) {
        resultsOfSingleAt.Add(new(node: node, spanIndex: givenLocation));
      } else {
        resultsBeginAt.Add(new(node: node, spanIndex: givenLocation));
      }
    }
    int begin = givenLocation - Math.Min(givenLocation, MaxSpanLength - 1);
    foreach (int theLocation in new BRange(begin, givenLocation)) {
      (int alpha, int bravo) = (givenLocation - theLocation + 1, Spans[theLocation].MaxLength);
      if (alpha > bravo) continue;
      foreach (int theLength in new BRange(alpha, bravo + 1)) {
        bool isEndAt = theLength <= givenLocation - begin;
        if (Spans[theLocation].NodeOf(theLength) is not {} node) continue;
        if (node.KeyArray.IsEmpty() || string.IsNullOrEmpty(node.KeyArray.Joined())) continue;
        NodeAnchor theAnchor = new(node: node, spanIndex: theLocation + 1);
        if (resultsOfSingleAt.Contains(theAnchor) || resultsBeginAt.Contains(theAnchor)) continue;
        if (isEndAt) {
          resultsEndAt.Add(theAnchor);
        } else {
          resultsCrossingAt.Add(theAnchor);
        }
      }
    }
    return filter switch {
      CandidateFetchFilter.BeginAt => new HashSet<NodeAnchor>(resultsOfSingleAt.Union(resultsBeginAt)).ToList(),
      CandidateFetchFilter.EndAt => new HashSet<NodeAnchor>(resultsOfSingleAt.Union(resultsEndAt)).ToList(),
      _ => new HashSet<NodeAnchor>(resultsOfSingleAt.Union(resultsEndAt).Union(resultsBeginAt.Union(resultsCrossingAt)))
               .ToList()
    };
  }
}
}  // namespace Megrez