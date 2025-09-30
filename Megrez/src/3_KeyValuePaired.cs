// (c) 2022 and onwards The vChewing Project (LGPL v3.0 License or later).
// ====================
// This code is released under the SPDX-License-Identifier: `LGPL-3.0-or-later`.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Megrez {
  /// <summary>
  /// 鍵值配對，乃索引鍵陣列與讀音的配對單元。
  /// </summary>
  public class KeyValuePaired : IEquatable<KeyValuePaired>, IComparable<KeyValuePaired> {
    private const double ScoreTolerance = 1e-12;

    /// <summary>
    /// 索引鍵陣列。一般情況下用來放置讀音等可以用來作為索引的內容。
    /// </summary>
    public List<string> KeyArray { get; }
    /// <summary>
    /// 資料值，通常是詞語或單個字。
    /// </summary>
    public string Value { get; }
    /// <summary>
    /// 權重（雙精度小數）。
    /// </summary>
    public double Score { get; }

    /// <summary>
    /// 初期化一組鍵值配對。
    /// </summary>
    /// <param name="keyArray">索引鍵陣列。一般情況下用來放置讀音等可以用來作為索引的內容。</param>
    /// <param name="value">資料值。</param>
    /// <param name="score">權重（雙精度小數）。</param>
    public KeyValuePaired(List<string>? keyArray = null, string value = "N/A", double score = 0) {
      List<string> sanitizedKeyArray = (keyArray ?? new List<string>()).ToList();
      if (sanitizedKeyArray.Count == 0) {
        sanitizedKeyArray = new List<string> { "N/A" };
      }
      KeyArray = sanitizedKeyArray;
      Value = string.IsNullOrEmpty(value) ? "N/A" : value;
      Score = score;
    }

    /// <summary>
    /// 初期化一組鍵值配對。
    /// </summary>
    /// <param name="tripletExpression">傳入的通用陣列表達形式。</param>
    public KeyValuePaired(Tuple<List<string>, string, double> tripletExpression)
      : this(
          tripletExpression?.Item1?.ToList() ?? new List<string>(),
          tripletExpression?.Item2 ?? "N/A",
          tripletExpression?.Item3 ?? 0
        ) { }

    /// <summary>
    /// 初期化一組鍵值配對。
    /// </summary>
    /// <param name="tupletExpression">傳入的通用陣列表達形式。</param>
    public KeyValuePaired(Tuple<List<string>, string> tupletExpression)
      : this(
          tupletExpression?.Item1?.ToList() ?? new List<string>(),
          tupletExpression?.Item2 ?? "N/A",
          0
        ) { }

    /// <summary>
    /// 初期化一組鍵值配對。
    /// </summary>
    /// <param name="key">索引鍵。一般情況下用來放置讀音等可以用來作為索引的內容。</param>
    /// <param name="value">資料值。</param>
    /// <param name="score">權重（雙精度小數）。</param>
    public KeyValuePaired(string key = "N/A", string value = "N/A", double score = 0)
      : this(SliceKey(key), value, score) { }

    /// <summary>
    /// 通用陣列表達形式。
    /// </summary>
    public Tuple<List<string>, string> KeyValueTuplet => Tuple.Create(KeyArray.ToList(), Value);

    /// <summary>
    /// 通用陣列表達形式。
    /// </summary>
    public Tuple<List<string>, string, double> Triplet => Tuple.Create(KeyArray.ToList(), Value, Score);

    /// <summary>
    /// 生成該鍵值配對的拷貝。
    /// </summary>
    public KeyValuePaired HardCopy => new(KeyArray.ToList(), Value, Score);

    /// <summary>
    /// 將索引鍵按照給定的分隔符銜接成一個字串。
    /// </summary>
    /// <param name="separator">給定的分隔符，預設值為 <see cref="Compositor.TheSeparator"/>。</param>
    /// <returns>已經銜接完畢的字串。</returns>
    public string JoinedKey(string? separator = null) =>
        string.Join(separator ?? Compositor.TheSeparator, KeyArray);

    /// <summary>
    /// 判斷當前鍵值配對是否合規。如果鍵與值有任一為空，則結果為 false。
    /// </summary>
    public bool IsValid => !string.IsNullOrEmpty(KeyArray.Joined()) && !string.IsNullOrEmpty(Value);

    /// <inheritdoc />
    public override string ToString() => $"({DescribeKeyArray(KeyArray)},{Value},{Score})";

    /// <inheritdoc />
    public bool Equals(KeyValuePaired? other) {
      if (other is null) return false;
      return KeyArray.SequenceEqual(other.KeyArray)
             && string.Equals(Value, other.Value, StringComparison.Ordinal)
             && Math.Abs(Score - other.Score) < ScoreTolerance;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as KeyValuePaired);

    /// <inheritdoc />
    public override int GetHashCode() {
      unchecked {
        int hash = 17;
        foreach (string key in KeyArray) {
          hash = hash * 23 + key.GetHashCode();
        }
        hash = hash * 23 + Value.GetHashCode();
        hash = hash * 23 + Score.GetHashCode();
        return hash;
      }
    }

    /// <summary>
    /// 會在某些特殊場合用到的字串表達方法。
    /// </summary>
    public string ToNGramKey => IsValid ? $"({JoinedKey()},{Value})" : "()";

    /// <summary>
    /// 生成用於排序的比較結果。
    /// </summary>
    public int CompareTo(KeyValuePaired? other) {
      if (other is null) return 1;
      int countComparison = KeyArray.Count.CompareTo(other.KeyArray.Count);
      if (countComparison != 0) return countComparison;
      return string.Compare(Value, other.Value, StringComparison.Ordinal);
    }

    /// <summary>
    /// 判斷兩個鍵值配對是否相等。
    /// </summary>
    public static bool operator ==(KeyValuePaired? lhs, KeyValuePaired? rhs) =>
      lhs?.Equals(rhs) ?? rhs is null;

    /// <summary>
    /// 判斷兩個鍵值配對是否不相等。
    /// </summary>
    public static bool operator !=(KeyValuePaired? lhs, KeyValuePaired? rhs) => !(lhs == rhs);

    /// <summary>
    /// 判斷一個鍵值配對是否小於另一個鍵值配對。
    /// </summary>
    public static bool operator <(KeyValuePaired lhs, KeyValuePaired rhs) => lhs.CompareTo(rhs) < 0;

    /// <summary>
    /// 判斷一個鍵值配對是否大於另一個鍵值配對。
    /// </summary>
    public static bool operator >(KeyValuePaired lhs, KeyValuePaired rhs) => lhs.CompareTo(rhs) > 0;

    /// <summary>
    /// 判斷一個鍵值配對是否小於或等於另一個鍵值配對。
    /// </summary>
    public static bool operator <=(KeyValuePaired lhs, KeyValuePaired rhs) => lhs.CompareTo(rhs) <= 0;

    /// <summary>
    /// 判斷一個鍵值配對是否大於或等於另一個鍵值配對。
    /// </summary>
    public static bool operator >=(KeyValuePaired lhs, KeyValuePaired rhs) => lhs.CompareTo(rhs) >= 0;

    private static List<string> SliceKey(string key) {
      if (string.IsNullOrEmpty(key)) {
        return new List<string>();
      }
      if (string.IsNullOrEmpty(Compositor.TheSeparator)) {
        return key.LiteralCharComponents();
      }
      return key.Split(new[] { Compositor.TheSeparator }, StringSplitOptions.None).ToList();
    }

    private static string DescribeKeyArray(IReadOnlyList<string> keyArray) {
      string content = string.Join(", ", keyArray.Select(static key => $"\"{key}\""));
      return $"[{content}]";
    }
  }
  public partial class Compositor {
    /// <summary>
    /// 候選字陣列內容的獲取範圍類型。
    /// </summary>
    public enum CandidateFetchFilter {
      /// <summary>
      /// 不只包含其它兩類結果，還允許游標穿插候選字。
      /// </summary>
      All,
      /// <summary>
      /// 僅獲取從當前游標位置開始的節點內的候選字。
      /// </summary>
      BeginAt,
      /// <summary>
      /// 僅獲取在當前游標位置結束的節點內的候選字。
      /// </summary>
      EndAt
    }

    /// <summary>
    /// 返回在當前位置的所有候選字詞（以詞音配對的形式）。<para/>如果組字器內有幅節、且游標
    /// 位於組字器的（文字輸入順序的）最前方（也就是游標位置的數值是最大合規數值）的
    /// 話，那麼這裡會對 location 的位置自動減去 1、以免去在呼叫該函式後再處理的麻煩。
    /// </summary>
    /// <param name="givenLocation">游標位置，必須是顯示的游標位置、不得做任何事先糾偏處理。</param>
    /// <param name="filter">候選字音配對陣列。</param>
    /// <returns></returns>
    public List<KeyValuePaired> FetchCandidatesAt(int? givenLocation = null,
                                                  CandidateFetchFilter filter = CandidateFetchFilter.All) {
      List<KeyValuePaired> result = new();
      if (Keys.IsEmpty()) return result;
      int location = Math.Max(0, Math.Min(givenLocation ?? Cursor, Keys.Count));
      if (filter == CandidateFetchFilter.EndAt) {
        if (location == Keys.Count) filter = CandidateFetchFilter.All;
        location -= 1;
      }
      location = Math.Max(0, Math.Min(location, Keys.Count - 1));
      // 按照讀音的長度（幅節長度）來給節點排序。
      List<NodeWithLocation> anchors = FetchOverlappingNodesAt(location);
      string keyAtCursor = Keys[location];
      anchors.ForEach(theAnchor => {
        Node theNode = theAnchor.Node;
        foreach (Unigram gram in theNode.Unigrams) {
          switch (filter) {
            case CandidateFetchFilter.All:
              // 得加上這道篩選，所以會出現很多無效結果。
              if (!theNode.KeyArray.Contains(keyAtCursor)) continue;
              break;
            case CandidateFetchFilter.BeginAt:
              if (theAnchor.Location != location) continue;
              break;
            case CandidateFetchFilter.EndAt:
              if (theNode.KeyArray.Last() != keyAtCursor) continue;
              if (theNode.SegLength >= 2 && theAnchor.Location + theAnchor.Node.SegLength - 1 != location) continue;
              break;
          }
          result.Add(new(theNode.KeyArray, gram.Value));
        }
      });
      return result;
    }

    /// <summary>
    /// 使用給定的候選字（詞音配對），將給定位置的節點的候選字詞改為與之一致的候選字詞。<para/>
    /// 該函式僅用作過程函式。
    /// </summary>
    /// <param name="candidate">指定用來覆寫為的候選字（詞音鍵值配對）。</param>
    /// <param name="location">游標位置。</param>
    /// <param name="overrideType">指定覆寫行為。</param>
    /// <param name="perceptionKeyHandler">覆寫成功後用於回傳漸退記憶感知資料的回呼。</param>
    /// <returns>該操作是否成功執行。</returns>
    public bool OverrideCandidate(KeyValuePaired candidate, int location,
                    Node.OverrideType overrideType = Node.OverrideType.HighScore,
                    Action<List<GramInPath>>? perceptionKeyHandler = null) =>
      OverrideCandidateAgainst(candidate.KeyArray, location, candidate.Value, overrideType, perceptionKeyHandler);

    /// <summary>
    /// 使用給定的候選字詞字串，將給定位置的節點的候選字詞改為與之一致的候選字詞。<para/>
    /// 注意：如果有多個「單元圖資料值雷同、卻讀音不同」的節點的話，該函式的行為結果不可控。
    /// </summary>
    /// <param name="candidate">指定用來覆寫為的候選字（字串）。</param>
    /// <param name="location">游標位置。</param>
    /// <param name="overrideType">指定覆寫行為。</param>
    /// <param name="perceptionKeyHandler">覆寫成功後用於回傳漸退記憶感知資料的回呼。</param>
    /// <returns>該操作是否成功執行。</returns>
    public bool OverrideCandidateLiteral(string candidate, int location,
                       Node.OverrideType overrideType = Node.OverrideType.HighScore,
                       Action<List<GramInPath>>? perceptionKeyHandler = null) =>
      OverrideCandidateAgainst(null, location, candidate, overrideType, perceptionKeyHandler);

    /// <summary>
    /// 使用給定的候選字（詞音配對）、或給定的候選字詞字串，將給定位置的節點的候選字詞改為與之一致的候選字詞。
    /// </summary>
    /// <param name="keyArray">索引鍵陣列，也就是詞音配對當中的讀音。</param>
    /// <param name="location">游標位置。</param>
    /// <param name="value">資料值。</param>
    /// <param name="overrideType">指定覆寫行為。</param>
    /// <param name="perceptionKeyHandler">覆寫成功後用於回傳漸退記憶感知資料的回呼。</param>
    /// <returns>該操作是否成功執行。</returns>
    internal bool OverrideCandidateAgainst(List<string>? keyArray, int location, string value,
                                           Node.OverrideType overrideType,
                                           Action<List<GramInPath>>? perceptionKeyHandler = null) {
      if (Keys.IsEmpty()) return false;
      location = Math.Max(Math.Min(location, Keys.Count), 0);  // 防呆。
      int effectiveLocation = Math.Min(Keys.Count - 1, location);
      List<NodeWithLocation> arrOverlappedNodes = FetchOverlappingNodesAt(effectiveLocation);
      if (arrOverlappedNodes.IsEmpty()) return false;

      NodeWithLocation? overriddenAnchor = null;
      Unigram? overriddenGram = null;
      bool errorHappened = false;

      foreach (NodeWithLocation anchor in arrOverlappedNodes) {
        if (keyArray is { } filter && !anchor.Node.KeyArray.SequenceEqual(filter)) continue;

        bool selectionSucceeded = anchor.Node.SelectOverrideUnigram(value, overrideType);
        overriddenGram = anchor.Node.CurrentUnigram;
        if (!selectionSucceeded) {
          errorHappened = true;
          break;
        }
        overriddenAnchor = anchor;
        break;
      }

      if (errorHappened || overriddenAnchor is null || overriddenGram is null) {
        return false;
      }

      NodeWithLocation overridden = overriddenAnchor.Value;
      try {
        int lengthUpperBound = Math.Min(Segments.Count, overridden.Location + overridden.Node.SegLength);
        foreach (int i in new ClosedRange(overridden.Location, lengthUpperBound - 1)) {
          List<NodeWithLocation> overlappingNodes = FetchOverlappingNodesAt(i);
          foreach (NodeWithLocation anchor in overlappingNodes) {
            if (ReferenceEquals(anchor.Node, overridden.Node)) continue;

            if (ShouldResetNode(anchor.Node, overridden.Node)) {
              anchor.Node.Reset();
            } else {
              anchor.Node.OverridingScore /= 4;
            }
          }
        }
      } finally {
        if (perceptionKeyHandler is { } handler) {
          List<GramInPath> assembledSentence = Assemble().ToList();
          while (assembledSentence.Count > 0 &&
                 !ReferenceEquals(assembledSentence[assembledSentence.Count - 1].Gram, overriddenGram)) {
            assembledSentence.RemoveAt(assembledSentence.Count - 1);
          }
          if (assembledSentence.Count > 0) {
            int count = Math.Min(3, assembledSentence.Count);
            handler(assembledSentence.GetRange(assembledSentence.Count - count, count));
          }
        }
      }
      return true;
    }

    /// <summary>
    /// 判斷一個節點是否需要被重設。
    /// </summary>
    private static bool ShouldResetNode(Node anchor, Node overriddenNode) {
      string anchorNodeKeyJoined = anchor.JoinedKey("\t");
      string overriddenNodeKeyJoined = overriddenNode.JoinedKey("\t");

      bool shouldReset = overriddenNodeKeyJoined.IndexOf(anchorNodeKeyJoined, StringComparison.Ordinal) < 0;
      if (overriddenNode.Value.IndexOf(anchor.Value, StringComparison.Ordinal) < 0) {
        shouldReset = true;
      }
      return shouldReset;
    }
  }
}  // namespace Megrez
