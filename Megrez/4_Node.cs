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
using System.Linq;
using System.Collections.Generic;

namespace Megrez {
/// <summary>
/// 節點。
/// </summary>
public class Node {
  /// <summary>
  /// 單元圖陣列。
  /// </summary>
  public List<Unigram> Unigrams { get; private set; }
  /// <summary>
  /// 雙元圖陣列。
  /// </summary>
  public List<Bigram> Bigrams { get; private set; }
  /// <summary>
  /// 專門「用單元圖資料值來調查索引值」的辭典。
  /// </summary>
  private Dictionary<string, int> _valueUnigramIndexMap;
  /// <summary>
  /// 專門「用給定鍵值來取對應的雙元圖陣列」的辭典。
  /// </summary>
  private Dictionary<KeyValuePaired, List<Bigram>> _precedingBigramMap = new();
  /// <summary>
  /// 用來登記「當前選中的單元圖」的索引值的變數。
  /// </summary>
  private int _selectedUnigramIndex;
  /// <summary>
  /// 指定的幅位長度。
  /// </summary>
  public int SpanLength { get; private set; }

  /// <summary>
  /// 用來登記要施加給「『被標記為選中狀態』的候選字詞」的複寫權重的數值。
  /// </summary>
  public const double ConSelectedCandidateScore = 99.0;
  /// <summary>
  /// 將當前節點的內容輸出為字串。
  /// </summary>
  /// <returns>當前節點的內容輸出成的字串。</returns>
  public override string ToString() =>
      $"(node,key:{Key},fixed:{(IsCandidateFixed ? "true" : "false")},selected:{_selectedUnigramIndex},{Unigrams})";
  /// <summary>
  /// 公開：候選字詞陣列（唯讀），以鍵值陣列的形式存在。
  /// </summary>
  public List<KeyValuePaired> Candidates { get; } = new();
  /// <summary>
  /// 公開：用來登記「當前選中的單元圖」的索引值的變數（唯讀）。
  /// </summary>
  public bool IsCandidateFixed { get; private set; }
  /// <summary>
  /// 公開：鍵（唯讀）。
  /// </summary>
  public string Key { get; private set; } = "";
  /// <summary>
  /// 公開：當前節點的當前被選中的候選字詞「在該節點內的」目前的權重（唯讀）。
  /// </summary>
  public double Score { get; private set; }
  /// <summary>
  /// 公開：當前被選中的候選字詞的鍵值配對。
  /// </summary>
  public KeyValuePaired CurrentPair =>
      _selectedUnigramIndex >= Unigrams.Count ? new() : Candidates[_selectedUnigramIndex];
  /// <summary>
  /// 公開：給出當前單元圖陣列內最高的權重數值。
  /// </summary>
  public double HighestUnigramScore => Unigrams.Count == 0 ? 0.0 : Unigrams.FirstOrDefault().Score;

  /// <summary>
  /// 初期化一個節點。
  /// </summary>
  /// <param name="key">索引鍵。</param>
  /// <param name="spanLength">幅位長度。</param>
  /// <param name="unigrams">單元圖陣列。</param>
  /// <param name="bigrams">雙元圖陣列（非必填）。</param>
  public Node(string key = "", int spanLength = 0, List<Unigram>? unigrams = null, List<Bigram>? bigrams = null) {
    Key = key;
    Unigrams = unigrams ?? new();
    Bigrams = bigrams ?? new();
    SpanLength = spanLength;
    Unigrams.Sort((a, b) => b.Score.CompareTo(a.Score));
    _valueUnigramIndexMap = new();
    if (Unigrams.Count > 0) Score = Unigrams[0].Score;
    for (int I = 0; I < Unigrams.Count; I++) {
      Unigram gram = Unigrams[I];
      _valueUnigramIndexMap[gram.KeyValue.Value] = I;
      Candidates.Add(gram.KeyValue);
    }
    foreach (Bigram gram in Bigrams.Where(gram => _precedingBigramMap.ContainsKey(gram.KeyValuePreceded))) {
      _precedingBigramMap[gram.KeyValuePreceded].Add(gram);
    }
  }
  /// <summary>
  /// 對擁有「給定的前述鍵值陣列」的節點提權。
  /// </summary>
  /// <param name="precedingKeyValues">前述鍵值陣列。</param>
  public void PrimeNodeWith(List<KeyValuePaired> precedingKeyValues) {
    int newIndex = _selectedUnigramIndex;
    double maxScore = Score;
    if (!IsCandidateFixed) {
      foreach (KeyValuePaired neta in precedingKeyValues) {
        List<Bigram> bigrams = new();
        if (_precedingBigramMap.ContainsKey(neta)) bigrams = _precedingBigramMap[neta];
        foreach (Bigram bigram in bigrams.Where(bigram => bigram.Score > maxScore)
                     .Where(bigram => _valueUnigramIndexMap.ContainsKey(bigram.KeyValue.Value))) {
          newIndex = _valueUnigramIndexMap[bigram.KeyValue.Value];
          maxScore = bigram.Score;
        }
      }
    }
    Score = maxScore;
    _selectedUnigramIndex = newIndex;
  }
  /// <summary>
  /// 選中位於給定索引位置的候選字詞。
  /// </summary>
  /// <param name="index">索引位置。</param>
  /// <param name="fix">是否將當前解點標記為「候選詞已鎖定」的狀態。</param>
  public void SelectCandidateAt(int index = 0, bool fix = false) {
    _selectedUnigramIndex = index >= Unigrams.Count ? 0 : index;
    IsCandidateFixed = fix;
    Score = ConSelectedCandidateScore;
  }
  /// <summary>
  /// 重設該節點的候選字詞狀態。
  /// </summary>
  public void ResetCandidate() {
    _selectedUnigramIndex = 0;
    IsCandidateFixed = false;
    if (Unigrams.Count != 0) Score = Unigrams.FirstOrDefault().Score;
  }
  /// <summary>
  /// 選中位於給定索引位置的候選字詞、且施加給定的權重。
  /// </summary>
  /// <param name="index">索引位置。</param>
  /// <param name="score">給定權重條件。</param>
  public void SelectFloatingCandidateAt(int index, double score) {
    int realIndex = Math.Abs(index);
    _selectedUnigramIndex = realIndex >= Unigrams.Count ? 0 : realIndex;
    IsCandidateFixed = false;
    Score = score;
  }
  /// <summary>
  /// 藉由給定的候選字詞字串，找出在庫的單元圖權重數值。沒有的話就找零。
  /// </summary>
  /// <param name="candidate">給定的候選字詞字串。</param>
  /// <returns>在庫的單元圖權重數值；沒有在庫的話就找零。</returns>
  public double ScoreFor(string candidate) {
    double result = 0.0;
    foreach (Unigram unigram in Unigrams.Where(unigram => unigram.KeyValue.Value == candidate)) {
      return unigram.Score;
    }
    return result;
  }
  /// <summary>
  /// 藉由給定的候選字詞鍵值配對，找出在庫的單元圖權重數值。沒有的話就找零。
  /// </summary>
  /// <param name="candidate">給定的候選字詞字串。</param>
  /// <returns>在庫的單元圖權重數值；沒有在庫的話就找零。</returns>
  public double ScoreForPaired(KeyValuePaired candidate) {
    double result = 0.0;
    foreach (Unigram unigram in Unigrams.Where(unigram => unigram.KeyValue == candidate)) {
      return unigram.Score;
    }
    return result;
  }
  /// <summary>
  /// 判定兩個節點是否相等。
  /// </summary>
  /// <param name="obj">用來比較的節點。</param>
  /// <returns>若相等，則返回 true。</returns>
  public override bool Equals(object obj) {
    return obj is Node node && EqualityComparer<List<Unigram>>.Default.Equals(Unigrams, node.Unigrams) &&
           EqualityComparer<List<KeyValuePaired>>.Default.Equals(Candidates, node.Candidates) &&
           EqualityComparer<Dictionary<string, int>>.Default.Equals(_valueUnigramIndexMap,
                                                                    node._valueUnigramIndexMap) &&
           EqualityComparer<Dictionary<KeyValuePaired, List<Bigram>>>.Default.Equals(_precedingBigramMap,
                                                                                     node._precedingBigramMap) &&
           IsCandidateFixed == node.IsCandidateFixed && _selectedUnigramIndex == node._selectedUnigramIndex;
  }

  /// <summary>
  /// 將當前節點的內容輸出為雜湊資料。
  /// </summary>
  /// <returns>當前節錨的內容輸出成的雜湊資料。</returns>
  public override int GetHashCode() {
    int a1 = HashCode.Combine(Key, Score, Unigrams, Bigrams);
    int a2 = HashCode.Combine(_precedingBigramMap, SpanLength, Candidates, _valueUnigramIndexMap);
    int a3 = HashCode.Combine(_precedingBigramMap, IsCandidateFixed, _selectedUnigramIndex);
    string combined = a1.ToString() + a2.ToString() + a3.ToString();
    return HashCode.Combine(combined);
  }
}
}