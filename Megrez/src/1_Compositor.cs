// CSharpened and further development by (c) 2022 and onwards The vChewing Project (MIT License).
// Was initially rebranded from (c) Lukhnos Liu's C++ library "Gramambular 2" (MIT License).
// ====================
// This code is released under the MIT license (SPDX-License-Identifier: MIT)
#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Megrez {
public partial struct Compositor {
  // MARK: - Enums.
  public enum TypingDirection { ToFront, ToRear }
  public enum ResizeBehavior { Expand, Shrink }

  // MARK: - Variables.

  private static int _maxSpanLength = 10;
  public static int MaxSpanLength {
    get => _maxSpanLength;
    set => _maxSpanLength = Math.Max(6, value);
  }

  public static string TheSeparator = "-";

  private int _cursor = 0;
  public int Cursor {
    get => _cursor;
    set {
      _cursor = Math.Max(0, Math.Min(value, Length));
      _marker = Cursor;
    }
  }

  private int _marker = 0;
  public int Marker {
    get => _cursor;
    set => _marker = Math.Max(0, Math.Min(value, Length));
  }

  public string Separator {
    get => TheSeparator;
    set => TheSeparator = value;
  }

  public int Length => Keys.Count;
  public List<Node> WalkedNodes = new();
  public bool IsEmpty => Spans.IsEmpty() && Keys.IsEmpty();

  public List<string> Keys { get; private set; }
  public List<SpanUnit> Spans { get; private set; }

  private LangModelRanked _theLangModel;

  public LangModelRanked TheLangModel {
    get => _theLangModel;
    set {
      _theLangModel = value;
      Clear();
    }
  }

  public Dictionary<int, int> CursorRegionMap { get; private set; }

  public Compositor(LangModelProtocol langModel, string separator = "-") {
    _theLangModel = new(langModel);
    TheSeparator = separator;
    CursorRegionMap = new();
    Keys = new();
    Spans = new();
  }

  public void Clear() {
    _cursor = 0;
    _marker = 0;
    Keys.Clear();
    Spans.Clear();
    WalkedNodes.Clear();
    CursorRegionMap.Clear();
  }

  public bool InsertKey(string key) {
    if (string.IsNullOrEmpty(key) || key == Separator) return false;
    if (!TheLangModel.HasUnigramsFor(new() { key })) return false;
    Keys.Insert(Cursor, key);
    List<SpanUnit> gridBackup = Spans;
    ResizeGridAt(Cursor, ResizeBehavior.Expand);
    int nodesInserted = Update();
    if (nodesInserted == 0) {
      Spans = gridBackup;
      return false;
    }
    Cursor += 1;
    return true;
  }

  public bool DropKey(TypingDirection direction) {
    bool isBksp = direction == TypingDirection.ToRear;
    if (Cursor == (isBksp ? 0 : Keys.Count)) return false;
    Keys.RemoveAt(Cursor - (isBksp ? 1 : 0));
    Cursor -= isBksp ? 1 : 0;
    ResizeGridAt(Cursor, ResizeBehavior.Shrink);
    Update();
    return true;
  }

  public bool JumpCursorBySpan(TypingDirection direction, bool isMarker = false) {
    int target = isMarker ? Marker : Cursor;
    switch (direction) {
      case TypingDirection.ToFront:
        if (target == Length) return false;
        break;
      case TypingDirection.ToRear:
        if (target == 0) return false;
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
    }
    // var currentRegion = CursorRegionMap[target];  // <- 這樣雖然 C# 不會建置報錯，但可能會在運行時查詢失敗。
    // if (CursorRegionMap[target] is not int currentRegion) return false; <- 這樣是錯的。
    if (!CursorRegionMap.TryGetValue(key: target, out int currentRegion)) return false;  // <- 這樣是對的。
    int aRegionForward = Math.Max(currentRegion - 1, 0);
    int currentRegionBorderRear = WalkedNodes.GetRange(0, currentRegion).Select(x => x.SpanLength).Sum();

    if (target == currentRegionBorderRear) {
      target = direction switch {
        TypingDirection.ToFront => currentRegion > WalkedNodes.Count
                                       ? Keys.Count
                                       : WalkedNodes.GetRange(0, currentRegion + 1).Select(x => x.SpanLength).Sum(),
        TypingDirection.ToRear => WalkedNodes.GetRange(0, aRegionForward).Select(x => x.SpanLength).Sum(),
        _ => target
      };
    } else {
      target =
          direction switch { TypingDirection.ToFront => currentRegionBorderRear + WalkedNodes[currentRegion].SpanLength,
                             TypingDirection.ToRear => currentRegionBorderRear,
                             _ => target };
    }
    switch (isMarker) {
      case false:
        Cursor = target;
        break;
      case true:
        Marker = target;
        break;
    }
    return true;
  }

  public string DumpDOT() {
    // C# StringBuilder 與 Swift NSMutableString 能提供爆發性的效能。
    StringBuilder strOutput = new();
    strOutput.Append("digraph {\ngraph [ rankdir=LR ];\nBOS;\n");
    for (int p = 0; p < Spans.Count; p++) {
      SpanUnit span = Spans[p];
      for (int ni = 0; ni <= span.MaxLength; ni++) {
        if (span.NodeOf(ni) is not {} np) continue;
        if (p == 0) strOutput.Append("BOS -> " + np.CurrentPair.Value + ";\n");
        strOutput.Append(np.CurrentPair.Value + ";\n");
        if (p + ni < Spans.Count) {
          SpanUnit destinationSpan = Spans[p + ni];
          for (int q = 0; q <= destinationSpan.MaxLength; q++) {
            if (destinationSpan.NodeOf(q) is not {} dn) continue;
            strOutput.Append(np.CurrentPair.Value + " -> " + dn.CurrentPair.Value + ";\n");
          }
        }
        if (p + ni == Spans.Count) strOutput.Append(np.CurrentPair.Value + " -> EOS;\n");
      }
    }
    strOutput.Append("EOS;\n}\n");
    return strOutput.ToString();
  }

  // MARK: - Internal Methods (Maybe Public)

  internal void ResizeGridAt(int location, ResizeBehavior action) {
    location = Math.Max(Math.Min(location, Spans.Count), 0);  // 防呆。
    switch (action) {
      case ResizeBehavior.Expand:
        Spans.Insert(location, new());
        if (location == 0 || location == Spans.Count) return;
        break;
      case ResizeBehavior.Shrink:
        if (Spans.Count == location) return;
        Spans.RemoveAt(location);
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(action), action, null);
    }
    DropWreckedNodesAt(location);
  }

  internal void DropWreckedNodesAt(int location) {
    location = Math.Max(Math.Min(location, Spans.Count), 0);  // 防呆。
    if (Spans.IsEmpty()) return;
    int affectedLength = MaxSpanLength - 1;
    int begin = Math.Max(0, location - affectedLength);
    if (location < begin) return;
    foreach (int i in new BRange(begin, location)) {
      Spans[i].DropNodesOfOrBeyond(location - i + 1);
    }
  }

  internal List<string> GetJoinedKeyArray(BRange range) =>
      range.Upperbound <= Keys.Count && range.Lowerbound >= 0
          ? Keys.GetRange(range.Lowerbound, range.Upperbound - range.Lowerbound).ToList()
          : new();

  internal Node? GetNodeAt(int location, int length, List<string> keyArray) {
    location = Math.Max(Math.Min(location, Spans.Count - 1), 0);  // 防呆。
    if (Spans[location].NodeOf(length) is not {} node) return null;
    return keyArray == node.KeyArray ? node : null;
  }

  public int Update(bool updateExisting = false) {
    BRange range = new(Math.Max(0, Cursor - MaxSpanLength), Math.Min(Cursor + MaxSpanLength, Keys.Count));
    int nodesChanged = 0;
    foreach (int position in range) {
      foreach (int theLength in new BRange(1, Math.Min(MaxSpanLength, range.Upperbound - position) + 1)) {
        List<string> joinedKeyArray = GetJoinedKeyArray(new(position, position + theLength));
        if (GetNodeAt(position, theLength, joinedKeyArray) is {} theNode) {
          if (!updateExisting) continue;
          List<Unigram> unigramsA = TheLangModel.UnigramsFor(joinedKeyArray);
          if (unigramsA.IsEmpty()) {
            if (theNode.KeyArray.Count == 1) continue;
            Spans[position].Nodes.RemoveAll(x => Equals(x, theNode));
          } else {
            theNode.SyncingUnigramsFrom(unigramsA);
          }
          nodesChanged += 1;
          continue;
        }
        List<Unigram> unigramsB = TheLangModel.UnigramsFor(joinedKeyArray);
        if (unigramsB.IsEmpty()) continue;
        Spans[position].Add(new(joinedKeyArray, theLength, unigramsB));
        nodesChanged += 1;
      }
    }
    return nodesChanged;
  }

  internal void UpdateCursorJumpingTables() {
    Dictionary<int, int> cursorRegionMapDict = new() { [-1] = 0 };  // 防呆。
    int counter = 0;
    foreach ((int i, Node theNode) in WalkedNodes.Enumerated()) {
      foreach (int _ in new BRange(0, theNode.SpanLength)) {
        cursorRegionMapDict[counter] = i;
        counter += 1;
      }
    }
    cursorRegionMapDict[counter] = WalkedNodes.Count;
    CursorRegionMap = cursorRegionMapDict;
  }
}
}  // namespace Megrez
