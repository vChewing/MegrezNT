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
  internal class Vertex {
    public Vertex? Prev;
    public List<Vertex> Edges = new();
    public double Distance = double.NegativeInfinity;
    public bool TopoSorted;
    public Node Node;

    public Vertex(Node node) { Node = node; }
  }
  internal void Relax(Vertex u, ref Vertex v) {
    double w = v.Node.Score;
    if (v.Distance >= u.Distance + w) return;
    v.Distance = u.Distance + w;
    v.Prev = u;
  }
  internal class TopoSortState {
    public int IterIndex { get; set; }
    public Vertex Vertex { get; set; }
    public TopoSortState(Vertex vertex, int iterIndex = 0) {
      Vertex = vertex;
      IterIndex = iterIndex;
    }
  }
  internal List<Vertex> TopoSort(Vertex root) {
    List<Vertex> result = new();
    List<TopoSortState> stack = new();
    stack.Add(new(root));
    while (!stack.IsEmpty()) {
      TopoSortState state = stack.Last();
      Vertex theVertex = state.Vertex;
      if (state.IterIndex < state.Vertex.Edges.Count) {
        Vertex newVertex = state.Vertex.Edges[state.IterIndex];
        state.IterIndex += 1;
        if (!newVertex.TopoSorted) {
          stack.Add(new(newVertex));
          continue;
        }
      }
      theVertex.TopoSorted = true;
      result.Add(theVertex);
      stack.Remove(stack.Last());
    }
    return result;
  }
}
}  // namespace Megrez