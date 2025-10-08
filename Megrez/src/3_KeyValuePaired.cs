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
      ) {
    }

    /// <summary>
    /// 初期化一組鍵值配對。
    /// </summary>
    /// <param name="tupletExpression">傳入的通用陣列表達形式。</param>
    public KeyValuePaired(Tuple<List<string>, string> tupletExpression)
      : this(
        tupletExpression?.Item1?.ToList() ?? new List<string>(),
        tupletExpression?.Item2 ?? "N/A",
        0
      ) {
    }

    /// <summary>
    /// 初期化一組鍵值配對。
    /// </summary>
    /// <param name="key">索引鍵。一般情況下用來放置讀音等可以用來作為索引的內容。</param>
    /// <param name="value">資料值。</param>
    /// <param name="score">權重（雙精度小數）。</param>
    public KeyValuePaired(string key = "N/A", string value = "N/A", double score = 0)
      : this(SliceKey(key), value, score) {
    }

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
    /// <param name="enforceRetokenization">是否強制重新分詞，對所有重疊節點施作重置與降權，以避免殘留舊節點狀態。</param>
    /// <param name="perceptionHandler">覆寫成功後用於回傳觀測智慧的回呼。</param>
    /// <returns>該操作是否成功執行。</returns>
    public bool OverrideCandidate(
      KeyValuePaired candidate, int location,
      Node.OverrideType overrideType = Node.OverrideType.Specified,
      bool enforceRetokenization = false,
      Action<PerceptionIntel>? perceptionHandler = null
    ) =>
      OverrideCandidateAgainst(
        candidate.KeyArray,
        location,
        candidate.Value,
        candidate.Score < 0 ? candidate.Score : null,
        overrideType,
        enforceRetokenization,
        perceptionHandler
      );

    /// <summary>
    /// 使用給定的候選字詞字串，將給定位置的節點的候選字詞改為與之一致的候選字詞。<para/>
    /// 注意：如果有多個「單元圖資料值雷同、卻讀音不同」的節點的話，該函式的行為結果不可控。
    /// </summary>
    /// <param name="candidate">指定用來覆寫為的候選字（字串）。</param>
    /// <param name="location">游標位置。</param>
    /// <param name="overrideType">指定覆寫行為。</param>
    /// <param name="enforceRetokenization">是否強制重新分詞，對所有重疊節點施作重置與降權，以避免殘留舊節點狀態。</param>
    /// <param name="perceptionHandler">覆寫成功後用於回傳觀測智慧的回呼。</param>
    /// <returns>該操作是否成功執行。</returns>
    public bool OverrideCandidateLiteral(
      string candidate, int location,
      Node.OverrideType overrideType = Node.OverrideType.Specified,
      bool enforceRetokenization = false,
      Action<PerceptionIntel>? perceptionHandler = null
    ) =>
      OverrideCandidateAgainst(
        null, location, candidate, null, overrideType, enforceRetokenization, perceptionHandler
      );

    /// <summary>
    /// 使用給定的候選字（詞音配對）、或給定的候選字詞字串，將給定位置的節點的候選字詞改為與之一致的候選字詞。
    /// </summary>
    /// <param name="keyArray">索引鍵陣列，也就是詞音配對當中的讀音。</param>
    /// <param name="location">游標位置。</param>
    /// <param name="value">資料值。</param>
    /// <param name="specifiedScore">指定分數。</param>
    /// <param name="overrideType">指定覆寫行為。</param>
    /// <param name="enforceRetokenization">是否強制重新分詞，對所有重疊節點施作重置與降權，以避免殘留舊節點狀態。</param>
    /// <param name="perceptionHandler">覆寫成功後用於回傳觀測智慧的回呼。</param>
    /// <returns>該操作是否成功執行。</returns>
    internal bool OverrideCandidateAgainst(
      List<string>? keyArray,
      int location,
      string value,
      double? specifiedScore,
      Node.OverrideType overrideType,
      bool enforceRetokenization = false,
      Action<PerceptionIntel>? perceptionHandler = null
    ) {
      if (Keys.IsEmpty()) return false;
      location = Math.Max(Math.Min(location, Keys.Count), 0); // 防呆。
      int effectiveLocation = Math.Min(Keys.Count - 1, location);
      List<NodeWithLocation> arrOverlappedNodes = FetchOverlappingNodesAt(effectiveLocation);
      if (arrOverlappedNodes.IsEmpty()) return false;

      // 用於觀測：覆寫生效前的 walk 與游標
      bool hasPerceptor = perceptionHandler != null;
      List<GramInPath> previouslyAssembled = hasPerceptor ? Assemble().ToList() : new List<GramInPath>();
      int beforeCursor = Math.Min(Keys.Count, location);

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

        if (specifiedScore is double score && anchor.Node.Score < score
                                           && anchor.Node.CurrentOverrideType != Node.OverrideType.TopUnigramScore) {
          anchor.Node.OverrideStatus = new NodeOverrideStatus(
            score,
            Node.OverrideType.Specified,
            anchor.Node.CurrentUnigramIndex
          );
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
        if (enforceRetokenization) {
          Node overriddenNodeRef = overridden.Node;
          double demotionScore = -Math.Max(1.0, Math.Abs(overriddenNodeRef.OverridingScore));
          foreach (int i in new ClosedRange(overridden.Location, lengthUpperBound - 1)) {
            List<NodeWithLocation> overlappingNodes = FetchOverlappingNodesAt(i);
            foreach (NodeWithLocation anchor in overlappingNodes) {
              if (ReferenceEquals(anchor.Node, overriddenNodeRef)) continue;
              if (anchor.Location > overridden.Location) continue;
              if (ShouldResetNode(anchor.Node, overriddenNodeRef)) {
                anchor.Node.Reset();
              }

              anchor.Node.OverrideStatus = new NodeOverrideStatus(
                demotionScore,
                Node.OverrideType.Specified,
                anchor.Node.CurrentUnigramIndex
              );
            }
          }
        } else {
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
        }
      } finally {
        // 覆寫後組句與觀測：
        List<GramInPath> currentAssembled = Assemble().ToList();
        if (perceptionHandler != null && !previouslyAssembled.IsEmpty()) {
          // 供新版觀測 API（前/後路徑比較 + 三情境分類）
          PerceptionIntel? perceptedIntel = MakePerceptionIntel(
            previouslyAssembled,
            currentAssembled,
            beforeCursor
          );
          if (perceptedIntel.HasValue) {
            perceptionHandler(perceptedIntel.Value);
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

    /// <summary>
    /// 根據候選字覆寫行為前後的組句結果，在指定游標位置得出觀測上下文之情形。
    /// </summary>
    /// <param name="previouslyAssembled">候選字覆寫行為前的組句結果。</param>
    /// <param name="currentAssembled">候選字覆寫行為後的組句結果。</param>
    /// <param name="cursor">游標。</param>
    /// <returns>觀測上下文結果。</returns>
    public static PerceptionIntel? MakePerceptionIntel(
      List<GramInPath> previouslyAssembled,
      List<GramInPath> currentAssembled,
      int cursor) {
      if (previouslyAssembled.IsEmpty() || currentAssembled.IsEmpty()) return null;

      // 確認游標落在 currentAssembled 的有效節點
      var afterHit = currentAssembled.FindGram(cursor);
      if (afterHit == null) return null;
      var current = afterHit.Value.gram;
      int currentLen = current.SegLength;
      if (currentLen > 3) return null;

      // 在 previouslyAssembled 中找到對應 head 的節點（使用 after 的節點區間上界 -1 作為內點）
      int border1 = afterHit.Value.range.Upperbound - 1;
      int border2 = previouslyAssembled.TotalKeyCount() - 1;
      int innerIndex = Math.Max(0, Math.Min(border1, border2));
      var beforeHit = previouslyAssembled.FindGram(innerIndex);
      if (beforeHit == null) return null;
      var prevHead = beforeHit.Value.gram;
      int prevLen = prevHead.SegLength;

      bool isBreakingUp = (currentLen == 1 && prevLen > 1);
      bool isShortToLong = (currentLen > prevLen);
      POMObservationScenario scenario;
      if (isBreakingUp) {
        scenario = POMObservationScenario.LongToShort;
      } else if (isShortToLong) {
        scenario = POMObservationScenario.ShortToLong;
      } else {
        scenario = POMObservationScenario.SameLenSwap;
      }

      bool forceHSO = isShortToLong; // 只有短→長時需要強推長詞

      List<GramInPath> primaryKeySource;
      List<GramInPath> fallbackKeySource;
      switch (scenario) {
        case POMObservationScenario.SameLenSwap:
          primaryKeySource = currentAssembled;
          fallbackKeySource = previouslyAssembled;
          break;
        case POMObservationScenario.ShortToLong:
          primaryKeySource = previouslyAssembled;
          fallbackKeySource = currentAssembled;
          break;
        case POMObservationScenario.LongToShort:
          primaryKeySource = currentAssembled;
          fallbackKeySource = previouslyAssembled;
          break;
        default:
          primaryKeySource = previouslyAssembled;
          fallbackKeySource = currentAssembled;
          break;
      }

      int keyCursorRaw = Math.Max(
        afterHit.Value.range.Lowerbound,
        Math.Min(cursor, afterHit.Value.range.Upperbound - 1)
      );
      if (primaryKeySource.TotalKeyCount() <= 0 && fallbackKeySource.TotalKeyCount() <= 0) return null;
      int primaryTotal = primaryKeySource.TotalKeyCount();
      int keyCursorPrimary = Math.Max(0, Math.Min(keyCursorRaw, Math.Max(primaryTotal - 1, 0)));
      var keyGen = primaryKeySource.GenerateKeyForPerception(keyCursorPrimary);
      if (keyGen == null && fallbackKeySource.TotalKeyCount() > 0) {
        int fallbackTotal = fallbackKeySource.TotalKeyCount();
        int keyCursorFallback = Math.Max(0, Math.Min(keyCursorRaw, Math.Max(fallbackTotal - 1, 0)));
        keyGen = fallbackKeySource.GenerateKeyForPerception(keyCursorFallback);
      }

      if (keyGen == null) return null;

      return new PerceptionIntel {
        NGramKey = keyGen.Value.NGramKey,
        Candidate = current.Value,
        HeadReading = keyGen.Value.HeadReading,
        Scenario = scenario,
        ForceHighScoreOverride = forceHSO,
        ScoreFromLM = afterHit.Value.gram.Score
      };
    }
  }

  /// <summary>
  /// 觀測上下文類型。
  /// </summary>
  public enum POMObservationScenario {
    /// <summary>
    /// 同長度更換。
    /// </summary>
    SameLenSwap,

    /// <summary>
    /// 短詞變長詞。
    /// </summary>
    ShortToLong,

    /// <summary>
    /// 長詞變短詞。
    /// </summary>
    LongToShort
  }

  /// <summary>
  /// 觀測上下文情形。
  /// </summary>
  public struct PerceptionIntel : IEquatable<PerceptionIntel> {
    /// <summary>
    /// N-gram 索引鍵。
    /// </summary>
    public string NGramKey { get; set; }

    /// <summary>
    /// 候選字。
    /// </summary>
    public string Candidate { get; set; }

    /// <summary>
    /// 頭部讀音。
    /// </summary>
    public string HeadReading { get; set; }

    /// <summary>
    /// 觀測場景。
    /// </summary>
    public POMObservationScenario Scenario { get; set; }

    /// <summary>
    /// 強制高分覆寫。
    /// </summary>
    public bool ForceHighScoreOverride { get; set; }

    /// <summary>
    /// 語言模型分數。
    /// </summary>
    public double ScoreFromLM { get; set; }

    /// <summary>
    /// 判斷當前 PerceptionIntel 是否等於另一個 PerceptionIntel。
    /// </summary>
    /// <param name="other">要比較的另一個 PerceptionIntel 物件。</param>
    /// <returns>如果相等則返回 true，否則返回 false。</returns>
    public bool Equals(PerceptionIntel other) {
      return NGramKey == other.NGramKey &&
             Candidate == other.Candidate &&
             HeadReading == other.HeadReading &&
             Scenario == other.Scenario &&
             ForceHighScoreOverride == other.ForceHighScoreOverride &&
             Math.Abs(ScoreFromLM - other.ScoreFromLM) < 1e-12;
    }

    /// <summary>
    /// 判斷當前 PerceptionIntel 是否等於指定的物件。
    /// </summary>
    /// <param name="obj">要比較的物件。</param>
    /// <returns>如果相等則返回 true，否則返回 false。</returns>
    public override bool Equals(object obj) => obj is PerceptionIntel other && Equals(other);

    /// <summary>
    /// 獲取當前 PerceptionIntel 的雜湊碼。
    /// </summary>
    /// <returns>雜湊碼值。</returns>
    public override int GetHashCode() {
      unchecked {
        int hash = 17;
        hash = hash * 23 + (NGramKey?.GetHashCode() ?? 0);
        hash = hash * 23 + (Candidate?.GetHashCode() ?? 0);
        hash = hash * 23 + (HeadReading?.GetHashCode() ?? 0);
        hash = hash * 23 + Scenario.GetHashCode();
        hash = hash * 23 + ForceHighScoreOverride.GetHashCode();
        hash = hash * 23 + ScoreFromLM.GetHashCode();
        return hash;
      }
    }

    /// <summary>
    /// 判斷兩個 PerceptionIntel 是否相等。
    /// </summary>
    /// <param name="left">左側 PerceptionIntel 物件。</param>
    /// <param name="right">右側 PerceptionIntel 物件。</param>
    /// <returns>如果相等則返回 true，否則返回 false。</returns>
    public static bool operator ==(PerceptionIntel left, PerceptionIntel right) => left.Equals(right);

    /// <summary>
    /// 判斷兩個 PerceptionIntel 是否不相等。
    /// </summary>
    /// <param name="left">左側 PerceptionIntel 物件。</param>
    /// <param name="right">右側 PerceptionIntel 物件。</param>
    /// <returns>如果不相等則返回 true，否則返回 false。</returns>
    public static bool operator !=(PerceptionIntel left, PerceptionIntel right) => !(left == right);
  }
} // namespace Megrez
