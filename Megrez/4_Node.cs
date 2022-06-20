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
  private List<Unigram> _unigrams;
  /// <summary>
  /// 雙元圖陣列。
  /// </summary>
  private List<Bigram> _bigrams;
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
  /// 用來登記要施加給「『被標記為選中狀態』的候選字詞」的複寫權重的數值。
  /// </summary>
  public const double ConSelectedCandidateScore = 99.0;
  public override string ToString() =>
      $"(node,key:{Key},fixed:{(IsCandidateFixed ? "true" : "false")},selected:{_selectedUnigramIndex},{_unigrams})";
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
  public string Key { get; } = "";
  /// <summary>
  /// 公開：當前節點的當前被選中的候選字詞「在該節點內的」目前的權重（唯讀）。
  /// </summary>
  public double Score { get; private set; }
  /// <summary>
  /// 公開：當前被選中的候選字詞的鍵值配對。
  /// </summary>
  public KeyValuePaired CurrentKeyValue =>
      _selectedUnigramIndex >= _unigrams.Count ? new() : Candidates[_selectedUnigramIndex];
  /// <summary>
  /// 公開：給出當前單元圖陣列內最高的權重數值。
  /// </summary>
  public double HighestUnigramScore => _unigrams.Count == 0 ? 0.0 : _unigrams.FirstOrDefault().Score;

  /// <summary>
  /// 初期化一個節點。
  /// </summary>
  /// <param name="key">索引鍵。</param>
  /// <param name="unigrams">單元圖陣列。</param>
  /// <param name="bigrams">雙元圖陣列（非必填）。</param>
  public Node(string key, List<Unigram> unigrams, List<Bigram>? bigrams = null) {
    Key = key;
    _unigrams = unigrams;
    _bigrams = bigrams ?? new();
    _unigrams.Sort((a, b) => b.Score.CompareTo(a.Score));
    _valueUnigramIndexMap = new();
    if (_unigrams.Count > 0) Score = _unigrams[0].Score;
    for (int I = 0; I < _unigrams.Count; I++) {
      Unigram gram = _unigrams[I];
      _valueUnigramIndexMap[gram.KeyValue.Value] = I;
      Candidates.Add(gram.KeyValue);
    }
    foreach (Bigram gram in _bigrams.Where(gram => _precedingBigramMap.ContainsKey(gram.KeyValuePreceded))) {
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
    _selectedUnigramIndex = index >= _unigrams.Count ? 0 : index;
    IsCandidateFixed = fix;
    Score = ConSelectedCandidateScore;
  }
  /// <summary>
  /// 重設該節點的候選字詞狀態。
  /// </summary>
  public void ResetCandidate() {
    _selectedUnigramIndex = 0;
    IsCandidateFixed = false;
    if (_unigrams.Count != 0) Score = _unigrams.FirstOrDefault().Score;
  }
  /// <summary>
  /// 選中位於給定索引位置的候選字詞、且施加給定的權重。
  /// </summary>
  /// <param name="index">索引位置。</param>
  /// <param name="score">給定權重條件。</param>
  public void SelectFloatingCandidateAt(int index, double score) {
    int realIndex = Math.Abs(index);
    _selectedUnigramIndex = realIndex >= _unigrams.Count ? 0 : realIndex;
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
    foreach (Unigram unigram in _unigrams.Where(unigram => unigram.KeyValue.Value == candidate)) {
      result = unigram.Score;
    }
    return result;
  }
  public override bool Equals(object obj) {
    return obj is Node node && EqualityComparer<List<Unigram>>.Default.Equals(_unigrams, node._unigrams) &&
           EqualityComparer<List<KeyValuePaired>>.Default.Equals(Candidates, node.Candidates) &&
           EqualityComparer<Dictionary<string, int>>.Default.Equals(_valueUnigramIndexMap,
                                                                    node._valueUnigramIndexMap) &&
           EqualityComparer<Dictionary<KeyValuePaired, List<Bigram>>>.Default.Equals(_precedingBigramMap,
                                                                                     node._precedingBigramMap) &&
           IsCandidateFixed == node.IsCandidateFixed && _selectedUnigramIndex == node._selectedUnigramIndex;
  }

  public override int GetHashCode() {
    unchecked { return (int)BitConverter.ToInt64(Convert.FromBase64String(ToString()), 0); }
  }
}
}