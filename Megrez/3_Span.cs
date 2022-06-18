// CSharpened by (c) 2022 and onwards The vChewing Project (MIT-NTL License).
// Rebranded from (c) Lukhnos Liu's C++ library "Gramambular" (MIT License).
/*
Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

1. The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

2. No trademark license is granted to use the trade names, trademarks, service
marks, or product names of Contributor, except as required to fulfill notice
requirements above.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;

namespace Megrez {
/// <summary>
/// 幅位。
/// </summary>
public struct Span {
  /// <summary>
  /// 幅位。
  /// </summary>
  public Span() {}
  /// <summary>
  /// 辭典：以節點長度為索引，以節點為資料值。
  /// </summary>
  private Dictionary<int, Node> MutLengthNodeMap = new();
  /// <summary>
  /// 公開：最長幅距（唯讀）。
  /// </summary>
  private int MutMaximumLength = 0;
  public int MaximumLength => MutMaximumLength;
  /// <summary>
  /// 自我清空，各項參數歸零。
  /// </summary>
  public void Clear() {
    MutLengthNodeMap.Clear();
    MutMaximumLength = 0;
  }
  /// <summary>
  /// 往自身插入一個節點、及給定的節點長度。
  /// </summary>
  /// <param name="Node">節點。</param>
  /// <param name="Length">給定的節點長度。</param>
  public void Insert(Node Node, int Length) {
    Length = Math.Abs(Length);
    MutLengthNodeMap[Length] = Node;
    MutMaximumLength = Math.Max(MutMaximumLength, Length);
  }
  /// <summary>
  /// 移除任何比給定的長度更長的節點。
  /// </summary>
  /// <param name="Length">給定的節點長度。</param>
  public void RemoveNodeOfLengthGreaterThan(int Length) {
    Length = Math.Abs(Length);
    if (Length > MutMaximumLength) return;
    int LenMax = 0;
    Dictionary<int, Node> RemovalList = new();
    foreach (int Key in MutLengthNodeMap.Keys) {
      if (Key > Length)
        RemovalList.Add(Key, MutLengthNodeMap[Key]);
      else
        LenMax = Math.Max(Key, LenMax);
    }
    foreach (int Key in RemovalList.Keys) {
      MutLengthNodeMap.Remove(Key);
    }
    MutMaximumLength = LenMax;
  }
  /// <summary>
  /// 給定節點長度，獲取節點。
  /// </summary>
  /// <param name="Length">給定的節點長度。</param>
  /// <returns>節點。如果沒有節點則傳回 null。</returns>
  public Node? Node(int Length) {
    if (MutLengthNodeMap.ContainsKey(Math.Abs(Length))) {
      return MutLengthNodeMap[Math.Abs(Length)];
    } else {
      return null;
    }
  }
}
}