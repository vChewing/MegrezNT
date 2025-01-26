// CSharpened and further development by (c) 2022 and onwards The vChewing Project (MIT License).
// Was initially rebranded from (c) Lukhnos Liu's C++ library "Gramambular 2" (MIT License).
// ====================
// This code is released under the MIT license (SPDX-License-Identifier: MIT)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Megrez {
/// <summary>
/// 字詞節點。<para/>
/// 一個節點由這些內容組成：幅位長度、索引鍵、以及一組單元圖。幅位長度就是指這個
/// 節點在組字器內橫跨了多少個字長。組字器負責構築自身的節點。對於由多個漢字組成
/// 的詞，組字器會將多個讀音索引鍵合併為一個讀音索引鍵、據此向語言模組請求對應的
/// 單元圖結果陣列。舉例說，如果一個詞有兩個漢字組成的話，那麼讀音也是有兩個、其
/// 索引鍵也是由兩個讀音組成的，那麼這個節點的幅位長度就是 2。
/// </summary>
public partial class Node {
  // MARK: - Enums

  /// <summary>
  /// 三種不同的針對一個節點的覆寫行為。
  /// </summary>
  public enum OverrideType {
    /// <summary>
    /// 無覆寫行為。
    /// </summary>
    NoOverrides = 0,
    /// <summary>
    /// 使用指定的單元圖資料值來覆寫該節點，但卻使用
    /// 當前狀態下權重最高的單元圖的權重數值。打比方說，如果該節點內的單元圖陣列是
    ///  [("a", -114), ("b", -514), ("c", -1919)] 的話，指定該覆寫行為則會導致該節
    ///  點返回的結果為 ("c", -114)。該覆寫行為多用於諸如使用者半衰記憶模組的建議
    ///  行為。被覆寫的這個節點的狀態可能不會再被爬軌行為擅自改回。該覆寫行為無法
    ///  防止其它節點被爬軌函式所支配。這種情況下就需要用到 <see cref="OverridingScore"/>。
    /// </summary>
    TopUnigramScore = 1,
    /// <summary>
    /// 將該節點權重覆寫為 <see cref="OverridingScore"/>，使其被爬軌函式所青睞。
    /// </summary>
    HighScore = 2
  }

  // MARK: - Variables

  /// <summary>
  /// 一個用以覆寫權重的數值。該數值之高足以改變爬軌函式對該節點的讀取結果。這裡用
  /// 「0」可能看似足夠了，但仍會使得該節點的覆寫狀態有被爬軌函式忽視的可能。比方說
  /// 要針對索引鍵「a b c」複寫的資料值為「A B C」，使用大寫資料值來覆寫節點。這時，
  /// 如果這個獨立的 c 有一個可以拮抗權重的詞「bc」的話，可能就會導致爬軌函式的算法
  /// 找出「A->bc」的爬軌途徑（尤其是當 A 和 B 使用「0」作為複寫數值的情況下）。這樣
  /// 一來，「A-B」就不一定始終會是爬軌函式的青睞結果了。所以，這裡一定要用大於 0 的
  /// 數（比如野獸常數），以讓「c」更容易單獨被選中。
  /// </summary>
  public double OverridingScore = 114514;

  /// <summary>
  /// 索引鍵陣列。
  /// </summary>
  public List<string> KeyArray { get; }
  /// <summary>
  /// 幅位長度。
  /// </summary>
  public int SpanLength { get; }
  /// <summary>
  /// 單元圖陣列。
  /// </summary>
  public List<Unigram> Unigrams { get; private set; }
  /// <summary>
  /// 該節點目前的覆寫狀態種類。
  /// </summary>
  public OverrideType CurrentOverrideType { get; private set; }

  private int _currentUnigramIndex;
  /// <summary>
  /// 當前該節點所指向的（單元圖陣列內的）單元圖索引位置。
  /// </summary>
  public int CurrentUnigramIndex {
    get => _currentUnigramIndex;
    set {
      int corrected = Math.Max(Math.Min(Unigrams.Count - 1, value), 0);
      _currentUnigramIndex = corrected;
    }
  }

  // MARK: - Constructor and Other Fundamentals

  /// <summary>
  /// 生成一個字詞節點。<para/>
  /// 一個節點由這些內容組成：幅位長度、索引鍵、以及一組單元圖。幅位長度就是指這個
  /// 節點在組字器內橫跨了多少個字長。組字器負責構築自身的節點。對於由多個漢字組成
  /// 的詞，組字器會將多個讀音索引鍵合併為一個讀音索引鍵、據此向語言模組請求對應的
  /// 單元圖結果陣列。舉例說，如果一個詞有兩個漢字組成的話，那麼讀音也是有兩個、其
  /// 索引鍵也是由兩個讀音組成的，那麼這個節點的幅位長度就是 2。
  /// </summary>
  /// <param name="keyArray">給定索引鍵陣列，不得為空。</param>
  /// <param name="spanLength">給定幅位長度，一般情況下與給定索引鍵陣列內的索引鍵數量一致。</param>
  /// <param name="unigrams">給定單元圖陣列，不得為空。</param>
  public Node(List<string> keyArray, int spanLength, List<Unigram> unigrams) {
    _currentUnigramIndex = 0;
    KeyArray = keyArray;
    SpanLength = Math.Max(0, spanLength);
    Unigrams = unigrams;
    CurrentOverrideType = OverrideType.NoOverrides;
  }

  /// <summary>
  /// 以指定字詞節點生成拷貝。
  /// </summary>
  /// <remarks>
  /// 因為 Node 不是 Struct，所以會在 Compositor 被拷貝的時候無法被真實複製。
  /// 這樣一來，Compositor 複製品當中的 Node 的變化會被反應到原先的 Compositor 身上。
  /// 這在某些情況下會造成意料之外的混亂情況，所以需要引入一個拷貝用的建構子。
  /// </remarks>
  /// <param name="node"></param>
  public Node(Node node) {
    OverridingScore = node.OverridingScore;
    KeyArray = node.KeyArray;
    SpanLength = node.SpanLength;
    Unigrams = node.Unigrams;
    CurrentOverrideType = node.CurrentOverrideType;
    CurrentUnigramIndex = node.CurrentUnigramIndex;
  }

  /// <summary>
  /// 生成自身的拷貝。
  /// </summary>
  /// <remarks>
  /// 因為 Node 不是 Struct，所以會在 Compositor 被拷貝的時候無法被真實複製。
  /// 這樣一來，Compositor 複製品當中的 Node 的變化會被反應到原先的 Compositor 身上。
  /// 這在某些情況下會造成意料之外的混亂情況，所以需要引入一個拷貝用的建構子。
  /// </remarks>
  /// <returns>拷貝。</returns>
  public Node Copy() => new(node: this);

  /// <summary>
  ///
  /// </summary>
  /// <param name="obj"></param>
  /// <returns></returns>
  public override bool Equals(object obj) {
    if (obj is not Node node) return false;
    return OverridingScore == node.OverridingScore && KeyArray.SequenceEqual(node.KeyArray) &&
           SpanLength == node.SpanLength && Unigrams == node.Unigrams &&
           CurrentOverrideType == node.CurrentOverrideType && CurrentUnigramIndex == node.CurrentUnigramIndex;
  }

  /// <summary>
  /// 做為預設雜湊函式。
  /// </summary>
  /// <returns>目前物件的雜湊碼。</returns>
  public override int GetHashCode() {
    int[] x = { OverridingScore.GetHashCode(),     KeyArray.GetHashCode(),
                SpanLength.GetHashCode(),          Unigrams.GetHashCode(),
                CurrentUnigramIndex.GetHashCode(), CurrentOverrideType.GetHashCode() };
    return x.GetHashCode();
  }

  // MARK: - Dynamic Variables

  /// <summary>
  /// 該節點當前狀態所展示的鍵值配對。
  /// </summary>
  public KeyValuePaired CurrentPair => new(KeyArray, Value);

  /// <summary>
  /// 給出該節點內部單元圖陣列內目前被索引位置所指向的單元圖。
  /// </summary>
  public Unigram CurrentUnigram => Unigrams.IsEmpty() ? new() : Unigrams[CurrentUnigramIndex];

  /// <summary>
  /// 給出該節點內部單元圖陣列內目前被索引位置所指向的單元圖的資料值。
  /// </summary>
  public string Value => CurrentUnigram.Value;

  /// <summary>
  /// 給出目前的最高權重單元圖當中的權重值。該結果可能會受節點覆寫狀態所影響。
  /// </summary>
  public double Score {
    get {
      if (Unigrams.IsEmpty()) return 0;
      return CurrentOverrideType switch { OverrideType.HighScore => OverridingScore,
                                          OverrideType.TopUnigramScore => Unigrams.First().Score,
                                          _ => CurrentUnigram.Score };
    }
  }

  /// <summary>
  /// 檢查當前節點是否「讀音字長與候選字字長不一致」。
  /// </summary>
  public bool IsReadingMismatched => KeyArray.Count != Value.LiteralCount();
  /// <summary>
  /// 該節點是否處於被覆寫的狀態。
  /// </summary>
  public bool IsOverridden => CurrentOverrideType != OverrideType.NoOverrides;

  // MARK: - Methods and Functions

  /// <summary>
  /// 將索引鍵按照給定的分隔符銜接成一個字串。
  /// </summary>
  /// <param name="separator">給定的分隔符，預設值為 <see cref="Compositor.TheSeparator"/>。</param>
  /// <returns>已經銜接完畢的字串。</returns>
  public string JoinedKey(string? separator = null) => KeyArray.Joined(separator: separator ?? Compositor.TheSeparator);

  /// <summary>
  /// 重設該節點的覆寫狀態、及其內部的單元圖索引位置指向。
  /// </summary>
  public void Reset() {
    _currentUnigramIndex = 0;
    CurrentOverrideType = OverrideType.NoOverrides;
  }

  /// <summary>
  /// 置換掉該節點內的單元圖陣列資料。
  /// 如果此時影響到了 currentUnigramIndex 所指的內容的話，則將其重設為 0。
  /// </summary>
  /// <param name="source">新的單元圖陣列資料，必須不能為空（否則必定崩潰）。</param>
  public void SyncingUnigramsFrom(List<Unigram> source) {
    string oldCurrentValue = Unigrams[CurrentUnigramIndex].Value;
    Unigrams = source;
    CurrentUnigramIndex = _currentUnigramIndex;  // 自動觸發 didSet() 的糾錯過程。
    string newCurrentValue = Unigrams[CurrentUnigramIndex].Value;
    if (oldCurrentValue != newCurrentValue) Reset();
  }

  /// <summary>
  /// 指定要覆寫的單元圖資料值、以及覆寫行為種類。
  /// </summary>
  /// <param name="value">給定的單元圖資料值。</param>
  /// <param name="type">覆寫行為種類。</param>
  /// <returns>操作是否順利完成。</returns>
  public bool SelectOverrideUnigram(string value, OverrideType type) {
    if (type == OverrideType.NoOverrides) return false;
    foreach (EnumeratedItem<Unigram> pair in Unigrams.Enumerated()) {
      int i = pair.Offset;
      Unigram gram = pair.Value;
      if (value != gram.Value) continue;
      CurrentUnigramIndex = i;
      CurrentOverrideType = type;
      return true;
    }
    return false;
  }
}

// MARK: - [Node] Implementations.

/// <summary>
/// 針對節點陣列的功能擴充。
/// </summary>
public static class NodeExtensions {
  /// <summary>
  /// 從一個節點陣列當中取出目前的索引鍵陣列。
  /// </summary>
  /// <param name="self">節點。</param>
  /// <returns>目前的索引鍵陣列。</returns>
  public static List<List<string>> KeyArray(this List<Node> self) => self.Select(x => x.KeyArray).ToList();
  /// <summary>
  /// 從一個節點陣列當中取出目前的選字字串陣列。
  /// </summary>
  /// <param name="self">節點。</param>
  /// <returns>目前的自動選字字串陣列。</returns>
  public static List<string> Values(this List<Node> self) => self.Select(x => x.Value).ToList();
  /// <summary>
  /// 從一個節點陣列當中取出目前的索引鍵陣列。
  /// </summary>
  /// <param name="self">節點。</param>
  /// <param name="separator"></param>
  /// <returns>取出目前的索引鍵陣列。</returns>
  public static List<string> JoinedKeys(this List<Node> self, string? separator) =>
      self.Select(x => x.KeyArray.Joined(separator ?? Compositor.TheSeparator)).ToList();
  /// <summary>
  /// 總讀音單元數量。在絕大多數情況下，可視為總幅位長度。
  /// </summary>
  /// <param name="self">節點。</param>
  /// <returns>總讀音單元數量。</returns>
  public static int TotalKeyCount(this List<Node> self) => self.Select(x => x.KeyArray.Count).Sum();

  /// <summary>
  /// (Result A, Result B) 辭典陣列。Result A 以索引查座標，Result B 以座標查索引。
  /// </summary>
  public struct CursorMapPair {
    /// <summary>
    /// 以索引查座標的辭典陣列。
    /// </summary>
    public Dictionary<int, int> RegionCursorMap { get; private set; }
    /// <summary>
    /// 以座標查索引的辭典陣列。
    /// </summary>
    public Dictionary<int, int> CursorRegionMap { get; private set; }
    /// <summary>
    /// (Result A, Result B) 辭典陣列。Result A 以索引查座標，Result B 以座標查索引。
    /// </summary>
    /// <param name="regionCursorMap">以索引查座標的辭典陣列。</param>
    /// <param name="cursorRegionMap">以座標查索引的辭典陣列。</param>
    public CursorMapPair(Dictionary<int, int> regionCursorMap, Dictionary<int, int> cursorRegionMap) {
      RegionCursorMap = regionCursorMap;
      CursorRegionMap = cursorRegionMap;
    }
  }

  /// <summary>
  /// 返回一連串的節點起點。結果為 (Result A, Result B) 辭典陣列。
  /// Result A 以索引查座標，Result B 以座標查索引。
  /// </summary>
  /// <param name="self">節點。</param>
  /// <returns> (Result A, Result B) 辭典陣列。Result A 以索引查座標，Result B 以座標查索引。</returns>
  private static CursorMapPair NodeBorderPointDictPair(this List<Node> self) {
    Dictionary<int, int> resultA = new();
    Dictionary<int, int> resultB = new() { [-1] = 0 };  // 防呆
    int cursorCounter = 0;
    foreach (EnumeratedItem<Node> pair in self.Enumerated()) {
      int nodeCounter = pair.Offset;
      Node neta = pair.Value;
      resultA[nodeCounter] = cursorCounter;
      foreach (string _ in neta.KeyArray) {
        resultB[cursorCounter] = nodeCounter;
        cursorCounter += 1;
      }
    }
    resultA[self.Count] = cursorCounter;
    resultB[cursorCounter] = self.Count;
    return new(resultA, resultB);
  }

  /// <summary>
  /// 返回一個辭典，以座標查索引。允許以游標位置查詢其屬於第幾個幅位座標（從 0 開始算）。
  /// </summary>
  /// <param name="self">節點。</param>
  /// <returns>一個辭典，以座標查索引。允許以游標位置查詢其屬於第幾個幅位座標（從 0 開始算）。</returns>
  public static Dictionary<int, int> CursorRegionMap(this List<Node> self) =>
      self.NodeBorderPointDictPair().CursorRegionMap;

  /// <summary>
  /// 根據給定的游標，返回其前後最近的節點邊界。
  /// </summary>
  /// <param name="self">節點。</param>
  /// <param name="givenCursor">給定的游標。</param>
  /// <returns>前後最近的邊界點。</returns>
  public static BRange ContextRange(this List<Node> self, int givenCursor) {
    if (self.IsEmpty()) return new(0, 0);
    int lastSpanLength = self.Last().KeyArray.Count;
    int totalKeyCount = self.TotalKeyCount();
    BRange nilReturn = new(totalKeyCount - lastSpanLength, totalKeyCount);
    if (givenCursor >= totalKeyCount) return nilReturn;
    int cursor = Math.Max(0, givenCursor);  // 防呆。
    nilReturn = new(cursor, cursor);
    CursorMapPair dictPair = self.NodeBorderPointDictPair();
    // 下文按道理來講不應該會出現 nilReturn。
    if (!dictPair.CursorRegionMap.TryGetValue(cursor, out int rearNodeID)) return nilReturn;
    if (!dictPair.RegionCursorMap.TryGetValue(rearNodeID, out int rearIndex)) return nilReturn;
    if (!dictPair.RegionCursorMap.TryGetValue(rearNodeID + 1, out int frontIndex)) return nilReturn;
    return new(rearIndex, frontIndex);
  }
  /// <summary>
  /// 在陣列內以給定游標位置找出對應的節點。
  /// </summary>
  /// <param name="self">節點。</param>
  /// <param name="givenCursor">給定游標位置。</param>
  /// <param name="outCursorPastNode">找出的節點的前端位置。</param>
  /// <returns>查找結果。</returns>
  public static Node? FindNodeAt(this List<Node> self, int givenCursor, ref int outCursorPastNode) {
    if (self.IsEmpty()) return null;
    int cursor = Math.Max(0, Math.Min(givenCursor, self.TotalKeyCount() - 1));
    BRange range = self.ContextRange(givenCursor: cursor);
    outCursorPastNode = range.Upperbound;
    CursorMapPair dictPair = self.NodeBorderPointDictPair();
    if (!dictPair.CursorRegionMap.TryGetValue(cursor + 1, out int rearNodeID)) return null;
    return self.Count - 1 >= rearNodeID ? self[rearNodeID] : null;
  }
  /// <summary>
  /// 在陣列內以給定游標位置找出對應的節點。
  /// </summary>
  /// <param name="self">節點。</param>
  /// <param name="givenCursor">給定游標位置。</param>
  /// <returns>查找結果。</returns>
  public static Node? FindNodeAt(this List<Node> self, int givenCursor) {
    int mudamuda = 0;  // muda = useless.
    return self.FindNodeAt(givenCursor, ref mudamuda);
  }

  /// <summary>
  /// 提供一組逐字的字音配對陣列（不使用 Megrez 的 KeyValuePaired 類型），但字音不匹配的節點除外。
  /// </summary>
  /// <param name="self">節點。</param>
  /// <returns>一組逐字的字音配對陣列。</returns>
  public static List<KeyValuePair<string, string>> SmashedPairs(this List<Node> self) {
    List<KeyValuePair<string, string>> arrData = new();

    foreach (Node node in self) {
      if (node.IsReadingMismatched && !string.IsNullOrEmpty(node.KeyArray.Joined())) {
        arrData.Add(new(node.KeyArray.Joined("\t"), node.Value));
        continue;
      }
      List<string> arrValueChars = node.Value.LiteralCharComponents();
      foreach (EnumeratedItem<string> pair in node.KeyArray.Enumerated()) {
        arrData.Add(new(pair.Value, arrValueChars[pair.Offset]));
      }
    }
    return arrData;
  }
}
}  // namespace Megrez
