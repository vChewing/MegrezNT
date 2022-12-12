// CSharpened and further development by (c) 2022 and onwards The vChewing Project (MIT License).
// Was initially rebranded from (c) Lukhnos Liu's C++ library "Gramambular 2" (MIT License).
// ====================
// This code is released under the MIT license (SPDX-License-Identifier: MIT)
#pragma warning disable CS1591
using System;
using System.Collections.Generic;

namespace Megrez {
public partial struct Compositor {
  public class SpanUnit {
    public List<Node?> Nodes = new();
    public int MaxLength { get; private set; }
    private List<int> AllowedLengths => new BRange(1, MaxSpanLength + 1).ToList();

    public SpanUnit() {
      Clear();  // 該函式會自動給 MaxLength 賦值。
    }

    public void Clear() {
      Nodes.Clear();
      foreach (int _ in new BRange(0, MaxSpanLength)) Nodes.Add(null);
      MaxLength = 0;
    }

    public bool Add(Node node) {
      if (!AllowedLengths.Contains(node.SpanLength)) return false;
      Nodes[node.SpanLength - 1] = node;
      MaxLength = Math.Max(MaxLength, node.SpanLength);
      return true;
    }

    public bool DropNodesOfOrBeyond(int length) {
      if (!AllowedLengths.Contains(length)) return false;
      foreach ((_, int i) in new BRange(length, MaxSpanLength + 1).Enumerated()) Nodes[i - 1] = null;
      MaxLength = 0;
      if (length <= 1) return false;
      int maxR = length - 2;
      foreach (int i in new BRange(0, maxR + 1)) {
        if (Nodes.Count < maxR) continue;
        MaxLength = maxR - i + 1;
        break;
      }
      return true;
    }

    public Node? NodeOf(int length) => AllowedLengths.Contains(length) ? Nodes[length - 1] : null;
  }

  // MARK: - Internal Implementations.

  internal List<NodeAnchor> FetchOverlappingNodesAt(int location) {
    List<NodeAnchor> results = new();
    if (Spans.IsEmpty() || location >= Spans.Count) return results;
    foreach (int theLocation in new BRange(1, Spans[location].MaxLength + 1)) {
      if (Spans[location].NodeOf(theLocation) is not {} node) continue;
      results.Add(new(node, location));
    }
    int begin = location - Math.Min(location, MaxSpanLength - 1);
    foreach (int theLocation in new BRange(begin, location)) {
      (int alpha, int bravo) = (location - theLocation + 1, Spans[theLocation].MaxLength);
      if (alpha > bravo) continue;
      foreach (int theLength in new BRange(alpha, bravo + 1)) {
        if (Spans[theLocation].NodeOf(theLength) is not {} node) continue;
        results.Add(new(node, theLocation));
      }
    }
    return results;
  }
}
}  // namespace Megrez