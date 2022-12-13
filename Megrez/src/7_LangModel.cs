// CSharpened and further development by (c) 2022 and onwards The vChewing Project (MIT License).
// Was initially rebranded from (c) Lukhnos Liu's C++ library "Gramambular 2" (MIT License).
// ====================
// This code is released under the MIT license (SPDX-License-Identifier: MIT)

using System.Collections.Generic;

namespace Megrez {
/// <summary>
/// 語言模型協定。
/// </summary>
public interface LangModelProtocol {
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

public partial struct Compositor {
  /// <summary>
  /// 一個專門用來與其它語言模型對接的外皮模組層，將所有獲取到的資料自動做穩定排序處理。
  /// </summary>
  public class LangModelRanked : LangModelProtocol {
    /// <summary>
    /// 對接的語言模型副本。
    /// </summary>
    public LangModelProtocol TheLangModel;
    /// <summary>
    /// 一個專門用來與其它語言模型對接的外皮模組層，將所有獲取到的資料自動做固定排序處理。
    /// </summary>
    /// <param name="langModel">要對接的語言模型副本。</param>
    public LangModelRanked(LangModelProtocol langModel) { TheLangModel = langModel; }
    /// <summary>
    /// 給定索引鍵陣列，讓語言模型找給一組經過穩定排序的單元圖陣列。
    /// </summary>
    /// <param name="keyArray">給定索引鍵陣列。</param>
    /// <returns>找出來的對應的在庫的單元圖。</returns>
    public List<Unigram> UnigramsFor(List<string> keyArray) =>
        TheLangModel.UnigramsFor(keyArray).StableSorted((x, y) => y.Score.CompareTo(x.Score));
    /// <inheritdoc />
    public bool HasUnigramsFor(List<string> keyArray) => TheLangModel.HasUnigramsFor(keyArray);
  }
}
}  // namespace Megrez
