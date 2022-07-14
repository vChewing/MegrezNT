// CSharpened by (c) 2022 and onwards The vChewing Project (MIT-NTL License).
// Rebranded from (c) Lukhnos Liu's C++ library "Gramambular" (MIT License).
/*
Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

1. The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

2. No trademark license is granted to use the trade names, trademarks, service
marks, or product names of Contributor, except as required to fulfill notice
requirements above.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Megrez.Tests;

public class MegrezTests : TestDataClass {
  [Test]
  public void Test01_SpanUnitInternalAbilities() {
    SimpleLM langModel = new(input: StrSampleData);
    SpanUnit span = new();
    Node n1 = new(key: "gao", unigrams: langModel.UnigramsFor(key: "gao1"));
    Node n3 = new(key: "gao1ke1ji4", unigrams: langModel.UnigramsFor(key: "gao1ke1ji4"));
    Assert.AreEqual(actual: span.MaxLength, expected: 0);
    span.Insert(node: n1, length: 1);
    Assert.AreEqual(actual: span.MaxLength, expected: 1);
    span.Insert(node: n3, length: 3);
    Assert.AreEqual(actual: span.MaxLength, expected: 3);
    Assert.AreEqual(actual: span.NodeOf(length: 1), expected: n1);
    Assert.AreEqual(actual: span.NodeOf(length: 2), expected: null);
    Assert.AreEqual(actual: span.NodeOf(length: 3), expected: n3);
    span.Clear();
    Assert.AreEqual(actual: span.MaxLength, expected: 0);
    Assert.AreEqual(actual: span.NodeOf(length: 1), expected: null);
    Assert.AreEqual(actual: span.NodeOf(length: 2), expected: null);
    Assert.AreEqual(actual: span.NodeOf(length: 3), expected: null);

    span.Insert(node: n1, length: 1);
    span.Insert(node: n3, length: 3);
    span.DropNodesBeyond(length: 1);
    Assert.AreEqual(actual: span.MaxLength, expected: 1);
    Assert.AreEqual(actual: span.NodeOf(length: 1), expected: n1);
    Assert.AreEqual(actual: span.NodeOf(length: 2), expected: null);
    Assert.AreEqual(actual: span.NodeOf(length: 3), expected: null);
    span.DropNodesBeyond(length: 0);
    Assert.AreEqual(actual: span.MaxLength, expected: 0);
    Assert.AreEqual(actual: span.NodeOf(length: 1), expected: null);
  }

  [Test]
  public void Test02_BasicFeaturesOfCompositor() {
    Compositor compositor = new(lm: new MockLM(), separator: "");
    Assert.AreEqual(actual: compositor.JoinSeparator, expected: "");
    Assert.AreEqual(actual: compositor.Cursor, expected: 0);
    Assert.AreEqual(actual: compositor.Length, expected: 0);

    Assert.IsTrue(compositor.InsertReading("a"));
    Assert.AreEqual(actual: compositor.Cursor, expected: 1);
    Assert.AreEqual(actual: compositor.Length, expected: 1);
    Assert.AreEqual(actual: compositor.Width, expected: 1);
    Assert.AreEqual(actual: compositor.Spans[0].MaxLength, expected: 1);
    if (compositor.Spans[0].NodeOf(length: 1) is not {} zeroNode) return;
    Assert.AreEqual(actual: zeroNode.Key, expected: "a");

    Assert.IsTrue(compositor.DropReading(direction: Compositor.TypingDirection.ToRear));
    Assert.AreEqual(actual: compositor.Cursor, expected: 0);
    Assert.AreEqual(actual: compositor.Cursor, expected: 0);
    Assert.AreEqual(actual: compositor.Width, expected: 0);
  }

  [Test]
  public void Test03_InvalidOperations() {
    Compositor compositor = new(lm: new TestLM(), separator: ";");
    Assert.IsFalse(compositor.InsertReading("bar"));
    Assert.IsFalse(compositor.InsertReading(""));
    Assert.IsFalse(compositor.InsertReading(""));
    Assert.IsFalse(compositor.DropReading(direction: Compositor.TypingDirection.ToRear));
    Assert.IsFalse(compositor.DropReading(direction: Compositor.TypingDirection.ToFront));

    compositor.InsertReading("foo");
    Assert.IsTrue(compositor.DropReading(direction: Compositor.TypingDirection.ToRear));
    Assert.AreEqual(actual: compositor.Length, expected: 0);
    compositor.InsertReading("foo");
    compositor.Cursor = 0;
    Assert.IsTrue(compositor.DropReading(direction: Compositor.TypingDirection.ToFront));
    Assert.AreEqual(actual: compositor.Length, expected: 0);
  }

  [Test]
  public void Test04_DeleteToTheFrontOfCursor() {
    Compositor compositor = new(lm: new MockLM());
    compositor.InsertReading("a");
    compositor.Cursor = 0;
    Assert.AreEqual(actual: compositor.Cursor, expected: 0);
    Assert.AreEqual(actual: compositor.Length, expected: 1);
    Assert.AreEqual(actual: compositor.Width, expected: 1);
    Assert.IsFalse(compositor.DropReading(direction: Compositor.TypingDirection.ToRear));
    Assert.AreEqual(actual: compositor.Cursor, expected: 0);
    Assert.AreEqual(actual: compositor.Length, expected: 1);
    Assert.IsTrue(compositor.DropReading(direction: Compositor.TypingDirection.ToFront));
    Assert.AreEqual(actual: compositor.Cursor, expected: 0);
    Assert.AreEqual(actual: compositor.Length, expected: 0);
    Assert.AreEqual(actual: compositor.Width, expected: 0);
  }

  [Test]
  public void Test05_MultipleSpanUnits() {
    Compositor compositor = new(lm: new MockLM(), separator: ";");
    compositor.InsertReading("a");
    compositor.InsertReading("b");
    compositor.InsertReading("c");
    Assert.AreEqual(actual: compositor.Cursor, expected: 3);
    Assert.AreEqual(actual: compositor.Length, expected: 3);
    Assert.AreEqual(actual: compositor.Width, expected: 3);
    Assert.AreEqual(actual: compositor.Spans[0].MaxLength, expected: 3);
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 1)?.Key, expected: "a");
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 2)?.Key, expected: "a;b");
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 3)?.Key, expected: "a;b;c");
    Assert.AreEqual(actual: compositor.Spans[1].MaxLength, expected: 2);
    Assert.AreEqual(actual: compositor.Spans[1].NodeOf(length: 1)?.Key, expected: "b");
    Assert.AreEqual(actual: compositor.Spans[1].NodeOf(length: 2)?.Key, expected: "b;c");
    Assert.AreEqual(actual: compositor.Spans[2].MaxLength, expected: 1);
    Assert.AreEqual(actual: compositor.Spans[2].NodeOf(length: 1)?.Key, expected: "c");
  }

  [Test]
  public void Test06_SpanUnitDeletionFromFront() {
    Compositor compositor = new(lm: new MockLM(), separator: ";");
    compositor.InsertReading("a");
    compositor.InsertReading("b");
    compositor.InsertReading("c");
    Assert.IsFalse(compositor.DropReading(direction: Compositor.TypingDirection.ToFront));
    Assert.IsTrue(compositor.DropReading(direction: Compositor.TypingDirection.ToRear));
    Assert.AreEqual(actual: compositor.Cursor, expected: 2);
    Assert.AreEqual(actual: compositor.Length, expected: 2);
    Assert.AreEqual(actual: compositor.Width, expected: 2);
    Assert.AreEqual(actual: compositor.Spans[0].MaxLength, expected: 2);
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 1)?.Key, expected: "a");
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 2)?.Key, expected: "a;b");
    Assert.AreEqual(actual: compositor.Spans[1].MaxLength, expected: 1);
    Assert.AreEqual(actual: compositor.Spans[1].NodeOf(length: 1)?.Key, expected: "b");
  }

  [Test]
  public void Test07_SpanUnitDeletionFromMiddle() {
    Compositor compositor = new(lm: new MockLM(), separator: ";");
    compositor.InsertReading("a");
    compositor.InsertReading("b");
    compositor.InsertReading("c");
    compositor.Cursor = 2;

    Assert.IsTrue(compositor.DropReading(direction: Compositor.TypingDirection.ToRear));
    Assert.AreEqual(actual: compositor.Cursor, expected: 1);
    Assert.AreEqual(actual: compositor.Length, expected: 2);
    Assert.AreEqual(actual: compositor.Width, expected: 2);
    Assert.AreEqual(actual: compositor.Spans[0].MaxLength, expected: 2);
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 1)?.Key, expected: "a");
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 2)?.Key, expected: "a;c");
    Assert.AreEqual(actual: compositor.Spans[1].MaxLength, expected: 1);
    Assert.AreEqual(actual: compositor.Spans[1].NodeOf(length: 1)?.Key, expected: "c");

    compositor.Clear();
    compositor.InsertReading("a");
    compositor.InsertReading("b");
    compositor.InsertReading("c");
    compositor.Cursor = 1;

    Assert.IsTrue(compositor.DropReading(direction: Compositor.TypingDirection.ToFront));
    Assert.AreEqual(actual: compositor.Cursor, expected: 1);
    Assert.AreEqual(actual: compositor.Length, expected: 2);
    Assert.AreEqual(actual: compositor.Width, expected: 2);
    Assert.AreEqual(actual: compositor.Spans[0].MaxLength, expected: 2);
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 1)?.Key, expected: "a");
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 2)?.Key, expected: "a;c");
    Assert.AreEqual(actual: compositor.Spans[1].MaxLength, expected: 1);
    Assert.AreEqual(actual: compositor.Spans[1].NodeOf(length: 1)?.Key, expected: "c");
  }

  [Test]
  public void Test08_SpanUnitDeletionFromRear() {
    Compositor compositor = new(lm: new MockLM(), separator: ";");
    compositor.InsertReading("a");
    compositor.InsertReading("b");
    compositor.InsertReading("c");
    compositor.Cursor = 0;

    Assert.IsFalse(compositor.DropReading(direction: Compositor.TypingDirection.ToRear));
    Assert.IsTrue(compositor.DropReading(direction: Compositor.TypingDirection.ToFront));
    Assert.AreEqual(actual: compositor.Cursor, expected: 0);
    Assert.AreEqual(actual: compositor.Length, expected: 2);
    Assert.AreEqual(actual: compositor.Width, expected: 2);
    Assert.AreEqual(actual: compositor.Spans[0].MaxLength, expected: 2);
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 1)?.Key, expected: "b");
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 2)?.Key, expected: "b;c");
    Assert.AreEqual(actual: compositor.Spans[1].MaxLength, expected: 1);
    Assert.AreEqual(actual: compositor.Spans[1].NodeOf(length: 1)?.Key, expected: "c");
  }

  [Test]
  public void Test09_SpanUnitInsertion() {
    Compositor compositor = new(lm: new MockLM(), separator: ";");
    compositor.InsertReading("a");
    compositor.InsertReading("b");
    compositor.InsertReading("c");
    compositor.Cursor = 1;
    compositor.InsertReading("X");

    Assert.AreEqual(actual: compositor.Cursor, expected: 2);
    Assert.AreEqual(actual: compositor.Length, expected: 4);
    Assert.AreEqual(actual: compositor.Width, expected: 4);
    Assert.AreEqual(actual: compositor.Spans[0].MaxLength, expected: 4);
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 1)?.Key, expected: "a");
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 2)?.Key, expected: "a;X");
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 3)?.Key, expected: "a;X;b");
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 4)?.Key, expected: "a;X;b;c");
    Assert.AreEqual(actual: compositor.Spans[1].MaxLength, expected: 3);
    Assert.AreEqual(actual: compositor.Spans[1].NodeOf(length: 1)?.Key, expected: "X");
    Assert.AreEqual(actual: compositor.Spans[1].NodeOf(length: 2)?.Key, expected: "X;b");
    Assert.AreEqual(actual: compositor.Spans[1].NodeOf(length: 3)?.Key, expected: "X;b;c");
    Assert.AreEqual(actual: compositor.Spans[2].MaxLength, expected: 2);
    Assert.AreEqual(actual: compositor.Spans[2].NodeOf(length: 1)?.Key, expected: "b");
    Assert.AreEqual(actual: compositor.Spans[2].NodeOf(length: 2)?.Key, expected: "b;c");
    Assert.AreEqual(actual: compositor.Spans[3].MaxLength, expected: 1);
    Assert.AreEqual(actual: compositor.Spans[3].NodeOf(length: 1)?.Key, expected: "c");
  }

  [Test]
  public void Test10_LongGridDeletion() {
    Compositor compositor = new(lm: new MockLM(), separator: "");
    compositor.InsertReading("a");
    compositor.InsertReading("b");
    compositor.InsertReading("c");
    compositor.InsertReading("d");
    compositor.InsertReading("e");
    compositor.InsertReading("f");
    compositor.InsertReading("g");
    compositor.InsertReading("h");
    compositor.InsertReading("i");
    compositor.InsertReading("j");
    compositor.InsertReading("k");
    compositor.InsertReading("l");
    compositor.InsertReading("m");
    compositor.InsertReading("n");
    compositor.Cursor = 7;
    Assert.IsTrue(compositor.DropReading(direction: Compositor.TypingDirection.ToRear));
    Assert.AreEqual(actual: compositor.Cursor, expected: 6);
    Assert.AreEqual(actual: compositor.Length, expected: 13);
    Assert.AreEqual(actual: compositor.Width, expected: 13);
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 6)?.Key, expected: "abcdef");
    Assert.AreEqual(actual: compositor.Spans[1].NodeOf(length: 6)?.Key, expected: "bcdefh");
    Assert.AreEqual(actual: compositor.Spans[1].NodeOf(length: 5)?.Key, expected: "bcdef");
    Assert.AreEqual(actual: compositor.Spans[2].NodeOf(length: 6)?.Key, expected: "cdefhi");
    Assert.AreEqual(actual: compositor.Spans[2].NodeOf(length: 5)?.Key, expected: "cdefh");
    Assert.AreEqual(actual: compositor.Spans[3].NodeOf(length: 6)?.Key, expected: "defhij");
    Assert.AreEqual(actual: compositor.Spans[4].NodeOf(length: 6)?.Key, expected: "efhijk");
    Assert.AreEqual(actual: compositor.Spans[5].NodeOf(length: 6)?.Key, expected: "fhijkl");
    Assert.AreEqual(actual: compositor.Spans[6].NodeOf(length: 6)?.Key, expected: "hijklm");
    Assert.AreEqual(actual: compositor.Spans[7].NodeOf(length: 6)?.Key, expected: "ijklmn");
    Assert.AreEqual(actual: compositor.Spans[8].NodeOf(length: 5)?.Key, expected: "jklmn");
  }

  [Test]
  public void Test11_LongGridInsertion() {
    Compositor compositor = new(lm: new MockLM(), separator: "");
    compositor.InsertReading("a");
    compositor.InsertReading("b");
    compositor.InsertReading("c");
    compositor.InsertReading("d");
    compositor.InsertReading("e");
    compositor.InsertReading("f");
    compositor.InsertReading("g");
    compositor.InsertReading("h");
    compositor.InsertReading("i");
    compositor.InsertReading("j");
    compositor.InsertReading("k");
    compositor.InsertReading("l");
    compositor.InsertReading("m");
    compositor.InsertReading("n");
    compositor.Cursor = 7;
    compositor.InsertReading("X");
    Assert.AreEqual(actual: compositor.Cursor, expected: 8);
    Assert.AreEqual(actual: compositor.Length, expected: 15);
    Assert.AreEqual(actual: compositor.Width, expected: 15);
    Assert.AreEqual(actual: compositor.Spans[0].NodeOf(length: 6)?.Key, expected: "abcdef");
    Assert.AreEqual(actual: compositor.Spans[1].NodeOf(length: 6)?.Key, expected: "bcdefg");
    Assert.AreEqual(actual: compositor.Spans[2].NodeOf(length: 6)?.Key, expected: "cdefgX");
    Assert.AreEqual(actual: compositor.Spans[3].NodeOf(length: 6)?.Key, expected: "defgXh");
    Assert.AreEqual(actual: compositor.Spans[3].NodeOf(length: 5)?.Key, expected: "defgX");
    Assert.AreEqual(actual: compositor.Spans[4].NodeOf(length: 6)?.Key, expected: "efgXhi");
    Assert.AreEqual(actual: compositor.Spans[4].NodeOf(length: 5)?.Key, expected: "efgXh");
    Assert.AreEqual(actual: compositor.Spans[4].NodeOf(length: 4)?.Key, expected: "efgX");
    Assert.AreEqual(actual: compositor.Spans[4].NodeOf(length: 3)?.Key, expected: "efg");
    Assert.AreEqual(actual: compositor.Spans[5].NodeOf(length: 6)?.Key, expected: "fgXhij");
    Assert.AreEqual(actual: compositor.Spans[6].NodeOf(length: 6)?.Key, expected: "gXhijk");
    Assert.AreEqual(actual: compositor.Spans[7].NodeOf(length: 6)?.Key, expected: "Xhijkl");
    Assert.AreEqual(actual: compositor.Spans[8].NodeOf(length: 6)?.Key, expected: "hijklm");
  }

  [Test]
  public void Test12_WordSegmentation() {
    Compositor compositor = new(lm: new SimpleLM(input: StrSampleData, swapKeyValue: true)) { JoinSeparator = "" };
    string testStr = "高科技公司的年終獎金";
    foreach (char c in testStr) compositor.InsertReading(c.ToString());
    Assert.AreEqual(actual: compositor.Walk().Keys(),
                    expected: new List<string> { "高科技", "公司", "的", "年終", "獎金" });
  }

  [Test]
  public void Test13_LanguageInputAndCursorJump() {
    Compositor compositor = new(lm: new SimpleLM(input: StrSampleData), separator: "");
    compositor.InsertReading("gao1");
    compositor.Walk();
    compositor.InsertReading("ji4");
    compositor.Walk();
    compositor.Cursor = 1;
    compositor.InsertReading("ke1");
    compositor.Walk();
    compositor.Cursor = 0;
    compositor.DropReading(direction: Compositor.TypingDirection.ToFront);
    compositor.InsertReading("gao1");
    compositor.Walk();
    compositor.Cursor = compositor.Length;
    compositor.InsertReading("gong1");
    compositor.Walk();
    compositor.InsertReading("si1");
    compositor.Walk();
    compositor.InsertReading("de5");
    compositor.Walk();
    compositor.InsertReading("nian2");
    compositor.Walk();
    compositor.InsertReading("zhong1");
    compositor.Walk();
    compositor.InsertReading("jiang3");
    compositor.Walk();
    compositor.InsertReading("jin1");
    Console.WriteLine("// Normal walk: Time test started.");
    DateTime startTime = DateTime.Now;
    compositor.Walk();
    TimeSpan timeElapsed = DateTime.Now - startTime;
    Console.WriteLine($"// Normal walk: Time test elapsed: {timeElapsed.TotalSeconds}s.");
    Assert.AreEqual(actual: compositor.WalkedAnchors.Values(),
                    expected: new List<string> { "高科技", "公司", "的", "年中", "獎金" });
    Assert.AreEqual(actual: compositor.Length, expected: 10);
    Assert.False(compositor.FixNodeWithCandidate(new(key: "nian2zhong1", value: "年終"), 7).IsEmpty);
    compositor.Cursor = 8;
    Assert.False(compositor.FixNodeWithCandidate(new(key: "nian2zhong1", value: "年終"), compositor.Cursor).IsEmpty);
    compositor.Walk();
    Assert.AreEqual(actual: compositor.WalkedAnchors.Values(),
                    expected: new List<string> { "高科技", "公司", "的", "年終", "獎金" });
    Assert.IsTrue(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToRear));
    Assert.AreEqual(actual: compositor.Cursor, expected: 6);
    Assert.IsTrue(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToRear));
    Assert.AreEqual(actual: compositor.Cursor, expected: 5);
    Assert.IsTrue(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToRear));
    Assert.AreEqual(actual: compositor.Cursor, expected: 3);
    Assert.IsTrue(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToRear));
    Assert.AreEqual(actual: compositor.Cursor, expected: 0);
    Assert.IsFalse(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToRear));
    Assert.AreEqual(actual: compositor.Cursor, expected: 0);
    Assert.IsTrue(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToFront));
    Assert.AreEqual(actual: compositor.Cursor, expected: 3);
    Assert.IsTrue(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToFront));
    Assert.AreEqual(actual: compositor.Cursor, expected: 5);
    Assert.IsTrue(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToFront));
    Assert.AreEqual(actual: compositor.Cursor, expected: 6);
    Assert.IsTrue(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToFront));
    Assert.AreEqual(actual: compositor.Cursor, expected: 8);
    Assert.IsTrue(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToFront));
    Assert.AreEqual(actual: compositor.Cursor, expected: 10);
    Assert.IsFalse(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToFront));
    Assert.AreEqual(actual: compositor.Cursor, expected: 10);
    compositor.Walk();
    Assert.AreEqual(actual: compositor.WalkedAnchors.Values(),
                    expected: new List<string> { "高科技", "公司", "的", "年終", "獎金" });
  }
  [Test]
  public void Test14_OverrideOverlappingNodes() {
    Compositor compositor = new(lm: new SimpleLM(input: StrSampleData)) { JoinSeparator = "" };
    compositor.InsertReading("gao1");
    compositor.InsertReading("ke1");
    compositor.InsertReading("ji4");
    compositor.Cursor = 1;
    compositor.FixNodeWithCandidateLiteral("膏", compositor.Cursor);
    List<NodeAnchor> result = compositor.Walk();
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "膏", "科技" });
    compositor.FixNodeWithCandidateLiteral("高科技", 2);
    result = compositor.Walk();
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "高科技" });
    compositor.FixNodeWithCandidateLiteral("膏", 1);
    result = compositor.Walk();
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "膏", "科技" });

    compositor.FixNodeWithCandidateLiteral("柯", 2);
    result = compositor.Walk();
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "膏", "柯", "際" });

    compositor.FixNodeWithCandidateLiteral("暨", 3);
    result = compositor.Walk();
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "膏", "柯", "暨" });

    compositor.FixNodeWithCandidateLiteral("高科技", 3);
    result = compositor.Walk();
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "高科技" });
  }
  [Test]
  public void Test15_OverrideReset() {
    string newSampleData = StrSampleData + "zhong1jiang3 終講 -11.0\n" + "jiang3jin1 槳襟 -11.0\n";
    Compositor compositor = new(lm: new SimpleLM(input: newSampleData), separator: "");
    compositor.InsertReading("nian2");
    compositor.InsertReading("zhong1");
    compositor.InsertReading("jiang3");
    compositor.InsertReading("jin1");
    compositor.Walk();
    Assert.AreEqual(actual: compositor.WalkedAnchors.Values(), expected: new List<string> { "年中", "獎金" });

    compositor.FixNodeWithCandidateLiteral("終講", 2);
    compositor.Walk();
    Assert.AreEqual(actual: compositor.WalkedAnchors.Values(), expected: new List<string> { "年", "終講", "金" });

    compositor.FixNodeWithCandidateLiteral("槳襟", 3);
    compositor.Walk();
    Assert.AreEqual(actual: compositor.WalkedAnchors.Values(), expected: new List<string> { "年中", "槳襟" });

    compositor.FixNodeWithCandidateLiteral("年終", 1);
    compositor.Walk();
    Assert.AreEqual(actual: compositor.WalkedAnchors.Values(), expected: new List<string> { "年終", "槳襟" });
  }
  [Test]
  public void Test16_CandidateDisambiguation() {
    Compositor compositor = new(lm: new SimpleLM(input: StrEmojiSampleData), separator: "");
    compositor.InsertReading("gao1");
    compositor.InsertReading("re4");
    compositor.InsertReading("huo3");
    compositor.InsertReading("yan4");
    compositor.InsertReading("wei2");
    compositor.InsertReading("xian3");
    compositor.InsertReading("mi4");
    compositor.InsertReading("feng1");
    List<NodeAnchor>? result = compositor.Walk();
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "高熱", "火焰", "危險", "蜜蜂" });

    compositor.FixNodeWithCandidate(new(key: "huo3", value: "🔥"), 3);
    result = compositor.Walk();
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "高熱", "🔥", "焰", "危險", "蜜蜂" });

    compositor.FixNodeWithCandidate(new(key: "huo3yan4", value: "🔥"), 4);
    result = compositor.Walk();
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "高熱", "🔥", "危險", "蜜蜂" });

    compositor.Cursor = compositor.Width;

    compositor.FixNodeWithCandidate(new(key: "mi4feng1", value: "🐝"), compositor.Cursor);
    result = compositor.Walk();
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "高熱", "🔥", "危險", "🐝" });

    compositor.FixNodeWithCandidate(new(key: "feng1", value: "🐝"), compositor.Cursor);
    result = compositor.Walk();
    Assert.AreEqual(actual: result.Values(), expected: new List<string> { "高熱", "🔥", "危險", "蜜", "🐝" });
  }

  [Test]
  public void Test17_StressBenchmark_MachineGun() {
    // 測試結果發現：只敲入完全雷同的某個漢字的話，想保證使用體驗就得讓一個組字區最多塞 20 字。
    Console.WriteLine("// Normal walk: Machine-Gun Stress test preparation begins.");
    Compositor compositor = new(lm: new SimpleLM(input: StrStressData));
    // 這個測試最多只能塞 20 字，否則會慢死。;
    for (int i = 0; i < 20; i += 1) compositor.InsertReading("yi1");
    Console.WriteLine("// Normal walk: Machine-Gun Stress test started.");
    DateTime startTime = DateTime.Now;
    compositor.Walk();
    TimeSpan timeElapsed = DateTime.Now - startTime;
    Console.WriteLine($"// Normal walk: Machine-Gun Stress test elapsed: {timeElapsed.TotalSeconds}s.");
  }

  [Test]
  public void Test18_StressBenchmark_SpeakLikeAHuman() {
    // C# 的測試結果與 Swift 不同，只敲入完全雷同的某個漢字時的處理速度反而更快。
    // 複雜輸入的話，一個組字區最多塞 20-30 字。不然很快就會遲鈍。
    Compositor compositor = new(lm: new SimpleLM(input: StrSampleData), separator: "");
    List<string> testMaterial =
        new() { "gao1", "ke1", "ji4", "gong1", "si1", "de5", "nian2", "zhong1", "jiang3", "jin1" };
    foreach (string neta in testMaterial) compositor.InsertReading(neta);
    foreach (string neta in testMaterial) compositor.InsertReading(neta);
    foreach (string neta in testMaterial) compositor.InsertReading(neta);
    Console.WriteLine("// Normal walk: Time test started.");
    DateTime startTime = DateTime.Now;
    compositor.Walk();
    TimeSpan timeElapsed = DateTime.Now - startTime;
    Console.WriteLine($"// Normal walk: Time test elapsed: {timeElapsed.TotalSeconds}s.");
  }
}
