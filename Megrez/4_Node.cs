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
  /// 當前節點對應的語言模型。
  /// </summary>
  private LanguageModel MutLM = new();
  /// <summary>
  /// 鍵。
  /// </summary>
  private string MutKey = "";
  /// <summary>
  /// 當前節點的當前被選中的候選字詞「在該節點內的」目前的權重。
  /// </summary>
  private double MutScore = 0;
  /// <summary>
  /// 單元圖陣列。
  /// </summary>
  private List<Unigram> MutUnigrams = new();
  /// <summary>
  /// 雙元圖陣列。
  /// </summary>
  private List<Bigram> MutBigrams = new();
  /// <summary>
  /// 候選字詞陣列，以鍵值陣列的形式存在。
  /// </summary>
  private List<KeyValuePair> MutCandidates = new();
  /// <summary>
  /// 專門「用單元圖資料值來調查索引值」的辭典。
  /// </summary>
  private Dictionary<string, int> MutValueUnigramIndexMap = new();
  /// <summary>
  /// 專門「用給定鍵值來取對應的雙元圖陣列」的辭典。
  /// </summary>
  private Dictionary<KeyValuePair, List<Bigram>> MutPrecedingBigramMap = new();
  /// <summary>
  /// 狀態標記變數，用來記載當前節點是否處於候選字詞鎖定狀態。
  /// </summary>
  private bool MutCandidateFixed = false;
  /// <summary>
  /// 用來登記「當前選中的單元圖」的索引值的變數。
  /// </summary>
  private int MutSelectedUnigramIndex = 0;

  /// <summary>
  /// 用來登記要施加給「『被標記為選中狀態』的候選字詞」的複寫權重的數值。
  /// </summary>
  public double ConSelectedCandidateScore = 99.0;
  public override string ToString() =>
      $"(node,key:{MutKey},fixed:{(MutCandidateFixed ? "true" : "false")},selected:{MutSelectedUnigramIndex},{MutUnigrams})";
  /// <summary>
  /// 公開：候選字詞陣列（唯讀），以鍵值陣列的形式存在。
  /// </summary>
  public List<KeyValuePair> Candidates => MutCandidates;
  /// <summary>
  /// 公開：用來登記「當前選中的單元圖」的索引值的變數（唯讀）。
  /// </summary>
  public bool IsCandidateFixed => MutCandidateFixed;
  /// <summary>
  /// 公開：鍵（唯讀）。
  /// </summary>
  public string Key => MutKey;
  /// <summary>
  /// 公開：當前節點的當前被選中的候選字詞「在該節點內的」目前的權重（唯讀）。
  /// </summary>
  public double Score => MutScore;
  /// <summary>
  /// 公開：當前被選中的候選字詞的鍵值配對。
  /// </summary>
  public KeyValuePair CurrentKeyValue =>
      MutSelectedUnigramIndex >= MutUnigrams.Count ? new() : MutCandidates[MutSelectedUnigramIndex];
  /// <summary>
  /// 公開：給出當前單元圖陣列內最高的權重數值。
  /// </summary>
  public double HighestUnigramScore => MutUnigrams.Count == 0 ? 0.0 : MutUnigrams.FirstOrDefault().Score;

  /// <summary>
  /// 初期化一個節點。
  /// </summary>
  /// <param name="Key">索引鍵。</param>
  /// <param name="Unigrams">單元圖陣列。</param>
  /// <param name="Bigrams">雙元圖陣列（非必填）。</param>
  public Node(string Key, List<Unigram> Unigrams, List<Bigram>? Bigrams = null) {
    this.MutKey = Key;
    this.MutUnigrams = Unigrams;
    this.MutBigrams = Bigrams ?? new();
    MutUnigrams.Sort((A, B) => B.Score.CompareTo(A.Score));
    if (MutUnigrams.Count > 0) MutScore = MutUnigrams[0].Score;
    for (int I = 0; I < MutUnigrams.Count; I++) {
      Unigram Gram = MutUnigrams[I];
      MutValueUnigramIndexMap[Gram.KeyValue.Value] = I;
      MutCandidates.Add(Gram.KeyValue);
    }
    foreach (Bigram Gram in MutBigrams) {
      if (MutPrecedingBigramMap.ContainsKey(Gram.KeyValuePreceded)) {
        MutPrecedingBigramMap[Gram.KeyValuePreceded].Add(Gram);
      }
    }
  }
  /// <summary>
  /// 對擁有「給定的前述鍵值陣列」的節點提權。
  /// </summary>
  /// <param name="PrecedingKeyValues">前述鍵值陣列。</param>
  public void PrimeNodeWith(List<KeyValuePair> PrecedingKeyValues) {
    int NewIndex = MutSelectedUnigramIndex;
    double MaxScore = MutScore;
    if (!IsCandidateFixed) {
      foreach (KeyValuePair Neta in PrecedingKeyValues) {
        List<Bigram> Bigrams = MutPrecedingBigramMap[Neta] ?? new();
        foreach (Bigram Bigram in Bigrams) {
          if (!(Bigram.Score > MaxScore)) continue;
          if (!MutValueUnigramIndexMap.ContainsKey(Bigram.KeyValue.Value)) continue;
          NewIndex = MutValueUnigramIndexMap[Bigram.KeyValue.Value];
          MaxScore = Bigram.Score;
        }
      }
    }
    MutScore = MaxScore;
    MutSelectedUnigramIndex = NewIndex;
  }
  /// <summary>
  /// 選中位於給定索引位置的候選字詞。
  /// </summary>
  /// <param name="Index">索引位置。</param>
  /// <param name="Fix">是否將當前解點標記為「候選詞已鎖定」的狀態。</param>
  public void SelectCandidateAt(int Index = 0, bool Fix = false) {
    MutSelectedUnigramIndex = (Index >= MutUnigrams.Count) ? 0 : Index;
    MutCandidateFixed = Fix;
    MutScore = ConSelectedCandidateScore;
  }
  /// <summary>
  /// 重設該節點的候選字詞狀態。
  /// </summary>
  public void ResetCandidate() {
    MutSelectedUnigramIndex = 0;
    MutCandidateFixed = false;
    if (MutUnigrams.Count != 0) MutScore = MutUnigrams.FirstOrDefault().Score;
  }
  /// <summary>
  /// 選中位於給定索引位置的候選字詞、且施加給定的權重。
  /// </summary>
  /// <param name="Index">索引位置。</param>
  /// <param name="Score">給定權重條件。</param>
  public void SelectFloatingCandidateAt(int Index, double Score) {
    int RealIndex = Math.Abs(Index);
    MutSelectedUnigramIndex = (RealIndex >= MutUnigrams.Count) ? 0 : RealIndex;
    MutCandidateFixed = false;
    MutScore = Score;
  }
  /// <summary>
  /// 藉由給定的候選字詞字串，找出在庫的單元圖權重數值。沒有的話就找零。
  /// </summary>
  /// <param name="Candidate">給定的候選字詞字串。</param>
  /// <returns>在庫的單元圖權重數值；沒有在庫的話就找零。</returns>
  public double ScoreFor(string Candidate) {
    double Result = 0.0;
    foreach (Unigram Unigram in MutUnigrams) {
      if (Unigram.KeyValue.Value == Candidate) {
        Result = Unigram.Score;
      }
    }
    return Result;
  }
  public override bool Equals(object Obj) {
    return Obj is Node Node && EqualityComparer<List<Unigram>>.Default.Equals(MutUnigrams, Node.MutUnigrams) &&
           EqualityComparer<List<KeyValuePair>>.Default.Equals(MutCandidates, Node.MutCandidates) &&
           EqualityComparer<Dictionary<string, int>>.Default.Equals(MutValueUnigramIndexMap,
                                                                    Node.MutValueUnigramIndexMap) &&
           EqualityComparer<Dictionary<KeyValuePair, List<Bigram>>>.Default.Equals(MutPrecedingBigramMap,
                                                                                   Node.MutPrecedingBigramMap) &&
           MutCandidateFixed == Node.MutCandidateFixed && MutSelectedUnigramIndex == Node.MutSelectedUnigramIndex;
  }

  public override int GetHashCode() {
    unchecked { return (int)BitConverter.ToInt64(Convert.FromBase64String(ToString()), 0); }
  }
}
}
