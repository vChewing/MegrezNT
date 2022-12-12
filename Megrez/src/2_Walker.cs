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
  public (List<Node> WalkedNodes, bool Succeeded) Walk() {
    List<Node> result = new();
    try {
      if (Spans.IsEmpty()) return (result, true);
      List<List<Vertex>> vertexSpans = new();
      foreach (SpanUnit _ in Spans) vertexSpans.Add(new());
      foreach ((int i, SpanUnit span) in Spans.Enumerated()) {
        foreach (int j in new BRange(1, span.MaxLength + 1)) {
          if (span.NodeOf(j) is not {} theNode) continue;
          vertexSpans[i].Add(new(theNode));
        }
      }

      Vertex terminal = new(node: new(new() { "_TERMINAL_" }, spanLength: 0, unigrams: new()));

      foreach ((int i, List<Vertex> vertexSpan) in vertexSpans.Enumerated()) {
        foreach (Vertex vertex in vertexSpan) {
          int nextVertexPosition = i + vertex.Node.SpanLength;
          if (nextVertexPosition == vertexSpans.Count) {
            vertex.Edges.Add(terminal);
            continue;
          }
          foreach (Vertex nextVertex in vertexSpans[nextVertexPosition]) {
            vertex.Edges.Add(nextVertex);
          }
        }
      }

      Vertex root = new(node: new(new() { "_ROOT_" }, spanLength: 0, unigrams: new())) { Distance = 0 };
      root.Edges.AddRange(vertexSpans.First());

      List<Vertex> ordered = TopoSort(root);
      foreach ((int j, Vertex neta) in ordered.Reversed().Enumerated()) {
        foreach ((int k, _) in neta.Edges.Enumerated()) {
          if (neta.Edges[k] is {} relaxV) Relax(neta, ref relaxV);
        }
        ordered[j] = neta;
      }

      List<Node> walked = new();
      int totalLengthOfKeys = 0;
      Vertex iterated = terminal;
      while (iterated.Prev is {} itPrev) {
        walked.Add(itPrev.Node);
        iterated = itPrev;
        totalLengthOfKeys += iterated.Node.SpanLength;
      }

      if (totalLengthOfKeys != Keys.Count) {
        Console.WriteLine("!!! MEGREZ ERROR A.");
        return (result, false);
      }

      if (walked.Count < 2) {
        Console.WriteLine("!!! MEGREZ ERROR B.");
        return (result, false);
      }

      walked.Remove(walked.Last());
      result = walked.Reversed();
      return (result, true);
    } finally {
      WalkedNodes = result;
      UpdateCursorJumpingTables();
    }
  }
}
}