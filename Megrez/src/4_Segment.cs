// (c) 2022 and onwards The vChewing Project (LGPL v3.0 License or later).
// ====================
// This code is released under the SPDX-License-Identifier: `LGPL-3.0-or-later`.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Megrez {
  public partial class Compositor {
    /// <summary>
    /// 組字引擎內的區段管理單元。
    /// </summary>
    /// <remarks>
    /// 區段單元管理一組具有共同起始位置的節點集合。實質上相當於一個以整數為索引、
    /// 以節點為數值的字典結構。由於 C# 環境缺乏類似 Swift 的 TypeAlias 功能，
    /// 因此保持完整的結構定義而不進行簡化。
    /// </remarks>
    public struct Segment {
      /// <summary>
      /// 節點資料的儲存字典，每個位置可能包含對應的節點物件或為空值。
      /// </summary>
      public Dictionary<int, Node> Nodes = new();
      /// <summary>
      /// 區段單元內所有節點中具有最大涵蓋範圍的節點長度數值。
      /// 此數值會隨著區段操作函數的執行而自動更新。
      /// </summary>
      public int MaxLength => Nodes.Keys.Count > 0 ? Nodes.Keys.Max() : 0;

      /// <summary>
      /// 區段單元的預設建構函數。
      /// </summary>
      /// <remarks>
      /// 區段單元管理一組具有共同起始位置的節點集合。實質上相當於一個以整數為索引、
      /// 以節點為數值的字典結構。由於 C# 環境缺乏類似 Swift 的 TypeAlias 功能，
      /// 因此保持完整的結構定義而不進行簡化。
      /// </remarks>
      public Segment() { }

      /// <summary>
      /// 區段單元的複製建構函數，用於基於現有區段單元建立深層複製。
      /// </summary>
      /// <remarks>
      /// 區段單元管理一組具有共同起始位置的節點集合。實質上相當於一個以整數為索引、
      /// 以節點為數值的字典結構。由於 C# 環境缺乏類似 Swift 的 TypeAlias 功能，
      /// 因此保持完整的結構定義而不進行簡化。
      /// </remarks>
      public Segment(Segment segment) {
        foreach (int segLength in segment.Nodes.Keys)
          Nodes[segLength] = segment.Nodes[segLength].Copy();
      }

      /// <summary>
      ///
      /// </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      public override bool Equals(object obj) {
        return obj is not Segment segment ? false : Nodes.SequenceEqual(segment.Nodes);
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
      public Segment HardCopy() => new(segment: this);

      /// <summary>
      /// 清除該幅節單元內的全部的節點。
      /// </summary>
      public void Clear() => Nodes.Clear();

      /// <summary>
      /// 以給定的幅節長度，在當前幅節單元內找出對應的節點。
      /// </summary>
      /// <param name="length">給定的幅節長度。</param>
      /// <returns>查詢結果。</returns>
      public Node? NodeOf(int length) => Nodes.ContainsKey(length) ? Nodes[length] : null;

      /// <summary>
      /// 
      /// </summary>
      /// <param name="left"></param>
      /// <param name="right"></param>
      /// <returns></returns>
      public static bool operator ==(Segment left, Segment right) {
        return left.Equals(right);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="left"></param>
      /// <param name="right"></param>
      /// <returns></returns>
      public static bool operator !=(Segment left, Segment right) {
        return !(left == right);
      }
    }

    // MARK: - Internal Implementations.

    /// <summary>
    /// (Result A, Result B) 字典陣列。Result A 以索引查座標，Result B 以座標查索引。
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
      /// (Result A, Result B) 字典陣列。Result A 以索引查座標，Result B 以座標查索引。
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
      if (Segments.IsEmpty())
        return results;

      // 先獲取該位置的所有單字節點。
      foreach (int segLength in new BRange(1, Segments[givenLocation].MaxLength + 1)) {
        if (Segments[givenLocation].NodeOf(segLength) is not { } node)
          continue;
        InsertAnchor(segmentIndex: givenLocation, node: node, targetContainer: ref results);
      }

      // 再獲取以當前位置結尾或開頭的節點。
      int begin = givenLocation - Math.Min(givenLocation, MaxSegLength - 1);
      foreach (int theLocation in new BRange(begin, givenLocation)) {
        int alpha = givenLocation - theLocation + 1;
        int bravo = Segments[theLocation].MaxLength;
        if (alpha > bravo)
          continue;
        foreach (int theLength in new BRange(alpha, bravo + 1)) {
          if (Segments[theLocation].NodeOf(theLength) is not { } node)
            continue;
          InsertAnchor(segmentIndex: theLocation, node: node, targetContainer: ref results);
        }
      }
      return results;
    }

    private static void InsertAnchor(int segmentIndex, Node node, ref List<NodeWithLocation> targetContainer) {
      if (string.IsNullOrEmpty(node.KeyArray.Joined()))
        return;
      NodeWithLocation anchor = new(segmentIndex, node);
      foreach (int i in new BRange(lowerbound: 0, upperbound: targetContainer.Count + 1)) {
        if (targetContainer.IsEmpty())
          break;
        if (targetContainer.First().Node.SegLength > anchor.Node.SegLength)
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
