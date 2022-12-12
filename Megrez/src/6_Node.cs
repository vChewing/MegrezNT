// CSharpened and further development by (c) 2022 and onwards The vChewing Project (MIT License).
// Was initially rebranded from (c) Lukhnos Liu's C++ library "Gramambular 2" (MIT License).
// ====================
// This code is released under the MIT license (SPDX-License-Identifier: MIT)
#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Linq;

namespace Megrez {
public partial struct Compositor {
  public class Node {
    // MARK: - Enums

    public enum OverrideType { NoOverrides = 0, TopUnigramScore = 1, HighScore = 2 }

    // MARK: - Variables

    public double OverridingScore = 114514;

    public List<string> KeyArray { get; private set; }
    public int SpanLength { get; private set; }
    public List<Unigram> Unigrams { get; private set; }
    public OverrideType CurrentOverrideType { get; private set; }
    private int _currentUnigramIndex;
    public int CurrentUnigramIndex {
      get => _currentUnigramIndex;
      set {
        int corrected = Math.Max(Math.Min(Unigrams.Count - 1, value), 0);
        _currentUnigramIndex = corrected;
      }
    }

    // MARK: - Constructor and Other Fundamentals

    public Node(List<string> keyArray, int spanLength, List<Unigram> unigrams) {
      _currentUnigramIndex = 0;
      KeyArray = keyArray;
      SpanLength = spanLength;
      Unigrams = unigrams;
      CurrentOverrideType = OverrideType.NoOverrides;
    }
    public override bool Equals(object obj) => obj is Node node && KeyArray == node.KeyArray
                                               && SpanLength == node.SpanLength && Unigrams == node.Unigrams
                                               && CurrentOverrideType == node.CurrentOverrideType;

    public override int GetHashCode() => HashCode.Combine(KeyArray, SpanLength, Unigrams, CurrentUnigramIndex,
                                                          SpanLength, CurrentOverrideType);

    // MARK: - Dynamic Variables

    public KeyValuePaired CurrentPair => new(KeyArray, Value);

    public Unigram CurrentUnigram => Unigrams.IsEmpty() ? new() : Unigrams[CurrentUnigramIndex];

    public string Value => CurrentUnigram.Value;

    public double Score {
      get {
        if (Unigrams.IsEmpty()) return 0;
        return CurrentOverrideType switch { OverrideType.HighScore => OverridingScore,
                                            OverrideType.TopUnigramScore => Unigrams.First().Score,
                                            _ => CurrentUnigram.Score };
      }
    }

    public bool IsReadingMismatched => KeyArray.Count != Value.LiteralCount();
    public bool IsOverridden => CurrentOverrideType != OverrideType.NoOverrides;

    // MARK: - Methods and Functions

    public string JoinedKey(string? separator = null) => KeyArray.Joined(separator: separator ?? TheSeparator);

    public void Reset() {
      _currentUnigramIndex = 0;
      CurrentOverrideType = OverrideType.NoOverrides;
    }

    public void SyncingUnigramsFrom(List<Unigram> source) {
      string oldCurrentValue = Unigrams[CurrentUnigramIndex].Value;
      Unigrams = source;
      CurrentUnigramIndex = _currentUnigramIndex;  // 自動觸發 didSet() 的糾錯過程。
      string newCurrentValue = Unigrams[CurrentUnigramIndex].Value;
      if (oldCurrentValue != newCurrentValue) _currentUnigramIndex = 0;
    }

    public bool SelectOverrideUnigram(string value, OverrideType type) {
      if (type == OverrideType.NoOverrides) return false;
      foreach ((int i, Unigram gram) in Unigrams.Enumerated()) {
        if (value != gram.Value) continue;
        CurrentUnigramIndex = i;
        CurrentOverrideType = type;
        return true;
      }
      return false;
    }
  }

  public struct NodeAnchor {
    public Node Node { get; set; }
    public int SpanIndex { get; set; }
    public NodeAnchor(Node node, int spanIndex) {
      Node = node;
      SpanIndex = spanIndex;
    }
    public int SpanLength => Node.SpanLength;
    public List<Unigram> Unigrams => Node.Unigrams;
    public List<string> KeyArray => Node.KeyArray;
    public string Value => Node.Value;
    public override int GetHashCode() => HashCode.Combine(Node, SpanIndex);
  }
}

// MARK: - [Node] Implementations.

public static class NodeExtensions {
  public static List<List<string>> KeyArray(this List<Compositor.Node> self) => self.Select(x => x.KeyArray).ToList();
  public static List<string> Values(this List<Compositor.Node> self) => self.Select(x => x.Value).ToList();
  public static List<string> JoinedKeys(this List<Compositor.Node> self, string? separator) =>
      self.Select(x => x.KeyArray.Joined(separator ?? Compositor.TheSeparator)).ToList();
  public static int TotalKeyCount(this List<Compositor.Node> self) => self.Select(x => x.KeyArray.Count).Sum();
  public static (Dictionary<int, int>, Dictionary<int, int>) NodeBorderPointDictPair(this List<Compositor.Node> self) {
    Dictionary<int, int> resultA = new();
    Dictionary<int, int> resultB = new();
    int i = 0;
    foreach ((int j, Compositor.Node neta) in self.Enumerated()) {
      resultA[j] = i;
      foreach (string _ in neta.KeyArray) {
        resultB[i] = j;
        i += 1;
      }
    }
    resultA[resultA.Count] = i;
    resultB[i] = resultB.Count;
    return (resultA, resultB);
  }
  public static BRange ContextRange(this List<Compositor.Node> self, int givenCursor) {
    if (self.IsEmpty()) return new(0, 0);
    int lastSpanLength = self.Last().KeyArray.Count;
    int totalKeyCount = self.TotalKeyCount();
    BRange nilReturn = new(totalKeyCount - lastSpanLength, totalKeyCount);
    if (givenCursor >= totalKeyCount) return nilReturn;
    int cursor = Math.Max(0, givenCursor);  // 防呆。
    nilReturn = new(cursor, cursor);
    (Dictionary<int, int>, Dictionary<int, int>)dictPair = self.NodeBorderPointDictPair();
    if (!dictPair.Item2.TryGetValue(cursor, out int rearNodeID)) return nilReturn;
    if (!dictPair.Item1.TryGetValue(rearNodeID, out int rearIndex)) return nilReturn;
    if (!dictPair.Item1.TryGetValue(rearNodeID + 1, out int frontIndex)) return nilReturn;
    return new(rearIndex, frontIndex);
  }
  public static Compositor.Node? FindNodeAt(this List<Compositor.Node> self, int givenCursor,
                                            ref int outCursorPastNode) {
    if (self.IsEmpty()) return null;
    int cursor = Math.Max(0, Math.Min(givenCursor, self.TotalKeyCount() - 1));
    BRange range = self.ContextRange(givenCursor: cursor);
    outCursorPastNode = range.Upperbound;
    (Dictionary<int, int>, Dictionary<int, int>)dictPair = self.NodeBorderPointDictPair();
    if (!dictPair.Item2.TryGetValue(cursor + 1, out int rearNodeID)) return null;
    return self.Count - 1 >= rearNodeID ? self[rearNodeID] : null;
  }
  public static Compositor.Node? FindNodeAt(this List<Compositor.Node> self, int givenCursor) {
    int mudamuda = 0;  // muda = useless.
    return self.FindNodeAt(givenCursor, ref mudamuda);
  }
}
}  // namespace Megrez
