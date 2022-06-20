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
/// 組字器。
/// </summary>
public class Compositor {
  /// <summary>
  /// 給被丟掉的節點路徑施加的負權重。
  /// </summary>
  private const double ConDroppedPathScore = -999;
  /// <summary>
  /// 該組字器的游標位置。
  /// </summary>
  private int MutCursorIndex;
  /// <summary>
  /// 該組字器所使用的語言模型。
  /// </summary>
  private LanguageModel MutLM = new();
  /// <summary>
  /// 公開：該組字器內可以允許的最大詞長。
  /// </summary>
  public int MaxBuildSpanLength => Grid.MaxBuildSpanLength;
  /// <summary>
  /// 公開：多字讀音鍵當中用以分割漢字讀音的記號，預設為空。
  /// </summary>
  public string JoinSeparator = "";
  /// <summary>
  /// 公開：該組字器的游標位置。
  /// </summary>
  public int CursorIndex {
    get => MutCursorIndex;
    set => MutCursorIndex = value < 0 ? 0 : Math.Min(value, Readings.Count);
  }
  /// <summary>
  /// 公開：該組字器是否為空。
  /// </summary>
  public bool IsEmpty => Grid.IsEmpty;
  /// <summary>
  /// 公開：該組字器的軌格（唯讀）。
  /// </summary>
  public Grid Grid { get; } = new();
  /// <summary>
  /// 公開：該組字器的長度，也就是內建漢字讀音的數量（唯讀）。
  /// </summary>
  public int Length => Readings.Count;
  /// <summary>
  /// 公開：該組字器的讀音陣列（唯讀）。
  /// </summary>
  public List<string> Readings { get; } = new();
  /// <summary>
  /// 組字器。
  /// </summary>
  /// <param name="Lm">語言模型。可以是任何基於 Megrez.LanguageModel 的衍生型別。</param>
  /// <param name="Length">指定該組字器內可以允許的最大詞長，預設為 10 字。</param>
  /// <param name="Separator">多字讀音鍵當中用以分割漢字讀音的記號，預設為空。</param>
  public Compositor(LanguageModel Lm, int Length = 10, string Separator = "") {
    MutLM = Lm;
    Grid = new(Math.Abs(Length));
    JoinSeparator = Separator;
  }
  /// <summary>
  /// 組字器自我清空專用函式。
  /// </summary>
  public void Clear() {
    MutCursorIndex = 0;
    Readings.Clear();
    Grid.Clear();
  }
  /// <summary>
  /// 在游標位置插入給定的讀音。
  /// </summary>
  /// <param name="Reading">要插入的讀音。</param>
  public void InsertReadingAtCursor(string Reading) {
    Readings.Insert(MutCursorIndex, Reading);
    Grid.ExpandGridByOneAt(MutCursorIndex);
    Build();
    MutCursorIndex += 1;
  }
  /// <summary>
  /// 朝著與文字輸入方向相反的方向、砍掉一個與游標相鄰的讀音。<br />
  /// 在威注音的術語體系當中，「與文字輸入方向相反的方向」為向後（Rear）。
  /// </summary>
  /// <returns>結果是否成功執行。</returns>
  public bool DeleteReadingAtTheRearOfCursor() {
    if (MutCursorIndex == 0) return false;
    Readings.RemoveAt(MutCursorIndex - 1);
    MutCursorIndex -= 1;
    Grid.ShrinkGridByOneAt(MutCursorIndex);
    Build();
    return true;
  }
  /// <summary>
  /// 朝著往文字輸入方向、砍掉一個與游標相鄰的讀音。<br />
  /// 在威注音的術語體系當中，「文字輸入方向」為向前（Front）。
  /// </summary>
  /// <returns>結果是否成功執行。</returns>
  public bool DeleteReadingToTheFrontOfCursor() {
    if (MutCursorIndex == Readings.Count) return false;
    Readings.RemoveAt(MutCursorIndex);
    Grid.ShrinkGridByOneAt(MutCursorIndex);
    Build();
    return true;
  }
  /// <summary>
  /// 移除該組字器最先被輸入的第 X 個讀音單元。<br />
  /// 用於輸入法組字區長度上限處理：<br />
  /// 將該位置要溢出的敲字內容遞交之後、再執行這個函式。
  /// </summary>
  /// <param name="Count">要移除的讀音單位數量。</param>
  /// <returns>結果是否成功執行。</returns>
  public bool RemoveHeadReadings(int Count) {
    int TheCount = Math.Abs(Count);
    if (TheCount > Length) return false;
    for (int I = 0; I < TheCount; I++) {
      if (MutCursorIndex > 0) MutCursorIndex -= 1;
      if (Readings.Count > 0) {
        Readings.RemoveAt(0);
        Grid.ShrinkGridByOneAt(0);
      }
      Build();
    }
    return true;
  }
  /// <summary>
  /// 對已給定的軌格按照給定的位置與條件進行正向爬軌。
  /// </summary>
  /// <param name="Location">開始爬軌的位置。</param>
  /// <param name="AccumulatedScore">給定累計權重，非必填參數。預設值為 0。</param>
  /// <param name="JoinedPhrase">用以統計累計長詞的內部參數，請勿主動使用。</param>
  /// <param name="LongPhrases">用以統計累計長詞的內部參數，請勿主動使用。</param>
  /// <returns>均有節點的節錨陣列。</returns>
  public List<NodeAnchor> Walk(int Location, double AccumulatedScore = 0.0, string JoinedPhrase = "",
                               List<string> LongPhrases = default(List<string>)) {
    int NewLocation = Grid.Width - Math.Abs(Location);
    List<NodeAnchor> Result = ReverseWalk(NewLocation, AccumulatedScore, JoinedPhrase, LongPhrases);
    Result.Reverse();
    return Result;
  }
  /// <summary>
  /// 對已給定的軌格按照給定的位置與條件進行反向爬軌。
  /// </summary>
  /// <param name="Location">開始爬軌的位置。</param>
  /// <param name="AccumulatedScore">給定累計權重，非必填參數。預設值為 0。</param>
  /// <param name="JoinedPhrase">用以統計累計長詞的內部參數，請勿主動使用。</param>
  /// <param name="LongPhrases">用以統計累計長詞的內部參數，請勿主動使用。</param>
  /// <returns>均有節點的節錨陣列。</returns>
  public List<NodeAnchor> ReverseWalk(int Location, double AccumulatedScore = 0.0, string JoinedPhrase = "",
                                      List<string> LongPhrases = default(List<string>)) {
    LongPhrases ??= new();  // 不要聽 IntelliSense 放屁。這一行不能砍，否則直接炸。
    Location = Math.Abs(Location);
    if (Location == 0 || Location > Grid.Width) return new();
    List<List<NodeAnchor>> Paths = new();
    List<NodeAnchor> Nodes = Grid.NodesEndingAt(Location);
    Nodes = Nodes.OrderByDescending(A => A.ScoreForSort).ToList();
    if (Nodes.FirstOrDefault().Node == null) return new();

    Node ZeroNode = Nodes.FirstOrDefault().Node ?? new("", new());

    if (ZeroNode.Score >= Node.ConSelectedCandidateScore) {
      NodeAnchor ZeroAnchor = Nodes.FirstOrDefault();
      ZeroAnchor.AccumulatedScore = AccumulatedScore + ZeroNode.Score;
      List<NodeAnchor> Path = ReverseWalk(Location - ZeroAnchor.SpanningLength, ZeroAnchor.AccumulatedScore);
      Path.Insert(0, ZeroAnchor);
      Paths.Add(Path);
    } else if (LongPhrases.Count > 0) {
      List<NodeAnchor> Path = new();
      foreach (NodeAnchor T in Nodes) {
        NodeAnchor TheAnchor = T;
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
                           JoinedValue.Length >= (LongPhrases.FirstOrDefault() ?? "").Length ? "" : JoinedValue,
                           LongPhrases);
        Path.Insert(0, TheAnchor);
        Paths.Add(Path);
      }
    } else {
      // 看看當前格位有沒有更長的候選字詞。
      LongPhrases.Clear();
      LongPhrases.AddRange(from TheAnchor in Nodes where TheAnchor.Node != null let TheNode = TheAnchor.Node where TheAnchor.SpanningLength > 1 select TheNode.CurrentKeyValue.Value);
      LongPhrases = LongPhrases.OrderByDescending(A => A.Length).ToList();
      foreach (NodeAnchor T in Nodes) {
        NodeAnchor TheAnchor = T;
        if (TheAnchor.Node == null) continue;
        Node TheNode = TheAnchor.Node;
        TheAnchor.AccumulatedScore = AccumulatedScore + TheNode.Score;
        List<NodeAnchor> Path =
            ReverseWalk(Location - TheAnchor.SpanningLength, TheAnchor.AccumulatedScore,
                        TheAnchor.SpanningLength > 1 ? "" : TheNode.CurrentKeyValue.Value, LongPhrases);
        Path.Insert(0, TheAnchor);
        Paths.Add(Path);
      }
    }

    List<NodeAnchor> Result = Paths.FirstOrDefault() ?? new();
    foreach (List<NodeAnchor> Neta in Paths.Where(Neta => Neta.LastOrDefault().AccumulatedScore >
                                                          Result.LastOrDefault().AccumulatedScore)) {
      Result = Neta;
    }

    return Result;
  }
  // MARK: - Private Functions
  private void Build() {
    int ItrBegin = MutCursorIndex < MaxBuildSpanLength ? 0 : MutCursorIndex - MaxBuildSpanLength;
    int ItrEnd = Math.Min(MutCursorIndex + MaxBuildSpanLength, Readings.Count);
    for (int P = ItrBegin; P < ItrEnd; P++) {
      for (int Q = 1; Q < MaxBuildSpanLength; Q++) {
        if (P + Q > ItrEnd) break;
        List<string> ArrSlice = Readings.GetRange(P, Q);
        string CombinedReading = Join(ArrSlice, JoinSeparator);
        if (Grid.HasMatchedNode(P, Q, CombinedReading)) continue;
        List<Unigram> Unigrams = MutLM.UnigramsFor(CombinedReading);
        if (Unigrams.Count == 0) continue;
        Node N = new(CombinedReading, Unigrams);
        Grid.InsertNode(N, P, Q);
      }
    }
  }
  private static string Join(IEnumerable<string> Slice, string Separator) => string.Join(Separator, Slice.ToList());
}
}