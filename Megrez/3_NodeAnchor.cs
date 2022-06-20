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

namespace Megrez {
/// <summary>
/// 節锚。
/// </summary>
public struct NodeAnchor {
  /// <summary>
  /// 節锚。
  /// </summary>
  public NodeAnchor(Node node, int location, int spanningLength) {
    Node = node;
    Location = location;
    SpanningLength = spanningLength;
  }
  /// <summary>
  /// 節點。一個節锚內不一定有節點，還可能會出 null。
  /// </summary>
  public Node? Node = null;
  /// <summary>
  /// 節锚所在的位置。
  /// </summary>
  public int Location = 0;
  /// <summary>
  /// 幅位長度。
  /// </summary>
  public int SpanningLength = 0;
  /// <summary>
  /// 累計權重。
  /// </summary>
  public double AccumulatedScore = 0.0;
  /// <summary>
  /// 索引鍵的長度。
  /// </summary>
  public int KeyLength => Node?.Key.Length ?? 0;
  /// <summary>
  /// 獲取用來比較的權重。
  /// </summary>
  public double ScoreForSort => Node?.Score ?? 0.0;
  public override string ToString() {
    string stream = "";
    stream += "{@(" + Location + "," + SpanningLength + "),";
    stream += Node != null ? Node.ToString() : "null";
    stream += "}";
    return stream;
  }
}
}