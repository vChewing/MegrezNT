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

using System.Collections.Generic;
using System.Linq;

namespace Megrez {
/// <summary>
/// 節锚。
/// </summary>
public struct NodeAnchor {
  /// <summary>
  /// 節锚。
  /// </summary>
  public NodeAnchor(Node node, double? mass = null) {
    Node = node;
    Mass = mass ?? Node.Score;
  }
  /// <summary>
  /// 用來判斷該節錨是否為空。
  /// </summary>
  public bool IsEmpty => string.IsNullOrEmpty(Node.Key);
  /// <summary>
  /// 節點。一個節锚內不一定有節點，還可能會出 null。
  /// </summary>
  public Node Node = new();
  /// <summary>
  /// 獲取用來比較的權重。
  /// </summary>
  public double ScoreForSort => Node.Score;
  /// <summary>
  /// 幅位長度。
  /// </summary>
  public int SpanLength => Node.SpanLength;
  /// <summary>
  /// 累計權重。
  /// </summary>
  public double Mass = 0.0;
  /// <summary>
  /// 索引鍵。
  /// </summary>
  public string Key => Node.Key;
  /// <summary>
  /// 單元圖陣列。
  /// </summary>
  public List<Unigram> Unigrams => Node.Unigrams;
  /// <summary>
  /// 雙元圖陣列。
  /// </summary>
  public List<Bigram> Bigrams => Node.Bigrams;
  /// <summary>
  /// 將當前節錨的內容輸出為字串。
  /// </summary>
  /// <returns>當前節錨的內容輸出成的字串。</returns>
  public override string ToString() {
    string stream = "";
    stream += "{@(" + SpanLength + "),";
    stream += Node != null ? Node.ToString() : "null";
    stream += "}";
    return stream;
  }
}
/// <summary>
/// 用以在陣列內容為節錨的時候擴展陣列功能。
/// </summary>
public static class NodeAnchorExtensions {
  /// <summary>
  /// 獲取當前節錨陣列當中的索引鍵陣列。
  /// </summary>
  /// <param name="list">資料來源陣列。</param>
  /// <returns>得到的索引鍵陣列。</returns>
  public static string[] Keys(this IEnumerable<NodeAnchor> list) {
    return list.Select(x => x.Node.CurrentPair.Key).ToArray();
  }
  /// <summary>
  /// 獲取當前節錨陣列當中的資料值陣列。
  /// </summary>
  /// <param name="list">資料來源陣列。</param>
  /// <returns>得到的資料值陣列。</returns>
  public static string[] Values(this IEnumerable<NodeAnchor> list) {
    return list.Select(x => x.Node.CurrentPair.Value).ToArray();
  }
}

}
