// CSharpened and further development by (c) 2022 and onwards The vChewing Project (MIT License).
// Was initially rebranded from (c) Lukhnos Liu's C++ library "Gramambular 2" (MIT License).
// ====================
// This code is released under the MIT license (SPDX-License-Identifier: MIT)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Megrez {
/// <summary>
/// 鍵值配對，乃索引鍵陣列與讀音的配對單元。
/// </summary>
public class KeyValuePaired : Unigram {
  /// <summary>
  /// 索引鍵陣列。一般情況下用來放置讀音等可以用來作為索引的內容。
  /// </summary>
  public List<string> KeyArray { get; }
  /// <summary>
  /// 初期化一組鍵值配對。
  /// </summary>
  /// <param name="keyArray">索引鍵陣列。一般情況下用來放置讀音等可以用來作為索引的內容。</param>
  /// <param name="value">資料值。</param>
  /// <param name="score">權重（雙精度小數）。</param>
  public KeyValuePaired(List<string> keyArray, string value, double score = 0) {
    KeyArray = keyArray;
    Value = value;
    Score = score;
  }
  /// <summary>
  /// 初期化一組鍵值配對。
  /// </summary>
  /// <param name="key">索引鍵。一般情況下用來放置讀音等可以用來作為索引的內容。</param>
  /// <param name="value">資料值。</param>
  /// <param name="score">權重（雙精度小數）。</param>
  public KeyValuePaired(string key, string value, double score = 0) {
    KeyArray = key.Split(Compositor.TheSeparator.ToCharArray()).ToList();
    Value = value;
    Score = score;
  }

  /// <summary>
  /// 將索引鍵按照給定的分隔符銜接成一個字串。
  /// </summary>
  /// <param name="separator">給定的分隔符，預設值為 <see cref="Compositor.TheSeparator"/>。</param>
  /// <returns>已經銜接完畢的字串。</returns>
  public string JoinedKey(string? separator = null) => string.Join(separator ?? Compositor.TheSeparator,
                                                                   KeyArray.ToArray());

  /// <summary>
  /// 判斷當前鍵值配對是否合規。如果鍵與值有任一為空，則結果為 false。
  /// </summary>
  public bool IsValid => !string.IsNullOrEmpty(JoinedKey()) && !string.IsNullOrEmpty(Value);

  /// <summary>
  ///
  /// </summary>
  /// <param name="obj"></param>
  /// <returns></returns>
  public override bool Equals(object obj) {
    return obj is KeyValuePaired pair && JoinedKey().SequenceEqual(pair.JoinedKey()) && Value == pair.Value &&
           Math.Abs(Score - pair.Score) < -0.000000000000001;
  }

  /// <summary>
  /// 做為預設雜湊函式。
  /// </summary>
  /// <returns>目前物件的雜湊碼。</returns>
  public override int GetHashCode() => HashCode.Combine(KeyArray, Value);
  /// <summary>
  /// 傳回代表目前物件的字串。
  /// </summary>
  /// <returns>表示目前物件的字串。</returns>
  public override string ToString() => $"({JoinedKey()},{Value},{Score})";

  /// <summary>
  /// 會在某些特殊場合用到的字串表達方法。
  /// </summary>
  public string ToNGramKey => IsValid ? $"({JoinedKey()},{Value})" : "()";
  /// <summary>
  ///
  /// </summary>
  /// <param name="lhs"></param>
  /// <param name="rhs"></param>
  /// <returns></returns>
  public static bool operator ==(KeyValuePaired lhs, KeyValuePaired rhs) {
    return lhs.KeyArray.SequenceEqual(rhs.KeyArray) && lhs.Value == rhs.Value;
  }
  /// <summary>
  ///
  /// </summary>
  /// <param name="lhs"></param>
  /// <param name="rhs"></param>
  /// <returns></returns>
  public static bool operator !=(KeyValuePaired lhs, KeyValuePaired rhs) {
    return lhs.KeyArray.Count != rhs.KeyArray.Count || lhs.Value != rhs.Value;
  }

  /// <summary>
  ///
  /// </summary>
  /// <param name="lhs"></param>
  /// <param name="rhs"></param>
  /// <returns></returns>
  public static bool operator<(KeyValuePaired lhs, KeyValuePaired rhs) {
    return lhs.KeyArray.Count < rhs.KeyArray.Count ||
           lhs.KeyArray.Count == rhs.KeyArray.Count &&
               string.Compare(lhs.Value, rhs.Value, StringComparison.Ordinal) < 0;
  }

  /// <summary>
  ///
  /// </summary>
  /// <param name="lhs"></param>
  /// <param name="rhs"></param>
  /// <returns></returns>
  public static bool operator>(KeyValuePaired lhs, KeyValuePaired rhs) {
    return lhs.KeyArray.Count > rhs.KeyArray.Count ||
           lhs.KeyArray.Count == rhs.KeyArray.Count &&
               string.Compare(lhs.Value, rhs.Value, StringComparison.Ordinal) > 0;
  }

  /// <summary>
  ///
  /// </summary>
  /// <param name="lhs"></param>
  /// <param name="rhs"></param>
  /// <returns></returns>
  public static bool operator <=(KeyValuePaired lhs, KeyValuePaired rhs) {
    return lhs.KeyArray.Count <= rhs.KeyArray.Count ||
           lhs.KeyArray.Count == rhs.KeyArray.Count &&
               string.Compare(lhs.Value, rhs.Value, StringComparison.Ordinal) <= 0;
  }

  /// <summary>
  ///
  /// </summary>
  /// <param name="lhs"></param>
  /// <param name="rhs"></param>
  /// <returns></returns>
  public static bool operator >=(KeyValuePaired lhs, KeyValuePaired rhs) {
    return lhs.KeyArray.Count >= rhs.KeyArray.Count ||
           lhs.KeyArray.Count == rhs.KeyArray.Count &&
               string.Compare(lhs.Value, rhs.Value, StringComparison.Ordinal) >= 0;
  }
}
public partial struct Compositor {
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
  /// 返回在當前位置的所有候選字詞（以詞音配對的形式）。<para/>如果組字器內有幅位、且游標
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
    // 按照讀音的長度（幅位長度）來給節點排序。
    List<(int Location, Node Node)> anchors = FetchOverlappingNodesAt(location);
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
            if (theNode.SpanLength >= 2 && theAnchor.Location + theAnchor.Node.SpanLength - 1 != location) continue;
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
  /// <returns>該操作是否成功執行。</returns>
  public bool OverrideCandidate(KeyValuePaired candidate, int location,
                                Node.OverrideType overrideType = Node.OverrideType.HighScore) =>
      OverrideCandidateAgainst(candidate.KeyArray, location, candidate.Value, overrideType);

  /// <summary>
  /// 使用給定的候選字詞字串，將給定位置的節點的候選字詞改為與之一致的候選字詞。<para/>
  /// 注意：如果有多個「單元圖資料值雷同、卻讀音不同」的節點的話，該函式的行為結果不可控。
  /// </summary>
  /// <param name="candidate">指定用來覆寫為的候選字（字串）。</param>
  /// <param name="location">游標位置。</param>
  /// <param name="overrideType">指定覆寫行為。</param>
  /// <returns>該操作是否成功執行。</returns>
  public bool OverrideCandidateLiteral(string candidate, int location,
                                       Node.OverrideType overrideType = Node.OverrideType.HighScore) =>
      OverrideCandidateAgainst(null, location, candidate, overrideType);

  /// <summary>
  /// 使用給定的候選字（詞音配對）、或給定的候選字詞字串，將給定位置的節點的候選字詞改為與之一致的候選字詞。
  /// </summary>
  /// <param name="keyArray">索引鍵陣列，也就是詞音配對當中的讀音。</param>
  /// <param name="location">游標位置。</param>
  /// <param name="value">資料值。</param>
  /// <param name="overrideType">指定覆寫行為。</param>
  /// <returns>該操作是否成功執行。</returns>
  internal bool OverrideCandidateAgainst(List<string>? keyArray, int location, string value,
                                         Node.OverrideType overrideType) {
    location = Math.Max(Math.Min(location, Keys.Count), 0);  // 防呆。
    List<(int Location, Node Node)> arrOverlappedNodes = FetchOverlappingNodesAt(Math.Min(Keys.Count - 1, location));
    Node fakeNode = new(new() { "_NULL_" }, spanLength: 0, new());
    (int Location, Node Node) overridden = (Location: 0, fakeNode);
    // 這裡必須用 SequenceEqual，因為 C# 只能用這種方法才能準確判定兩個字串陣列是否雷同。
    foreach (var anchor in arrOverlappedNodes.Where(
                 anchor => (keyArray == null || anchor.Node.KeyArray.SequenceEqual(keyArray)) &&
                           anchor.Node.SelectOverrideUnigram(value, overrideType))) {
      overridden = anchor;
      break;
    }
    if (Equals(overridden.Node, fakeNode)) return false;  // 啥也不覆寫。

    int lengthUpperBound = Math.Min(Spans.Count, overridden.Location + overridden.Node.SpanLength);
    foreach (int i in new BRange(overridden.Location, lengthUpperBound)) {
      // 咱們還得弱化所有在相同的幅位座標的節點的複寫權重。舉例說之前爬軌的結果是「A BC」
      // 且 A 與 BC 都是被覆寫的結果，然後使用者現在在與 A 相同的幅位座標位置
      // 選了「DEF」，那麼 BC 的覆寫狀態就有必要重設（但 A 不用重設）。
      arrOverlappedNodes = FetchOverlappingNodesAt(i);
      foreach (var anchor in arrOverlappedNodes.Where(anchor => !Equals(anchor.Node, overridden.Node))) {
        if (!overridden.Node.JoinedKey("\t").Contains(anchor.Node.JoinedKey("\t")) ||
            !overridden.Node.Value.Contains(anchor.Node.Value)) {
          anchor.Node.Reset();
          continue;
        }
        anchor.Node.OverridingScore /= 4;
      }
    }
    return true;
  }
}
}  // namespace Megrez
