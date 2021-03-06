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
public struct SpanUnit {
  /// <summary>
  /// 幅位。
  /// </summary>
  public SpanUnit() {}
  /// <summary>
  /// 辭典：以節點長度為索引，以節點為資料值。
  /// </summary>
  private Dictionary<int, Node> _lengthNodeMap = new();
  /// <summary>
  /// 公開：最長幅距（唯讀）。
  /// </summary>
  public int MaxLength { get; private set; } = 0;
  /// <summary>
  /// 自我清空，各項參數歸零。
  /// </summary>
  public void Clear() {
    _lengthNodeMap.Clear();
    MaxLength = 0;
  }
  /// <summary>
  /// 往自身插入一個節點、及給定的節點長度。
  /// </summary>
  /// <param name="node">節點。</param>
  /// <param name="length">給定的節點長度。</param>
  public void Insert(Node node, int length) {
    length = Math.Abs(length);
    _lengthNodeMap[length] = node;
    MaxLength = Math.Max(MaxLength, length);
  }
  /// <summary>
  /// 移除任何比給定的長度更長的節點。
  /// </summary>
  /// <param name="length">給定的節點長度。</param>
  public void DropNodesBeyond(int length) {
    length = Math.Abs(length);
    if (length > MaxLength) return;
    int lenMax = 0;
    Dictionary<int, Node> removalList = new();
    foreach (int key in _lengthNodeMap.Keys) {
      if (key > length)
        removalList.Add(key, _lengthNodeMap[key]);
      else
        lenMax = Math.Max(key, lenMax);
    }
    foreach (int key in removalList.Keys) {
      _lengthNodeMap.Remove(key);
    }
    MaxLength = lenMax;
  }
  /// <summary>
  /// 給定節點長度，獲取節點。
  /// </summary>
  /// <param name="length">給定的節點長度。</param>
  /// <returns>節點。如果沒有節點則傳回 null。</returns>
  public Node? NodeOf(int length) {
    return _lengthNodeMap.ContainsKey(Math.Abs(length)) ? _lengthNodeMap[Math.Abs(length)] : null;
  }
}
}