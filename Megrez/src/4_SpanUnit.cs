// (c) 2022 and onwards The vChewing Project (LGPL v3.0 License or later).
// ====================
// This code is released under the SPDX-License-Identifier: `LGPL-3.0-or-later`.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Megrez {
  public partial class Compositor {
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
      public int MaxLength => Nodes.Keys.Count > 0 ? Nodes.Keys.Max() : 0;

      /// <summary>
      /// 幅位乃指一組共享起點的節點。
      /// </summary>
      /// <remarks>
      /// 其實就是個 [int: Node] 形態的辭典。然而，C# 沒有 Swift 那樣的 TypeAlias 機制，所以還是不要精簡了。
      /// </remarks>
      public SpanUnit() { }

      /// <summary>
      /// 幅位乃指一組共享起點的節點。該建構子用來拿給定的既有幅位單元製作硬拷貝。
      /// </summary>
      /// <remarks>
      /// 其實就是個 [int: Node] 形態的辭典。然而，C# 沒有 Swift 那樣的 TypeAlias 機制，所以還是不要精簡了。
      /// </remarks>
      public SpanUnit(SpanUnit spanUnit) {
        foreach (int spanLength in spanUnit.Nodes.Keys)
          Nodes[spanLength] = spanUnit.Nodes[spanLength].Copy();
      }

      /// <summary>
      ///
      /// </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      public override bool Equals(object obj) {
        return obj is not SpanUnit spanUnit ? false : Nodes.SequenceEqual(spanUnit.Nodes);
      }

      /// <summary>
      ///
      /// </summary>
      /// <returns></returns>
      public override int GetHashCode() { return Nodes.GetHashCode(); }

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
      /// 以給定的幅位長度，在當前幅位單元內找出對應的節點。
      /// </summary>
      /// <param name="length">給定的幅位長度。</param>
      /// <returns>查詢結果。</returns>
      public Node? NodeOf(int length) => Nodes.ContainsKey(length) ? Nodes[length] : null;

      /// <summary>
      /// 
      /// </summary>
      /// <param name="left"></param>
      /// <param name="right"></param>
      /// <returns></returns>
      public static bool operator ==(SpanUnit left, SpanUnit right) {
        return left.Equals(right);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="left"></param>
      /// <param name="right"></param>
      /// <returns></returns>
      public static bool operator !=(SpanUnit left, SpanUnit right) {
        return !(left == right);
      }
    }

    // MARK: - Internal Implementations.

    /// <summary>
    /// (Result A, Result B) 辭典陣列。Result A 以索引查座標，Result B 以座標查索引。
    /// </summary>
    public struct NodeWithLocation {
      /// <summary>
      /// 節點在注拼槽當中的位置。
      /// </summary>
      public int Location { get; private set; }
      /// <summary>
      /// 節點。
      /// </summary>
      public Node Node { get; private set; }

      /// <summary>
      /// (Result A, Result B) 辭典陣列。Result A 以索引查座標，Result B 以座標查索引。
      /// </summary>
      /// <param name="location">節點在注拼槽當中的位置。</param>
      /// <param name="node">節點。</param>
      public NodeWithLocation(int location, Node node) {
        Location = location;
        Node = node;
      }
    }

    /// <summary>
    /// 找出所有與該位置重疊的節點。其返回值為一個節錨陣列（包含節點、以及其起始位置）。
    /// </summary>
    /// <param name="givenLocation">游標位置。</param>
    /// <returns>一個包含所有與該位置重疊的節點的陣列。</returns>
    public List<NodeWithLocation> FetchOverlappingNodesAt(int givenLocation) {
      List<NodeWithLocation> results = new();
      givenLocation = Math.Max(0, Math.Min(givenLocation, Keys.Count - 1));
      if (Spans.IsEmpty())
        return results;

      // 先獲取該位置的所有單字節點。
      foreach (int spanLength in new BRange(1, Spans[givenLocation].MaxLength + 1)) {
        if (Spans[givenLocation].NodeOf(spanLength) is not { } node)
          continue;
        InsertAnchor(spanIndex: givenLocation, node: node, targetContainer: ref results);
      }

      // 再獲取以當前位置結尾或開頭的節點。
      int begin = givenLocation - Math.Min(givenLocation, MaxSpanLength - 1);
      foreach (int theLocation in new BRange(begin, givenLocation)) {
        int alpha = givenLocation - theLocation + 1;
        int bravo = Spans[theLocation].MaxLength;
        if (alpha > bravo)
          continue;
        foreach (int theLength in new BRange(alpha, bravo + 1)) {
          if (Spans[theLocation].NodeOf(theLength) is not { } node)
            continue;
          InsertAnchor(spanIndex: theLocation, node: node, targetContainer: ref results);
        }
      }
      return results;
    }

    private static void InsertAnchor(int spanIndex, Node node, ref List<NodeWithLocation> targetContainer) {
      if (string.IsNullOrEmpty(node.KeyArray.Joined()))
        return;
      NodeWithLocation anchor = new(spanIndex, node);
      foreach (int i in new BRange(lowerbound: 0, upperbound: targetContainer.Count + 1)) {
        if (targetContainer.IsEmpty())
          break;
        if (targetContainer.First().Node.SpanLength > anchor.Node.SpanLength)
          continue;
        targetContainer.Insert(i, anchor);
        return;
      }
      if (!targetContainer.IsEmpty())
        return;
      targetContainer.Add(anchor);
    }
  }
}  // namespace Megrez
