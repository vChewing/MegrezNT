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
public class Compositor {
  private const double ConDroppedPathScore = -999;
  private int MutCursorIndex = 0;
  private List<string> MutReadings = new();
  private Grid MutGrid = new();
  private LanguageModel MutLM = new();
  public int MaxBuildSpanLength => MutGrid.MaxBuildSpanLength;
  public string JoinSeparator = "";
  public int CursorIndex {
    get => MutCursorIndex;
    set => MutCursorIndex = (value < 0) ? 0 : Math.Min(value, MutReadings.Count);
  }
  public Grid Grid => MutGrid;
  public int Length => MutReadings.Count;
  public List<string> Readings => MutReadings;
  // MARK: - Initialization
  public Compositor(LanguageModel Lm, int Length = 10, string Separator = "") {
    MutLM = Lm;
    MutGrid = new Grid(Math.Abs(Length));
    JoinSeparator = Separator;
  }
  // MARK: - 分節讀音槽自我清空專用函數
  public void Clear() {
    MutCursorIndex = 0;
    MutReadings.Clear();
    MutGrid.Clear();
  }
  // MARK: - 在游標位置插入給定的讀音
  public void InsertReadingAtCursor(string Reading) {
    MutReadings.Insert(MutCursorIndex, Reading);
    MutGrid.ExpandGridByOneAt(MutCursorIndex);
    Build();
    MutCursorIndex += 1;
  }
  // 朝著與文字輸入方向相反的方向、砍掉一個與游標相鄰的讀音。
  // 在威注音的術語體系當中，「與文字輸入方向相反的方向」為向後（Rear）。
  public bool DeleteReadingAtTheRearOfCursor() {
    if (MutCursorIndex == 0) return false;
    MutReadings.RemoveAt(MutCursorIndex - 1);
    MutCursorIndex -= 1;
    MutGrid.ShrinkGridByOneAt(MutCursorIndex);
    Build();
    return true;
  }
  // 朝著往文字輸入方向、砍掉一個與游標相鄰的讀音。
  // 在威注音的術語體系當中，「文字輸入方向」為向前（Front）。
  public bool DeleteReadingToTheFrontOfCursor() {
    if (MutCursorIndex == MutReadings.Count) return false;
    MutReadings.RemoveAt(MutCursorIndex);
    MutGrid.ShrinkGridByOneAt(MutCursorIndex);
    Build();
    return true;
  }
  // 移除該分節讀音槽的第一個讀音單元。用於輸入法組字區長度上限處理：
  // 將該位置要溢出的敲字內容遞交之後、再執行這個函數。
  public bool RemoveHeadReadings(int Count) {
    int TheCount = Math.Abs(Count);
    if (TheCount > Length) return false;
    for (int I = 0; I < TheCount; I++) {
      if (MutCursorIndex > 0) MutCursorIndex -= 1;
      if (MutReadings.Count > 0) {
        MutReadings.RemoveAt(0);
        MutGrid.ShrinkGridByOneAt(0);
      }
      Build();
    }
    return true;
  }
  // MARK: - Walker (non-reversed)
  public List<NodeAnchor> Walk(int Location, double AccumulatedScore = 0.0, string JoinedPhrase = "",
                               List<String> LongPhrases = default) {
    int NewLocation = MutGrid.Width - Math.Abs(Location);
    List<NodeAnchor> Result = ReverseWalk(NewLocation, AccumulatedScore, JoinedPhrase, LongPhrases);
    Result.Reverse();
    return Result;
  }
  // Mark: - Walker (reversed)
  public List<NodeAnchor> ReverseWalk(int Location, double AccumulatedScore = 0.0, string JoinedPhrase = "",
                                      List<String> LongPhrases = default) {
    if (LongPhrases == null) LongPhrases = new();
    Location = Math.Abs(Location);
    if (Location == 0 || Location > MutGrid.Width) return new();
    List<List<NodeAnchor>> Paths = new();
    List<NodeAnchor> Nodes = MutGrid.NodesEndingAt(Location);
    Nodes = Nodes.OrderByDescending(A => A.ScoreForSort).ToList();
    if (Nodes.FirstOrDefault().Node == null) return new();

    Node ZeroNode = Nodes.FirstOrDefault().Node ?? new("", new());

    if (ZeroNode.Score >= ZeroNode.ConSelectedCandidateScore) {
      NodeAnchor ZeroAnchor = Nodes.FirstOrDefault();
      ZeroAnchor.AccumulatedScore = AccumulatedScore + ZeroNode.Score;
      List<NodeAnchor> Path = ReverseWalk(Location - ZeroAnchor.SpanningLength, ZeroAnchor.AccumulatedScore);
      Path.Insert(0, ZeroAnchor);
      Paths.Add(Path);
    } else if (LongPhrases.Count > 0) {
      List<NodeAnchor> Path = new();
      for (int I = 0; I < Nodes.Count; I++) {
        NodeAnchor TheAnchor = Nodes[I];
        if (TheAnchor.Node == null) continue;
        Node TheNode = TheAnchor.Node;
        string JoinedValue = TheNode.CurrentKeyValue + JoinedPhrase;
        // 如果只是一堆單漢字的節點組成了同樣的長詞的話，直接棄用這個節點路徑。
        // 打比方說「八/月/中/秋/山/林/涼」與「八月/中秋/山林/涼」在使用者來看
        // 是「結果等價」的，那就扔掉前者。
        if (LongPhrases.Contains(JoinedValue)) {
          TheAnchor.AccumulatedScore = ConDroppedPathScore;
          Path.Insert(0, TheAnchor);
          Paths.Add(Path);
          continue;
        }
        TheAnchor.AccumulatedScore = AccumulatedScore + TheNode.Score;
        Path = ReverseWalk(Location - TheAnchor.SpanningLength, TheAnchor.AccumulatedScore,
                           (JoinedValue.Length >= (LongPhrases.FirstOrDefault() ?? "").Length) ? "" : JoinedValue,
                           LongPhrases);
        Path.Insert(0, TheAnchor);
        Paths.Add(Path);
      }
    } else {
      // 看看當前格位有沒有更長的候選字詞。
      LongPhrases.Clear();
      foreach (NodeAnchor TheAnchor in Nodes) {
        if (TheAnchor.Node == null) continue;
        Node TheNode = TheAnchor.Node;
        if (TheAnchor.SpanningLength > 1) LongPhrases.Add(TheNode.CurrentKeyValue.Value);
      }
      LongPhrases = LongPhrases.OrderByDescending(A => A.Length).ToList();
      for (int I = 0; I < Nodes.Count; I++) {
        NodeAnchor TheAnchor = Nodes[I];
        if (TheAnchor.Node == null) continue;
        Node TheNode = TheAnchor.Node;
        TheAnchor.AccumulatedScore = AccumulatedScore + TheNode.Score;
        List<NodeAnchor> Path =
            ReverseWalk(Location - TheAnchor.SpanningLength, TheAnchor.AccumulatedScore,
                        (TheAnchor.SpanningLength > 1) ? "" : TheNode.CurrentKeyValue.Value, LongPhrases);
        Path.Insert(0, TheAnchor);
        Paths.Add(Path);
      }
    }

    List<NodeAnchor> Result = Paths.FirstOrDefault() ?? new();
    foreach (List<NodeAnchor> Neta in Paths) {
      if (Neta.LastOrDefault().AccumulatedScore > Result.LastOrDefault().AccumulatedScore) Result = Neta;
    }

    return Result;
  }
  // MARK: - Private Functions
  private void Build() {
    int ItrBegin = (MutCursorIndex < MaxBuildSpanLength) ? 0 : MutCursorIndex - MaxBuildSpanLength;
    int ItrEnd = Math.Min(MutCursorIndex + MaxBuildSpanLength, MutReadings.Count);
    for (int P = ItrBegin; P < ItrEnd; P++) {
      for (int Q = 1; Q < MaxBuildSpanLength; Q++) {
        if (P + Q > ItrEnd) break;
        List<string> ArrSlice = MutReadings.GetRange(P, Q);
        string CombinedReading = Join(ArrSlice, JoinSeparator);
        if (!MutGrid.HasMatchedNode(P, Q, CombinedReading)) {
          List<Unigram> Unigrams = MutLM.UnigramsFor(CombinedReading);
          if (Unigrams.Count > 0) {
            Node? N = new Node(CombinedReading, Unigrams);
            if (N != null) {
              MutGrid.InsertNode(N, P, Q);
            }
          }
        }
      }
    }
  }
  private string Join(List<string> Slice, string Separator) {
    List<string> ArrResult = new();
    foreach (string Item in Slice) {
      ArrResult.Add(Item);
    }
    return String.Join(Separator, ArrResult);
  }
}
}