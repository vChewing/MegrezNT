// CSharpened and further development by (c) 2022 and onwards The vChewing Project (MIT License).
// Was initially rebranded from (c) Lukhnos Liu's C++ library "Gramambular 2" (MIT License).
// ====================
// This code is released under the MIT license (SPDX-License-Identifier: MIT)
#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Megrez {
public partial struct Compositor {
  public struct KeyValuePaired {
    public List<string> KeyArray { get; }
    public string Value { get; }
    public KeyValuePaired(List<string> keyArray, string value) {
      KeyArray = keyArray;
      Value = value;
    }
    public KeyValuePaired(string key, string value) {
      KeyArray = key.Split(TheSeparator.ToCharArray()).ToList();
      Value = value;
    }
    public string JoinedKey(string? separator = null) => string.Join(separator ?? TheSeparator, KeyArray.ToArray());

    public bool IsValid => !string.IsNullOrEmpty(JoinedKey()) && !string.IsNullOrEmpty(Value);

    public override bool Equals(object obj) {
      return obj is KeyValuePaired pair && JoinedKey() == pair.JoinedKey() && Value == pair.Value;
    }

    public override int GetHashCode() => HashCode.Combine(KeyArray, Value);
    public override string ToString() => $"({JoinedKey()},{Value})";

    public string ToNGramKey => IsValid ? $"({JoinedKey()},{Value})" : "()";
    public static bool operator ==(KeyValuePaired lhs, KeyValuePaired rhs) {
      return lhs.KeyArray.Count == rhs.KeyArray.Count && lhs.Value == rhs.Value;
    }
    public static bool operator !=(KeyValuePaired lhs, KeyValuePaired rhs) {
      return lhs.KeyArray.Count != rhs.KeyArray.Count || lhs.Value != rhs.Value;
    }

    public static bool operator<(KeyValuePaired lhs, KeyValuePaired rhs) {
      return lhs.KeyArray.Count < rhs.KeyArray.Count ||
             lhs.KeyArray.Count == rhs.KeyArray.Count &&
                 string.Compare(lhs.Value, rhs.Value, StringComparison.Ordinal) < 0;
    }

    public static bool operator>(KeyValuePaired lhs, KeyValuePaired rhs) {
      return lhs.KeyArray.Count > rhs.KeyArray.Count ||
             lhs.KeyArray.Count == rhs.KeyArray.Count &&
                 string.Compare(lhs.Value, rhs.Value, StringComparison.Ordinal) > 0;
    }

    public static bool operator <=(KeyValuePaired lhs, KeyValuePaired rhs) {
      return lhs.KeyArray.Count <= rhs.KeyArray.Count ||
             lhs.KeyArray.Count == rhs.KeyArray.Count &&
                 string.Compare(lhs.Value, rhs.Value, StringComparison.Ordinal) <= 0;
    }

    public static bool operator >=(KeyValuePaired lhs, KeyValuePaired rhs) {
      return lhs.KeyArray.Count >= rhs.KeyArray.Count ||
             lhs.KeyArray.Count == rhs.KeyArray.Count &&
                 string.Compare(lhs.Value, rhs.Value, StringComparison.Ordinal) >= 0;
    }
  }

  public enum CandidateFetchFilter { All, BeginAt, EndAt }

  public List<KeyValuePaired> FetchCandidatesAt(int location, CandidateFetchFilter filter = CandidateFetchFilter.All) {
    List<KeyValuePaired> result = new();
    if (Keys.IsEmpty()) return result;
    location = Math.Max(0, Math.Min(location, Keys.Count - 1));
    List<NodeAnchor> anchors =
        FetchOverlappingNodesAt(location).StableSorted((x, y) => x.SpanLength.CompareTo(y.SpanLength));
    string keyAtCursor = Keys[location];
    foreach (Node theNode in anchors.Select(x => x.Node).Where(x => !x.KeyArray.IsEmpty())) {
      foreach (Unigram gram in theNode.Unigrams) {
        switch (filter) {
          case CandidateFetchFilter.All:
            if (!theNode.KeyArray.Contains(keyAtCursor)) continue;
            break;
          case CandidateFetchFilter.BeginAt:
            if (theNode.KeyArray.First() != keyAtCursor) continue;
            break;
          case CandidateFetchFilter.EndAt:
            if (theNode.KeyArray.Last() != keyAtCursor) continue;
            break;
        }
        result.Add(new(theNode.KeyArray, gram.Value));
      }
    }
    return result;
  }

  public bool OverrideCandidate(KeyValuePaired candidate, int location,
                                Node.OverrideType overrideType = Node.OverrideType.HighScore) =>
      OverrideCandidateAgainst(candidate.KeyArray, location, candidate.Value, overrideType);

  public bool OverrideCandidateLiteral(string candidate, int location,
                                       Node.OverrideType overrideType = Node.OverrideType.HighScore) =>
      OverrideCandidateAgainst(null, location, candidate, overrideType);

  internal bool OverrideCandidateAgainst(List<string>? keyArray, int location, string value,
                                         Node.OverrideType overrideType) {
    location = Math.Max(Math.Min(location, Keys.Count), 0);
    List<NodeAnchor> arrOverlappedNodes = FetchOverlappingNodesAt(Math.Min(Keys.Count - 1, location));
    Node fakeNode = new(new() { "_NULL_" }, spanLength: 0, new());
    NodeAnchor overridden = new(fakeNode, spanIndex: 0);
    foreach (NodeAnchor anchor in arrOverlappedNodes.Where(
                 anchor => (keyArray == null || anchor.Node.KeyArray.SequenceEqual(keyArray)) &&
                           anchor.Node.SelectOverrideUnigram(value, overrideType))) {
      overridden.Node = anchor.Node;
      overridden.SpanIndex = anchor.SpanIndex;
      break;
    }
    if (Equals(overridden.Node, fakeNode)) return false;

    // Nerfing the overridden weights of contextural nodes.
    int lengthUpperBound = Math.Min(Spans.Count, overridden.SpanIndex + overridden.Node.SpanLength);
    foreach (int i in new BRange(overridden.SpanIndex, lengthUpperBound)) {
      arrOverlappedNodes = FetchOverlappingNodesAt(i);
      foreach (NodeAnchor anchor in arrOverlappedNodes.Where(anchor => !Equals(anchor.Node, overridden.Node))) {
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
