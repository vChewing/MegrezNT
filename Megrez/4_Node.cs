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
public class Node {
  // MARK: - 內部變數
  private LanguageModel MutLM = new();
  private string MutKey = "";
  private double MutScore = 0;
  private List<Unigram> MutUnigrams = new();
  private List<Bigram> MutBigrams = new();
  private List<KeyValuePair> MutCandidates = new();
  private Dictionary<string, int> MutValueUnigramIndexMap = new();
  private Dictionary<KeyValuePair, List<Bigram>> MutPrecedingBigramMap = new();
  private bool MutCandidateFixed = false;
  private int MutSelectedUnigramIndex = 0;
  // MARK: - 公開變數
  public double ConSelectedCandidateScore = 99.0;
  public override string ToString() =>
      $"(node,key:{MutKey},fixed:{(MutCandidateFixed ? "true" : "false")},selected:{MutSelectedUnigramIndex},{MutUnigrams})";
  public List<KeyValuePair> Candidates => MutCandidates;
  public bool IsCandidateFixed => MutCandidateFixed;
  public string Key => MutKey;
  public double Score => MutScore;
  public KeyValuePair CurrentKeyValue =>
      MutSelectedUnigramIndex >= MutUnigrams.Count ? new() : MutCandidates[MutSelectedUnigramIndex];
  public double HighestUnigramScore => MutUnigrams.Count == 0 ? 0.0 : MutUnigrams.FirstOrDefault().Score;
  // MARK: - 初期化
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
  // MARK: - 對擁有「給定的前述鍵值陣列」的節點提權
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
  // MARK: - 選中位於給定索引位置的候選字詞
  public void SelectCandidateAt(int Index = 0, bool Fix = false) {
    MutSelectedUnigramIndex = (Index >= MutUnigrams.Count) ? 0 : Index;
    MutCandidateFixed = Fix;
    MutScore = ConSelectedCandidateScore;
  }
  // MARK: - 重設該節點的候選字詞狀態
  public void ResetCandidate() {
    MutSelectedUnigramIndex = 0;
    MutCandidateFixed = false;
    if (MutUnigrams.Count != 0) MutScore = MutUnigrams.FirstOrDefault().Score;
  }
  // MARK: - 選中位於給定索引位置的候選字詞、且施加給定的權重
  public void SelectFloatingCandidateAt(int Index, double Score) {
    int RealIndex = Math.Abs(Index);
    MutSelectedUnigramIndex = (RealIndex >= MutUnigrams.Count) ? 0 : RealIndex;
    MutCandidateFixed = false;
    MutScore = Score;
  }
  // MARK: - 藉由給定的候選字詞字串，找出在庫的單元圖權重數值。沒有的話就找零
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
