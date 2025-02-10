// (c) 2022 and onwards The vChewing Project (LGPL v3.0 License or later).
// ====================
// This code is released under the SPDX-License-Identifier: `LGPL-3.0-or-later`.

using System.Collections.Generic;

namespace Megrez
{
  /// <summary>
  /// 語言模型協定。
  /// </summary>
  public interface LangModelProtocol
  {
    /// <summary>
    /// 給定索引鍵陣列，讓語言模型找給一組單元圖陣列。
    /// </summary>
    /// <param name="keyArray">給定索引鍵陣列。</param>
    /// <returns>找出來的對應的在庫的單元圖。</returns>
    public List<Unigram> UnigramsFor(List<string> keyArray);
    /// <summary>
    /// 根據給定的索引鍵來確認各個資料庫陣列內是否存在對應的資料。
    /// </summary>
    /// <param name="keyArray"></param>
    /// <returns></returns>
    public bool HasUnigramsFor(List<string> keyArray);
  }
}  // namespace Megrez
