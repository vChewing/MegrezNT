// CSharpened and further development by (c) 2022 and onwards The vChewing Project (MIT License).
// Was initially rebranded from (c) Lukhnos Liu's C++ library "Gramambular 2" (MIT License).
// ====================
// This code is released under the MIT license (SPDX-License-Identifier: MIT)

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Megrez.Tests;

public class MegrezTests : TestDataClass {
  [Test]
  public void Test01_SpanUnitInternalAbilities() {
    SimpleLM langModel = new(input: StrSampleData);
    Compositor.SpanUnit span = new();
    Node n1 = new(keyArray: new() { "gao" }, spanLength: 1, unigrams: langModel.UnigramsFor(new() { "gao1" }));
    Node n3 = new(keyArray: new() { "gao1", "ke1", "ji4" }, spanLength: 3,
                  unigrams: langModel.UnigramsFor(new() { "gao1ke1ji4" }));
    Assert.AreEqual(actual: span.MaxLength, expected: 0);
    span.Append(node: n1);
    Assert.AreEqual(actual: span.MaxLength, expected: 1);
    span.Append(node: n3);
    Assert.AreEqual(actual: span.MaxLength, expected: 3);
    Assert.AreEqual(actual: span.NodeOf(length: 1), expected: n1);
    Assert.AreEqual(actual: span.NodeOf(length: 2), expected: null);
    Assert.AreEqual(actual: span.NodeOf(length: 3), expected: n3);
    span.Clear();
    Assert.AreEqual(actual: span.MaxLength, expected: 0);
    Assert.AreEqual(actual: span.NodeOf(length: 1), expected: null);
    Assert.AreEqual(actual: span.NodeOf(length: 2), expected: null);
    Assert.AreEqual(actual: span.NodeOf(length: 3), expected: null);

    span.Append(node: n1);
    span.Append(node: n3);
    span.DropNodesOfOrBeyond(length: 2);
    Assert.AreEqual(actual: span.MaxLength, expected: 1);
    Assert.AreEqual(actual: span.NodeOf(length: 1), expected: n1);
    Assert.AreEqual(actual: span.NodeOf(length: 2), expected: null);
    Assert.AreEqual(actual: span.NodeOf(length: 3), expected: null);
    span.DropNodesOfOrBeyond(length: 1);
    Assert.AreEqual(actual: span.MaxLength, expected: 0);
    Assert.AreEqual(actual: span.NodeOf(length: 1), expected: null);
    Node n114514 = new(new(), 114_514, new());
    Assert.IsFalse(span.Append(n114514));
    Assert.IsNull(span.NodeOf(length: 0));
    Assert.IsNull(span.NodeOf(length: Compositor.MaxSpanLength + 1));
  }

  [Test]
  public void Test02_RankedLanguageModel() {
    LangModelProtocol lmTest = new TestLMForRanked();
    Compositor.LangModelRanked lmRanked = new(langModel: ref lmTest);
    Assert.IsTrue(lmRanked.HasUnigramsFor(new() { "foo" }));
    Assert.IsFalse(lmRanked.HasUnigramsFor(new() { "bar" }));
    Assert.IsEmpty(lmRanked.UnigramsFor(new() { "bar" }));
    List<Unigram> unigrams = lmRanked.UnigramsFor(new() { "foo" });
    Assert.AreEqual(actual: unigrams.Count, expected: 3);
    Assert.AreEqual(actual: unigrams[0], expected: new Unigram("highest", -2));
    Assert.AreEqual(actual: unigrams[1], expected: new Unigram("middle", -5));
    Assert.AreEqual(actual: unigrams[2], expected: new Unigram("lowest", -10));
  }

  [Test]
  public void Test03_BasicFeaturesOfCompositor() {
    Compositor compositor = new(langModel: new MockLM(), separator: "");
    Assert.AreEqual(actual: compositor.Separator, expected: "");
    Assert.AreEqual(actual: compositor.Cursor, expected: 0);
    Assert.AreEqual(actual: compositor.Length, expected: 0);

    Assert.IsTrue(compositor.InsertKey("a"));
    Assert.AreEqual(actual: compositor.Cursor, expected: 1);
    Assert.AreEqual(actual: compositor.Length, expected: 1);
    Assert.AreEqual(actual: compositor.Spans.Count, expected: 1);
    Assert.AreEqual(actual: compositor.Spans[0].MaxLength, expected: 1);
    if (compositor.Spans[0].NodeOf(length: 1) is not {} zeroNode) return;
    Assert.AreEqual(actual: zeroNode.KeyArray.Joined(), expected: "a");

    Assert.IsTrue(compositor.DropKey(direction: Compositor.TypingDirection.ToRear));
    Assert.AreEqual(actual: compositor.Cursor, expected: 0);
    Assert.AreEqual(actual: compositor.Length, expected: 0);
    Assert.AreEqual(actual: compositor.Spans.Count, expected: 0);
  }

  [Test]
  public void Test04_InvalidOperations() {
    Compositor compositor = new(langModel: new TestLM(), separator: ";");
    Assert.IsFalse(compositor.InsertKey("bar"));
    Assert.IsFalse(compositor.InsertKey(""));
    Assert.IsFalse(compositor.InsertKey(""));
    Assert.IsFalse(compositor.DropKey(direction: Compositor.TypingDirection.ToRear));
    Assert.IsFalse(compositor.DropKey(direction: Compositor.TypingDirection.ToFront));

    Assert.IsTrue(compositor.InsertKey("foo"));
    Assert.IsTrue(compositor.DropKey(direction: Compositor.TypingDirection.ToRear));
    Assert.AreEqual(actual: compositor.Length, expected: 0);
    Assert.IsTrue(compositor.InsertKey("foo"));
    compositor.Cursor = 0;
    Assert.IsTrue(compositor.DropKey(direction: Compositor.TypingDirection.ToFront));
    Assert.AreEqual(actual: compositor.Length, expected: 0);
  }

  [Test]
  public void Test05_DeleteToTheFrontOfCursor() {
    Compositor compositor = new(langModel: new MockLM());
    compositor.InsertKey("a");
    compositor.Cursor = 0;
    Assert.AreEqual(actual: compositor.Cursor, expected: 0);
    Assert.AreEqual(actual: compositor.Length, expected: 1);
    Assert.AreEqual(actual: compositor.Spans.Count, expected: 1);
    Assert.IsFalse(compositor.DropKey(direction: Compositor.TypingDirection.ToRear));
    Assert.AreEqual(actual: compositor.Cursor, expected: 0);
    Assert.AreEqual(actual: compositor.Length, expected: 1);
    Assert.AreEqual(actual: compositor.Spans.Count, expected: 1);
    Assert.IsTrue(compositor.DropKey(direction: Compositor.TypingDirection.ToFront));
    Assert.AreEqual(actual: compositor.Cursor, expected: 0);
    Assert.AreEqual(actual: compositor.Length, expected: 0);
    Assert.AreEqual(actual: compositor.Spans.Count, expected: 0);
  }

  [Test]
  public void Test06_MultipleSpanUnits() {
    Compositor compositor = new(langModel: new MockLM(), separator: ";");
    compositor.InsertKey("a");
    compositor.InsertKey("b");
    compositor.InsertKey("c");
    Assert.AreEqual(actual: compositor.Cursor, expected: 3);
    Assert.AreEqual(actual: compositor.Length, expected: 3);
    Assert.AreEqual(actual: compositor.Spans.Count, expected: 3);
    Assert.AreEqual(actual: compositor.Spans[0].MaxLength, expected: 3);
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 1)?.JoinedKey(), expected: "a");
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 2)?.JoinedKey(), expected: "a;b");
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 3)?.JoinedKey(), expected: "a;b;c");
    Assert.AreEqual(actual: compositor.Spans[1].MaxLength, expected: 2);
    Assert.AreEqual(actual: compositor.Spans[1].NodeOf(length: 1)?.JoinedKey(), expected: "b");
    Assert.AreEqual(actual: compositor.Spans[1].NodeOf(length: 2)?.JoinedKey(), expected: "b;c");
    Assert.AreEqual(actual: compositor.Spans[2].MaxLength, expected: 1);
    Assert.AreEqual(actual: compositor.Spans[2].NodeOf(length: 1)?.JoinedKey(), expected: "c");
  }

  [Test]
  public void Test07_SpanUnitDeletionFromFront() {
    Compositor compositor = new(langModel: new MockLM(), separator: ";");
    compositor.InsertKey("a");
    compositor.InsertKey("b");
    compositor.InsertKey("c");
    Assert.IsFalse(compositor.DropKey(direction: Compositor.TypingDirection.ToFront));
    Assert.IsTrue(compositor.DropKey(direction: Compositor.TypingDirection.ToRear));
    Assert.AreEqual(actual: compositor.Cursor, expected: 2);
    Assert.AreEqual(actual: compositor.Length, expected: 2);
    Assert.AreEqual(actual: compositor.Spans.Count, expected: 2);
    Assert.AreEqual(actual: compositor.Spans[0].MaxLength, expected: 2);
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 1)?.JoinedKey(), expected: "a");
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 2)?.JoinedKey(), expected: "a;b");
    Assert.AreEqual(actual: compositor.Spans[1].MaxLength, expected: 1);
    Assert.AreEqual(actual: compositor.Spans[1].NodeOf(length: 1)?.JoinedKey(), expected: "b");
  }

  [Test]
  public void Test08_SpanUnitDeletionFromMiddle() {
    Compositor compositor = new(langModel: new MockLM(), separator: ";");
    compositor.InsertKey("a");
    compositor.InsertKey("b");
    compositor.InsertKey("c");
    compositor.Cursor = 2;

    Assert.IsTrue(compositor.DropKey(direction: Compositor.TypingDirection.ToRear));
    Assert.AreEqual(actual: compositor.Cursor, expected: 1);
    Assert.AreEqual(actual: compositor.Length, expected: 2);
    Assert.AreEqual(actual: compositor.Spans.Count, expected: 2);
    Assert.AreEqual(actual: compositor.Spans[0].MaxLength, expected: 2);
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 1)?.JoinedKey(), expected: "a");
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 2)?.JoinedKey(), expected: "a;c");
    Assert.AreEqual(actual: compositor.Spans[1].MaxLength, expected: 1);
    Assert.AreEqual(actual: compositor.Spans[1].NodeOf(length: 1)?.JoinedKey(), expected: "c");

    compositor.Clear();
    compositor.InsertKey("a");
    compositor.InsertKey("b");
    compositor.InsertKey("c");
    compositor.Cursor = 1;

    Assert.IsTrue(compositor.DropKey(direction: Compositor.TypingDirection.ToFront));
    Assert.AreEqual(actual: compositor.Cursor, expected: 1);
    Assert.AreEqual(actual: compositor.Length, expected: 2);
    Assert.AreEqual(actual: compositor.Spans.Count, expected: 2);
    Assert.AreEqual(actual: compositor.Spans[0].MaxLength, expected: 2);
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 1)?.JoinedKey(), expected: "a");
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 2)?.JoinedKey(), expected: "a;c");
    Assert.AreEqual(actual: compositor.Spans[1].MaxLength, expected: 1);
    Assert.AreEqual(actual: compositor.Spans[1].NodeOf(length: 1)?.JoinedKey(), expected: "c");
  }

  [Test]
  public void Test09_SpanUnitDeletionFromRear() {
    Compositor compositor = new(langModel: new MockLM(), separator: ";");
    compositor.InsertKey("a");
    compositor.InsertKey("b");
    compositor.InsertKey("c");
    compositor.Cursor = 0;

    Assert.IsFalse(compositor.DropKey(direction: Compositor.TypingDirection.ToRear));
    Assert.IsTrue(compositor.DropKey(direction: Compositor.TypingDirection.ToFront));
    Assert.AreEqual(actual: compositor.Cursor, expected: 0);
    Assert.AreEqual(actual: compositor.Length, expected: 2);
    Assert.AreEqual(actual: compositor.Spans.Count, expected: 2);
    Assert.AreEqual(actual: compositor.Spans[0].MaxLength, expected: 2);
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 1)?.JoinedKey(), expected: "b");
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 2)?.JoinedKey(), expected: "b;c");
    Assert.AreEqual(actual: compositor.Spans[1].MaxLength, expected: 1);
    Assert.AreEqual(actual: compositor.Spans[1].NodeOf(length: 1)?.JoinedKey(), expected: "c");
  }

  [Test]
  public void Test10_SpanUnitInsertion() {
    Compositor compositor = new(langModel: new MockLM(), separator: ";");
    compositor.InsertKey("a");
    compositor.InsertKey("b");
    compositor.InsertKey("c");
    compositor.Cursor = 1;
    compositor.InsertKey("X");

    Assert.AreEqual(actual: compositor.Cursor, expected: 2);
    Assert.AreEqual(actual: compositor.Length, expected: 4);
    Assert.AreEqual(actual: compositor.Spans.Count, expected: 4);
    Assert.AreEqual(actual: compositor.Spans[0].MaxLength, expected: 4);
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 1)?.JoinedKey(), expected: "a");
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 2)?.JoinedKey(), expected: "a;X");
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 3)?.JoinedKey(), expected: "a;X;b");
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 4)?.JoinedKey(), expected: "a;X;b;c");
    Assert.AreEqual(actual: compositor.Spans[1].MaxLength, expected: 3);
    Assert.AreEqual(actual: compositor.Spans[1].NodeOf(length: 1)?.JoinedKey(), expected: "X");
    Assert.AreEqual(actual: compositor.Spans[1].NodeOf(length: 2)?.JoinedKey(), expected: "X;b");
    Assert.AreEqual(actual: compositor.Spans[1].NodeOf(length: 3)?.JoinedKey(), expected: "X;b;c");
    Assert.AreEqual(actual: compositor.Spans[2].MaxLength, expected: 2);
    Assert.AreEqual(actual: compositor.Spans[2].NodeOf(length: 1)?.JoinedKey(), expected: "b");
    Assert.AreEqual(actual: compositor.Spans[2].NodeOf(length: 2)?.JoinedKey(), expected: "b;c");
    Assert.AreEqual(actual: compositor.Spans[3].MaxLength, expected: 1);
    Assert.AreEqual(actual: compositor.Spans[3].NodeOf(length: 1)?.JoinedKey(), expected: "c");
  }

  [Test]
  public void Test11_LongGridDeletion() {
    Compositor compositor = new(langModel: new MockLM(), separator: "");
    compositor.InsertKey("a");
    compositor.InsertKey("b");
    compositor.InsertKey("c");
    compositor.InsertKey("d");
    compositor.InsertKey("e");
    compositor.InsertKey("f");
    compositor.InsertKey("g");
    compositor.InsertKey("h");
    compositor.InsertKey("i");
    compositor.InsertKey("j");
    compositor.InsertKey("k");
    compositor.InsertKey("l");
    compositor.InsertKey("m");
    compositor.InsertKey("n");
    compositor.Cursor = 7;
    Assert.IsTrue(compositor.DropKey(direction: Compositor.TypingDirection.ToRear));
    Assert.AreEqual(actual: compositor.Cursor, expected: 6);
    Assert.AreEqual(actual: compositor.Length, expected: 13);
    Assert.AreEqual(actual: compositor.Spans.Count, expected: 13);
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 6)?.JoinedKey(), expected: "abcdef");
    Assert.AreEqual(actual: compositor.Spans[1].NodeOf(length: 6)?.JoinedKey(), expected: "bcdefh");
    Assert.AreEqual(actual: compositor.Spans[1].NodeOf(length: 5)?.JoinedKey(), expected: "bcdef");
    Assert.AreEqual(actual: compositor.Spans[2].NodeOf(length: 6)?.JoinedKey(), expected: "cdefhi");
    Assert.AreEqual(actual: compositor.Spans[2].NodeOf(length: 5)?.JoinedKey(), expected: "cdefh");
    Assert.AreEqual(actual: compositor.Spans[3].NodeOf(length: 6)?.JoinedKey(), expected: "defhij");
    Assert.AreEqual(actual: compositor.Spans[4].NodeOf(length: 6)?.JoinedKey(), expected: "efhijk");
    Assert.AreEqual(actual: compositor.Spans[5].NodeOf(length: 6)?.JoinedKey(), expected: "fhijkl");
    Assert.AreEqual(actual: compositor.Spans[6].NodeOf(length: 6)?.JoinedKey(), expected: "hijklm");
    Assert.AreEqual(actual: compositor.Spans[7].NodeOf(length: 6)?.JoinedKey(), expected: "ijklmn");
    Assert.AreEqual(actual: compositor.Spans[8].NodeOf(length: 5)?.JoinedKey(), expected: "jklmn");
  }

  [Test]
  public void Test12_LongGridInsertion() {
    Compositor compositor = new(langModel: new MockLM(), separator: "");
    compositor.InsertKey("a");
    compositor.InsertKey("b");
    compositor.InsertKey("c");
    compositor.InsertKey("d");
    compositor.InsertKey("e");
    compositor.InsertKey("f");
    compositor.InsertKey("g");
    compositor.InsertKey("h");
    compositor.InsertKey("i");
    compositor.InsertKey("j");
    compositor.InsertKey("k");
    compositor.InsertKey("l");
    compositor.InsertKey("m");
    compositor.InsertKey("n");
    compositor.Cursor = 7;
    compositor.InsertKey("X");
    Assert.AreEqual(actual: compositor.Cursor, expected: 8);
    Assert.AreEqual(actual: compositor.Length, expected: 15);
    Assert.AreEqual(actual: compositor.Spans.Count, expected: 15);
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 6)?.JoinedKey(), expected: "abcdef");
    Assert.AreEqual(actual: compositor.Spans[1].NodeOf(length: 6)?.JoinedKey(), expected: "bcdefg");
    Assert.AreEqual(actual: compositor.Spans[2].NodeOf(length: 6)?.JoinedKey(), expected: "cdefgX");
    Assert.AreEqual(actual: compositor.Spans[3].NodeOf(length: 6)?.JoinedKey(), expected: "defgXh");
    Assert.AreEqual(actual: compositor.Spans[3].NodeOf(length: 5)?.JoinedKey(), expected: "defgX");
    Assert.AreEqual(actual: compositor.Spans[4].NodeOf(length: 6)?.JoinedKey(), expected: "efgXhi");
    Assert.AreEqual(actual: compositor.Spans[4].NodeOf(length: 5)?.JoinedKey(), expected: "efgXh");
    Assert.AreEqual(actual: compositor.Spans[4].NodeOf(length: 4)?.JoinedKey(), expected: "efgX");
    Assert.AreEqual(actual: compositor.Spans[4].NodeOf(length: 3)?.JoinedKey(), expected: "efg");
    Assert.AreEqual(actual: compositor.Spans[5].NodeOf(length: 6)?.JoinedKey(), expected: "fgXhij");
    Assert.AreEqual(actual: compositor.Spans[6].NodeOf(length: 6)?.JoinedKey(), expected: "gXhijk");
    Assert.AreEqual(actual: compositor.Spans[7].NodeOf(length: 6)?.JoinedKey(), expected: "Xhijkl");
    Assert.AreEqual(actual: compositor.Spans[8].NodeOf(length: 6)?.JoinedKey(), expected: "hijklm");
  }

  [Test]
  public void Test13_WalkerBenchMark() {
    Console.WriteLine("// Stress test preparation begins.");
    Compositor compositor = new(langModel: new SimpleLM(input: StrStressData));
    foreach (int _ in new BRange(0, 1919)) compositor.InsertKey("yi1");
    Console.WriteLine("// Stress test preparation started with keys inserted: " + compositor.Keys.Count);
    DateTime startTime = DateTime.Now;
    compositor.Walk();
    TimeSpan timeElapsed = DateTime.Now - startTime;
    Console.WriteLine($"// Normal walk: Time test elapsed: {timeElapsed.TotalSeconds}s.");
  }

  [Test]
  public void Test14_WordSegmentation() {
    Compositor compositor = new(langModel: new SimpleLM(input: StrSampleData, swapKeyValue: true), separator: "");
    string testStr = "高科技公司的年終獎金";
    List<string> arrStr = testStr.LiteralCharComponents();
    foreach (string c in arrStr) compositor.InsertKey(c);
    Assert.AreEqual(actual: compositor.Walk().WalkedNodes.JoinedKeys(separator: ""),
                    expected: new List<string> { "高科技", "公司", "的", "年終", "獎金" });
  }

  [Test]
  public void Test15_Compositor_InputTestAndCursorJump() {
    Compositor compositor = new(langModel: new SimpleLM(input: StrSampleData), separator: "");
    compositor.InsertKey("gao1");
    compositor.Walk();
    compositor.InsertKey("ji4");
    compositor.Walk();
    compositor.Cursor = 1;
    compositor.InsertKey("ke1");
    compositor.Walk();
    compositor.Cursor = 0;
    compositor.DropKey(direction: Compositor.TypingDirection.ToFront);
    compositor.Walk();
    compositor.InsertKey("gao1");
    compositor.Walk();
    compositor.Cursor = compositor.Length;
    compositor.InsertKey("gong1");
    compositor.Walk();
    compositor.InsertKey("si1");
    compositor.Walk();
    compositor.InsertKey("de5");
    compositor.Walk();
    compositor.InsertKey("nian2");
    compositor.Walk();
    compositor.InsertKey("zhong1");
    compositor.Walk();
    compositor.InsertKey("jiang3");
    compositor.Walk();
    compositor.InsertKey("jin1");
    List<Node> result = compositor.Walk().WalkedNodes;
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "高科技", "公司", "的", "年中", "獎金" });
    Assert.AreEqual(actual: compositor.Length, expected: 10);
    compositor.Cursor = 7;
    List<string> candidates = compositor.FetchCandidatesAt(compositor.Cursor).Select(x => x.Value).ToList();
    Assert.IsTrue(candidates.Contains("年中"));
    Assert.IsTrue(candidates.Contains("年終"));
    Assert.IsTrue(candidates.Contains("中"));
    Assert.IsTrue(candidates.Contains("鍾"));
    Assert.IsTrue(compositor.OverrideCandidateLiteral("年終", location: 7));
    result = compositor.Walk().WalkedNodes;
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "高科技", "公司", "的", "年終", "獎金" });
    List<string> candidatesBeginAt =
        compositor.FetchCandidatesAt(3, filter: Compositor.CandidateFetchFilter.BeginAt).Select(x => x.Value).ToList();
    List<string> candidatesEndAt =
        compositor.FetchCandidatesAt(3, filter: Compositor.CandidateFetchFilter.EndAt).Select(x => x.Value).ToList();
    Assert.IsFalse(candidatesBeginAt.Contains("濟公"));
    Assert.IsFalse(candidatesEndAt.Contains("公司"));
    // Test cursor jump.
    compositor.Cursor = 8;
    Assert.IsTrue(compositor.JumpCursorBySpan(direction: Compositor.TypingDirection.ToRear));
    Assert.AreEqual(actual: compositor.Cursor, expected: 6);
    Assert.IsTrue(compositor.JumpCursorBySpan(direction: Compositor.TypingDirection.ToRear));
    Assert.AreEqual(actual: compositor.Cursor, expected: 5);
    Assert.IsTrue(compositor.JumpCursorBySpan(direction: Compositor.TypingDirection.ToRear));
    Assert.AreEqual(actual: compositor.Cursor, expected: 3);
    Assert.IsTrue(compositor.JumpCursorBySpan(direction: Compositor.TypingDirection.ToRear));
    Assert.AreEqual(actual: compositor.Cursor, expected: 0);
    Assert.IsFalse(compositor.JumpCursorBySpan(direction: Compositor.TypingDirection.ToRear));
    Assert.AreEqual(actual: compositor.Cursor, expected: 0);
    Assert.IsTrue(compositor.JumpCursorBySpan(direction: Compositor.TypingDirection.ToFront));
    Assert.AreEqual(actual: compositor.Cursor, expected: 3);
    Assert.IsTrue(compositor.JumpCursorBySpan(direction: Compositor.TypingDirection.ToFront));
    Assert.AreEqual(actual: compositor.Cursor, expected: 5);
    Assert.IsTrue(compositor.JumpCursorBySpan(direction: Compositor.TypingDirection.ToFront));
    Assert.AreEqual(actual: compositor.Cursor, expected: 6);
    Assert.IsTrue(compositor.JumpCursorBySpan(direction: Compositor.TypingDirection.ToFront));
    Assert.AreEqual(actual: compositor.Cursor, expected: 8);
    Assert.IsTrue(compositor.JumpCursorBySpan(direction: Compositor.TypingDirection.ToFront));
    Assert.AreEqual(actual: compositor.Cursor, expected: 10);
    Assert.IsFalse(compositor.JumpCursorBySpan(direction: Compositor.TypingDirection.ToFront));
    Assert.AreEqual(actual: compositor.Cursor, expected: 10);
    // Test DumpDOT.
    string expectedDumpDOT =
        "digraph {\ngraph [ rankdir=LR ];\nBOS;\nBOS -> 高;\n高;\n高 -> 科;\n高 -> 科技;\nBOS -> 高科技;\n高科技;\n高科技 -> 工;\n高科技 -> 公司;\n科;\n科 -> 際;\n科 -> 濟公;\n科技;\n科技 -> 工;\n科技 -> 公司;\n際;\n際 -> 工;\n際 -> 公司;\n濟公;\n濟公 -> 斯;\n工;\n工 -> 斯;\n公司;\n公司 -> 的;\n斯;\n斯 -> 的;\n的;\n的 -> 年;\n的 -> 年終;\n年;\n年 -> 中;\n年終;\n年終 -> 獎;\n年終 -> 獎金;\n中;\n中 -> 獎;\n中 -> 獎金;\n獎;\n獎 -> 金;\n獎金;\n獎金 -> EOS;\n金;\n金 -> EOS;\nEOS;\n}\n";
    Assert.AreEqual(actual: compositor.DumpDOT(), expected: expectedDumpDOT);
  }

  [Test]
  public void Test16_Compositor_InputTest2() {
    Compositor compositor = new(langModel: new SimpleLM(input: StrSampleData), separator: "");
    compositor.InsertKey("gao1");
    compositor.InsertKey("ke1");
    compositor.InsertKey("ji4");
    List<Node> result = compositor.Walk().WalkedNodes;
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "高科技" });
    compositor.InsertKey("gong1");
    compositor.InsertKey("si1");
    result = compositor.Walk().WalkedNodes;
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "高科技", "公司" });
  }

  [Test]
  public void Test17_Compositor_OverrideOverlappingNodes() {
    Compositor compositor = new(langModel: new SimpleLM(input: StrSampleData), separator: "");
    compositor.InsertKey("gao1");
    compositor.InsertKey("ke1");
    compositor.InsertKey("ji4");
    List<Node> result = compositor.Walk().WalkedNodes;
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "高科技" });
    compositor.Cursor = 0;
    Assert.IsTrue(compositor.OverrideCandidateLiteral("膏", location: compositor.Cursor));
    result = compositor.Walk().WalkedNodes;
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "膏", "科技" });
    Assert.IsTrue(compositor.OverrideCandidateLiteral("高科技", location: 1));
    result = compositor.Walk().WalkedNodes;
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "高科技" });
    Assert.IsTrue(compositor.OverrideCandidateLiteral("膏", location: 0));
    result = compositor.Walk().WalkedNodes;
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "膏", "科技" });

    Assert.IsTrue(compositor.OverrideCandidateLiteral("柯", location: 1));
    result = compositor.Walk().WalkedNodes;
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "膏", "柯", "際" });

    Assert.IsTrue(compositor.OverrideCandidateLiteral("暨", location: 2));
    result = compositor.Walk().WalkedNodes;
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "膏", "柯", "暨" });

    Assert.IsTrue(compositor.OverrideCandidateLiteral("高科技", location: 3));
    result = compositor.Walk().WalkedNodes;
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "高科技" });
  }

  [Test]
  public void Test18_Compositor_OverrideReset() {
    Compositor compositor = new(
        new SimpleLM(input: StrSampleData + "zhong1jiang3 終講 -11.0\n" + "jiang3jin1 槳襟 -11.0\n"), separator: "");
    compositor.InsertKey("nian2");
    compositor.InsertKey("zhong1");
    compositor.InsertKey("jiang3");
    compositor.InsertKey("jin1");
    List<Node> result = compositor.Walk().WalkedNodes;
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "年中", "獎金" });

    Assert.IsTrue(compositor.OverrideCandidateLiteral("終講", location: 1));
    result = compositor.Walk().WalkedNodes;
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "年", "終講", "金" });

    Assert.IsTrue(compositor.OverrideCandidateLiteral("槳襟", location: 2));
    result = compositor.Walk().WalkedNodes;
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "年中", "槳襟" });

    Assert.IsTrue(compositor.OverrideCandidateLiteral("年終", location: 0));
    result = compositor.Walk().WalkedNodes;
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "年終", "槳襟" });
  }

  [Test]
  public void Test19_Compositor_CandidateDisambiguation() {
    Compositor compositor = new(langModel: new SimpleLM(input: StrEmojiSampleData), separator: "");
    compositor.InsertKey("gao1");
    compositor.InsertKey("re4");
    compositor.InsertKey("huo3");
    compositor.InsertKey("yan4");
    compositor.InsertKey("wei2");
    compositor.InsertKey("xian3");
    List<Node>? result = compositor.Walk().WalkedNodes;
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "高熱", "火焰", "危險" });

    Assert.IsTrue(compositor.OverrideCandidate(new(keyArray: new() { "huo3" }, value: "🔥"), location: 2));
    result = compositor.Walk().WalkedNodes;
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "高熱", "🔥", "焰", "危險" });

    Assert.IsTrue(compositor.OverrideCandidate(new(keyArray: new() { "huo3", "yan4" }, value: "🔥"), location: 2));
    result = compositor.Walk().WalkedNodes;
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "高熱", "🔥", "危險" });
  }

  [Test]
  public void Test20_Compositor_UpdateUnigramData() {
    SimpleLM theLM = new(input: StrSampleData);
    Compositor compositor = new(langModel: theLM, separator: "");
    compositor.InsertKey("nian2");
    compositor.InsertKey("zhong1");
    compositor.InsertKey("jiang3");
    compositor.InsertKey("jin1");
    string oldResult = compositor.Walk().WalkedNodes.Values().Joined();
    theLM.Trim(key: "nian2zhong1", value: "年中");
    compositor.Update(updateExisting: true);
    string newResult = compositor.Walk().WalkedNodes.Values().Joined();
    Assert.AreEqual(actual: new List<string> { oldResult, newResult },
                    expected: new List<string> { "年中獎金", "年終獎金" });
    compositor.Cursor = 4;
    compositor.DropKey(direction: Compositor.TypingDirection.ToRear);
    compositor.DropKey(direction: Compositor.TypingDirection.ToRear);
    theLM.Trim(key: "nian2zhong1", value: "年終");
    compositor.Update(updateExisting: true);
    string newResult2 = compositor.Walk().WalkedNodes.Values().Joined(separator: ",");
    Assert.AreEqual(actual: newResult2, expected: "年,中");
  }

  [Test]
  public void Test21_Compositor_HardCopy() {
    SimpleLM theLM = new(input: StrSampleData);
    string rawReadings = "gao1 ke1 ji4 gong1 si1 de5 nian2 zhong1 jiang3 jin1";
    Compositor compositorA = new(langModel: theLM, separator: "");
    foreach (string key in rawReadings.Split(separator: " ")) {
      compositorA.InsertKey(key);
    }
    Compositor compositorB = compositorA.HardCopy();
    List<Node> resultA = compositorA.Walk().WalkedNodes;
    List<Node> resultB = compositorB.Walk().WalkedNodes;
    Assert.True(resultA.SequenceEqual(resultB));
  }

  [Test]
  public void Test22_Compositor_SanitizingNodeCrossing() {
    SimpleLM theLM = new(input: StrSampleData);
    string rawReadings = "ke1 ke1";
    Compositor compositor = new(langModel: theLM, separator: "");
    foreach (string key in rawReadings.Split(separator: " ")) {
      compositor.InsertKey(key);
    }
    int a = compositor.FetchCandidatesAt(givenLocation: 1, filter: Compositor.CandidateFetchFilter.BeginAt)
                .Select(x => x.KeyArray.Count)
                .Max();
    int b = compositor.FetchCandidatesAt(givenLocation: 1, filter: Compositor.CandidateFetchFilter.EndAt)
                .Select(x => x.KeyArray.Count)
                .Max();
    int c = compositor.FetchCandidatesAt(givenLocation: 0, filter: Compositor.CandidateFetchFilter.BeginAt)
                .Select(x => x.KeyArray.Count)
                .Max();
    int d = compositor.FetchCandidatesAt(givenLocation: 2, filter: Compositor.CandidateFetchFilter.EndAt)
                .Select(x => x.KeyArray.Count)
                .Max();
    Assert.AreEqual(actual: $"{a} {b} {c} {d}", expected: "1 1 2 2");
    compositor.Cursor = compositor.Length;
    compositor.InsertKey("jin1");
    a = compositor.FetchCandidatesAt(givenLocation: 1, filter: Compositor.CandidateFetchFilter.BeginAt)
            .Select(x => x.KeyArray.Count)
            .Max();
    b = compositor.FetchCandidatesAt(givenLocation: 1, filter: Compositor.CandidateFetchFilter.EndAt)
            .Select(x => x.KeyArray.Count)
            .Max();
    c = compositor.FetchCandidatesAt(givenLocation: 0, filter: Compositor.CandidateFetchFilter.BeginAt)
            .Select(x => x.KeyArray.Count)
            .Max();
    d = compositor.FetchCandidatesAt(givenLocation: 2, filter: Compositor.CandidateFetchFilter.EndAt)
            .Select(x => x.KeyArray.Count)
            .Max();
    Assert.AreEqual(actual: $"{a} {b} {c} {d}", expected: "1 1 2 2");
  }

  [Test]
  public void Test23_Compositor_CheckGetCandidates() {
    SimpleLM theLM = new(input: StrSampleData);
    string rawReadings = "gao1 ke1 ji4 gong1 si1 de5 nian2 zhong1 jiang3 jin1";
    Compositor compositor = new(langModel: theLM, separator: "");
    foreach (string key in rawReadings.Split(separator: " ")) {
      compositor.InsertKey(key);
    }
    List<string> stack1A = new();
    List<string> stack1B = new();
    List<string> stack2A = new();
    List<string> stack2B = new();
    foreach (int i in new BRange(lowerbound: 0, upperbound: compositor.Keys.Count + 1)) {
      stack1A.Add(compositor.FetchCandidatesAt(i, Compositor.CandidateFetchFilter.BeginAt)
                      .Select(x => x.Value)
                      .Joined(separator: "-"));
      stack1B.Add(compositor.FetchCandidatesAt(i, Compositor.CandidateFetchFilter.EndAt)
                      .Select(x => x.Value)
                      .Joined(separator: "-"));
      stack2A.Add(compositor.FetchCandidatesDeprecatedAt(i, Compositor.CandidateFetchFilter.BeginAt)
                      .Select(x => x.Value)
                      .Joined(separator: "-"));
      stack2B.Add(compositor.FetchCandidatesDeprecatedAt(i, Compositor.CandidateFetchFilter.EndAt)
                      .Select(x => x.Value)
                      .Joined(separator: "-"));
    }
    stack1B.RemoveAt(0);
    stack2B.RemoveAt(stack2B.Count - 1);
    Assert.IsTrue(stack1A.SequenceEqual(stack2A));
    Assert.IsTrue(stack1B.SequenceEqual(stack2B));
  }
}
