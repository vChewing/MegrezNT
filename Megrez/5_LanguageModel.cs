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
/// 語言模型框架，回頭實際使用時需要派生一個型別、且重寫相關函式。
/// </summary>
public class LanguageModel {
  /// <summary>
  /// 語言模型框架，回頭實際使用時需要派生一個型別、且重寫相關函式。
  /// </summary>
  public LanguageModel() {}

  // This function works merely as a placeholder.
  // Please override this function to implement your own methods.
  /// <summary>
  /// 給定鍵，讓語言模型找給一組單元圖陣列。
  /// </summary>
  /// <param name="key">給定鍵。</param>
  /// <returns>一組單元圖陣列。</returns>
  public virtual List<Unigram> UnigramsFor(string key) {
    return key.Length == 0 ? new List<Unigram>().ToList() : new();
  }

  // This function works merely as a placeholder.
  // Please override this function to implement your own methods.
  /// <summary>
  /// 給定當前鍵與前述鍵，讓語言模型找給一組雙元圖陣列。
  /// </summary>
  /// <param name="precedingKey">前述鍵。</param>
  /// <param name="key">當前鍵。</param>
  /// <returns>一組雙元圖陣列。</returns>
  public virtual List<Bigram> BigramsForKeys(string precedingKey, string key) {
    return precedingKey == key ? new List<Bigram>().ToList() : new();
  }

  // This function works merely as a placeholder.
  // Please override this function to implement your own methods.
  /// <summary>
  /// 給定鍵，確認是否有單元圖記錄在庫。
  /// </summary>
  /// <param name="key">給定鍵。</param>
  /// <returns>True 則表示在庫，False 則表示不在庫。</returns>
  public virtual bool HasUnigramsFor(string key) { return key.Length != 0; }
}
}