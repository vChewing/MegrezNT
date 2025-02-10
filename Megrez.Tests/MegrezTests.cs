// (c) 2022 and onwards The vChewing Project (LGPL v3.0 License or later).
// ====================
// This code is released under the SPDX-License-Identifier: `LGPL-3.0-or-later`.

using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Megrez.Tests
{
  [TestFixture]
  public class MegrezTestsBasic
  {
    [Test]
    public void Test01_SpanOperations()
    {
      SimpleLM langModel = new(TestDataClass.StrLMSampleDataLitch);
      Compositor.SpanUnit span = new();
      Node n1 = new(
          new List<string> { "da4" }, 1, langModel.UnigramsFor(new List<string> { "da4" })
      );
      Node n3 = new(
          new List<string> { "da4", "qian2", "tian1" }, 3,
          langModel.UnigramsFor(new List<string> { "da4-qian2-tian1" })
      );

      Assert.That(span.MaxLength, Is.EqualTo(0));
      span.Nodes[n1.SpanLength] = n1;
      Assert.That(span.MaxLength, Is.EqualTo(1));
      span.Nodes[n3.SpanLength] = n3;
      Assert.That(span.MaxLength, Is.EqualTo(3));
      Assert.That(span.NodeOf(1), Is.EqualTo(n1));
      Assert.That(span.NodeOf(2), Is.Null);
      Assert.That(span.NodeOf(3), Is.EqualTo(n3));
      span.Clear();
      Assert.That(span.MaxLength, Is.EqualTo(0));
      Assert.That(span.NodeOf(1), Is.Null);
      Assert.That(span.NodeOf(2), Is.Null);
      Assert.That(span.NodeOf(3), Is.Null);
    }

    [Test]
    public void Test02_Compositor_BasicSpanNodeGramInsertion()
    {
      Compositor compositor = new(new MockLM());
      Assert.That(compositor.Separator, Is.EqualTo(Compositor.TheSeparator));
      Assert.That(compositor.Cursor, Is.EqualTo(0));
      Assert.That(compositor.Length, Is.EqualTo(0));

      compositor.InsertKey("s");
      Assert.That(compositor.Cursor, Is.EqualTo(1));
      Assert.That(compositor.Length, Is.EqualTo(1));
      Assert.That(compositor.Spans.Count, Is.EqualTo(1));
      Assert.That(compositor.Spans[0].MaxLength, Is.EqualTo(1));
      Assert.That(compositor.Spans[0].Nodes[1].KeyArray, Is.EqualTo(new[] { "s" }));
      compositor.DropKey(Compositor.TypingDirection.ToRear);
      Assert.That(compositor.Cursor, Is.EqualTo(0));
      Assert.That(compositor.Length, Is.EqualTo(0));
      Assert.That(compositor.Spans.Count, Is.EqualTo(0));
    }

    [Test]
    public void Test03_Compositor_DefendingInvalidOps()
    {
      SimpleLM mockLM = new("ping2 ping2 -1");
      Compositor compositor = new(mockLM);
      compositor.Separator = ";";
      Assert.That(compositor.InsertKey("guo3"), Is.False);
      Assert.That(compositor.InsertKey(""), Is.False);
      Assert.That(compositor.InsertKey(""), Is.False);
      CompositorConfig configAlpha = compositor.Config;
      Assert.That(compositor.DropKey(Compositor.TypingDirection.ToRear), Is.False);
      Assert.That(compositor.DropKey(Compositor.TypingDirection.ToFront), Is.False);
      CompositorConfig configBravo = compositor.Config;
      Assert.That(configAlpha, Is.EqualTo(configBravo));
      Assert.That(compositor.InsertKey("ping2"), Is.True);
      Assert.That(compositor.DropKey(Compositor.TypingDirection.ToRear), Is.True);
      Assert.That(compositor.Length, Is.EqualTo(0));
      Assert.That(compositor.InsertKey("ping2"), Is.True);
      compositor.Cursor = 0;
      Assert.That(compositor.DropKey(Compositor.TypingDirection.ToFront), Is.True);
      Assert.That(compositor.Length, Is.EqualTo(0));
    }

    [Test]
    public void Test04_Compositor_SpansAcrossPositions()
    {
      Compositor compositor = new(new MockLM());
      compositor.Separator = ";";
      compositor.InsertKey("h");
      compositor.InsertKey("o");
      compositor.InsertKey("g");
      Assert.That((compositor.Cursor, compositor.Length), Is.EqualTo((3, 3)));
      Assert.That((compositor.Spans.Count), Is.EqualTo(3));
      Assert.That(compositor.Spans[0].MaxLength, Is.EqualTo(3));
      Assert.That(compositor.Spans[0].Nodes[1].KeyArray.SequenceEqual(new[] { "h" }), Is.True);
      Assert.That(compositor.Spans[0].Nodes[2].KeyArray.SequenceEqual(new[] { "h", "o" }), Is.True);
      Assert.That(compositor.Spans[0].Nodes[3].KeyArray.SequenceEqual(new[] { "h", "o", "g" }), Is.True);
      Assert.That(compositor.Spans[1].MaxLength, Is.EqualTo(2));
      Assert.That(compositor.Spans[1].Nodes[1].KeyArray.SequenceEqual(new[] { "o" }), Is.True);
      Assert.That(compositor.Spans[1].Nodes[2].KeyArray.SequenceEqual(new[] { "o", "g" }), Is.True);
      Assert.That(compositor.Spans[2].MaxLength, Is.EqualTo(1));
      Assert.That(compositor.Spans[2].Nodes[1].KeyArray.SequenceEqual(new[] { "g" }), Is.True);
    }

    [Test]
    public void Test05_Compositor_KeyAndSpanDeletionInAllDirections()
    {
      Compositor compositor = new(new MockLM());
      compositor.InsertKey("a");
      compositor.Cursor = 0;
      Assert.That(compositor.Cursor, Is.EqualTo(0));
      Assert.That(compositor.Length, Is.EqualTo(1));
      Assert.That(compositor.Spans.Count, Is.EqualTo(1));
      Assert.That(compositor.DropKey(Compositor.TypingDirection.ToRear), Is.False);
      Assert.That(compositor.Cursor, Is.EqualTo(0));
      Assert.That(compositor.Length, Is.EqualTo(1));
      Assert.That(compositor.Spans.Count, Is.EqualTo(1));
      Assert.That(compositor.DropKey(Compositor.TypingDirection.ToFront), Is.True);
      Assert.That(compositor.Cursor, Is.EqualTo(0));
      Assert.That(compositor.Length, Is.EqualTo(0));
      Assert.That(compositor.Spans.Count, Is.EqualTo(0));

      void ResetCompositorForTests()
      {
        compositor.Clear();
        compositor.InsertKey("h");
        compositor.InsertKey("o");
        compositor.InsertKey("g");
      }

      // 測試對幅位的刪除行為所產生的影響（從最前端開始往後方刪除）。
      {
        ResetCompositorForTests();
        Assert.That(compositor.DropKey(Compositor.TypingDirection.ToFront), Is.False);
        Assert.That(compositor.DropKey(Compositor.TypingDirection.ToRear), Is.True);
        Assert.That((compositor.Cursor, compositor.Length), Is.EqualTo((2, 2)));
        Assert.That((compositor.Spans.Count), Is.EqualTo(2));
        Assert.That(compositor.Spans[0].MaxLength, Is.EqualTo(2));
        Assert.That(compositor.Spans[0].Nodes[1].KeyArray.SequenceEqual(new[] { "h" }), Is.True);
        Assert.That(compositor.Spans[0].Nodes[2].KeyArray.SequenceEqual(new[] { "h", "o" }), Is.True);
        Assert.That(compositor.Spans[1].MaxLength, Is.EqualTo(1));
        Assert.That(compositor.Spans[1].Nodes[1].KeyArray.SequenceEqual(new[] { "o" }), Is.True);
      }

      // 測試對幅位的刪除行為所產生的影響（從最後端開始往前方刪除）。
      {
        ResetCompositorForTests();
        compositor.Cursor = 0;
        Assert.That(compositor.DropKey(Compositor.TypingDirection.ToRear), Is.False);
        Assert.That(compositor.DropKey(Compositor.TypingDirection.ToFront), Is.True);
        Assert.That((compositor.Cursor, compositor.Length), Is.EqualTo((0, 2)));
        Assert.That((compositor.Spans.Count), Is.EqualTo(2));
        Assert.That(compositor.Spans[0].MaxLength, Is.EqualTo(2));
        Assert.That(compositor.Spans[0].Nodes[1].KeyArray.SequenceEqual(new[] { "o" }), Is.True);
        Assert.That(compositor.Spans[0].Nodes[2].KeyArray.SequenceEqual(new[] { "o", "g" }), Is.True);
        Assert.That(compositor.Spans[1].MaxLength, Is.EqualTo(1));
        Assert.That(compositor.Spans[1].Nodes[1].KeyArray.SequenceEqual(new[] { "g" }), Is.True);
      }

      // 測試對幅位的刪除行為所產生的影響（從中間開始往後方刪除）。
      {
        ResetCompositorForTests();
        compositor.Cursor = 2;
        Assert.That(compositor.DropKey(Compositor.TypingDirection.ToRear), Is.True);
        Assert.That((compositor.Cursor, compositor.Length), Is.EqualTo((1, 2)));
        Assert.That((compositor.Spans.Count), Is.EqualTo(2));
        Assert.That(compositor.Spans[0].MaxLength, Is.EqualTo(2));
        Assert.That(compositor.Spans[0].Nodes[1].KeyArray.SequenceEqual(new[] { "h" }), Is.True);
        Assert.That(compositor.Spans[0].Nodes[2].KeyArray.SequenceEqual(new[] { "h", "g" }), Is.True);
        Assert.That(compositor.Spans[1].MaxLength, Is.EqualTo(1));
        Assert.That(compositor.Spans[1].Nodes[1].KeyArray.SequenceEqual(new[] { "g" }), Is.True);
      }

      // 測試對幅位的刪除行為所產生的影響（從中間開始往前方刪除）。
      {
        CompositorConfig snapshot = compositor.Config;
        ResetCompositorForTests();
        compositor.Cursor = 1;
        Assert.That(compositor.DropKey(Compositor.TypingDirection.ToFront), Is.True);
        Assert.That((compositor.Cursor, compositor.Length), Is.EqualTo((1, 2)));
        Assert.That((compositor.Spans.Count), Is.EqualTo(2));
        Assert.That(compositor.Spans[0].MaxLength, Is.EqualTo(2));
        Assert.That(compositor.Spans[0].Nodes[1].KeyArray.SequenceEqual(new[] { "h" }), Is.True);
        Assert.That(compositor.Spans[0].Nodes[2].KeyArray.SequenceEqual(new[] { "h", "g" }), Is.True);
        Assert.That(compositor.Spans[1].MaxLength, Is.EqualTo(1));
        Assert.That(compositor.Spans[1].Nodes[1].KeyArray.SequenceEqual(new[] { "g" }), Is.True);
        Assert.That(snapshot, Is.EqualTo(compositor.Config));
      }
    }

    [Test]
    public void Test06_Compositor_SpanInsertion()
    {
      Compositor compositor = new(new MockLM());
      compositor.InsertKey("是");
      compositor.InsertKey("學");
      compositor.InsertKey("生");
      compositor.Cursor = 1;
      compositor.InsertKey("大");
      Assert.That((compositor.Cursor, compositor.Length), Is.EqualTo((2, 4)));
      Assert.That(compositor.Spans.Count, Is.EqualTo(4));
      Assert.That(compositor.Spans[0].MaxLength, Is.EqualTo(4));
      Assert.That(compositor.Spans[0].Nodes[1].KeyArray.SequenceEqual(new[] { "是" }), Is.True);
      Assert.That(compositor.Spans[0].Nodes[2].KeyArray.SequenceEqual(new[] { "是", "大" }), Is.True);
      Assert.That(compositor.Spans[0].Nodes[3].KeyArray.SequenceEqual(new[] { "是", "大", "學" }), Is.True);
      Assert.That(compositor.Spans[0].Nodes[4].KeyArray.SequenceEqual(new[] { "是", "大", "學", "生" }), Is.True);
      Assert.That(compositor.Spans[1].MaxLength, Is.EqualTo(3));
      Assert.That(compositor.Spans[1].Nodes[1].KeyArray.SequenceEqual(new[] { "大" }), Is.True);
      Assert.That(compositor.Spans[1].Nodes[2].KeyArray.SequenceEqual(new[] { "大", "學" }), Is.True);
      Assert.That(compositor.Spans[1].Nodes[3].KeyArray.SequenceEqual(new[] { "大", "學", "生" }), Is.True);
      Assert.That(compositor.Spans[2].MaxLength, Is.EqualTo(2));
      Assert.That(compositor.Spans[2].Nodes[1].KeyArray.SequenceEqual(new[] { "學" }), Is.True);
      Assert.That(compositor.Spans[2].Nodes[2].KeyArray.SequenceEqual(new[] { "學", "生" }), Is.True);
      Assert.That(compositor.Spans[3].MaxLength, Is.EqualTo(1));
      Assert.That(compositor.Spans[3].Nodes[1].KeyArray.SequenceEqual(new[] { "生" }), Is.True);
    }

    [Test]
    public void Test07_Compositor_LongGridDeletionAndInsertion()
    {
      Compositor compositor = new(new MockLM());
      foreach (char key in "無可奈何花作香幽蝶能留一縷芳")
      {
        compositor.InsertKey(key.ToString());
      }
      {
        compositor.Cursor = 8;
        Assert.That(compositor.DropKey(Compositor.TypingDirection.ToRear), Is.True);
        Assert.That((compositor.Cursor, compositor.Length), Is.EqualTo((7, 13)));
        Assert.That(compositor.Spans.Count, Is.EqualTo(13));
        Assert.That(compositor.Spans[0].Nodes[5].KeyArray.SequenceEqual(new[] { "無", "可", "奈", "何", "花" }), Is.True);
        Assert.That(compositor.Spans[1].Nodes[5].KeyArray.SequenceEqual(new[] { "可", "奈", "何", "花", "作" }), Is.True);
        Assert.That(compositor.Spans[2].Nodes[5].KeyArray.SequenceEqual(new[] { "奈", "何", "花", "作", "香" }), Is.True);
        Assert.That(compositor.Spans[3].Nodes[5].KeyArray.SequenceEqual(new[] { "何", "花", "作", "香", "蝶" }), Is.True);
        Assert.That(compositor.Spans[4].Nodes[5].KeyArray.SequenceEqual(new[] { "花", "作", "香", "蝶", "能" }), Is.True);
        Assert.That(compositor.Spans[5].Nodes[5].KeyArray.SequenceEqual(new[] { "作", "香", "蝶", "能", "留" }), Is.True);
        Assert.That(compositor.Spans[6].Nodes[5].KeyArray.SequenceEqual(new[] { "香", "蝶", "能", "留", "一" }), Is.True);
        Assert.That(compositor.Spans[7].Nodes[5].KeyArray.SequenceEqual(new[] { "蝶", "能", "留", "一", "縷" }), Is.True);
        Assert.That(compositor.Spans[8].Nodes[5].KeyArray.SequenceEqual(new[] { "能", "留", "一", "縷", "芳" }), Is.True);
      }
      {
        Assert.That(compositor.InsertKey("幽"), Is.True);
        Assert.That((compositor.Cursor, compositor.Length), Is.EqualTo((8, 14)));
        Assert.That(compositor.Spans.Count, Is.EqualTo(14));
        Assert.That(compositor.Spans[0].Nodes[6].KeyArray.SequenceEqual(new[] { "無", "可", "奈", "何", "花", "作" }), Is.True);
        Assert.That(compositor.Spans[1].Nodes[6].KeyArray.SequenceEqual(new[] { "可", "奈", "何", "花", "作", "香" }), Is.True);
        Assert.That(compositor.Spans[2].Nodes[6].KeyArray.SequenceEqual(new[] { "奈", "何", "花", "作", "香", "幽" }), Is.True);
        Assert.That(compositor.Spans[3].Nodes[6].KeyArray.SequenceEqual(new[] { "何", "花", "作", "香", "幽", "蝶" }), Is.True);
        Assert.That(compositor.Spans[4].Nodes[6].KeyArray.SequenceEqual(new[] { "花", "作", "香", "幽", "蝶", "能" }), Is.True);
        Assert.That(compositor.Spans[5].Nodes[6].KeyArray.SequenceEqual(new[] { "作", "香", "幽", "蝶", "能", "留" }), Is.True);
        Assert.That(compositor.Spans[6].Nodes[6].KeyArray.SequenceEqual(new[] { "香", "幽", "蝶", "能", "留", "一" }), Is.True);
        Assert.That(compositor.Spans[7].Nodes[6].KeyArray.SequenceEqual(new[] { "幽", "蝶", "能", "留", "一", "縷" }), Is.True);
        Assert.That(compositor.Spans[8].Nodes[6].KeyArray.SequenceEqual(new[] { "蝶", "能", "留", "一", "縷", "芳" }), Is.True);
      }
    }
  }

  [TestFixture]
  public class MegrezTestsAdvanced
  {
    [Test]
    public void Test08_WordSegmentation()
    {
      string regexPattern = ".* 能留 .*\n";
      string rawData = System.Text.RegularExpressions.Regex.Replace(
        TestDataClass.StrLMSampleDataHutao,
        regexPattern,
        ""
      );

      Compositor compositor = new(
        new SimpleLM(rawData, true, ""),
        ""
      );

      foreach (char c in "幽蝶能留一縷芳")
      {
        compositor.InsertKey(c.ToString());
      }

      List<Node> result = compositor.Walk();

      Assert.That(result.JoinedKeys(separator: "").SequenceEqual(
        new[] { "幽蝶", "能", "留", "一縷", "芳" }), Is.True
      );

      Compositor hardCopy = compositor.Copy();
      Assert.That(hardCopy.Config, Is.EqualTo(compositor.Config));
    }

    [Test]
    public void Test09_Compositor_StressBench()
    {
      Console.WriteLine("// Stress test preparation begins.");
      Compositor compositor = new(new SimpleLM(TestDataClass.StrLMStressData));
      for (int i = 0; i < 1919; i++)
      {
        compositor.InsertKey("sheng1");
      }
      Console.WriteLine("// Stress test started.");
      DateTime startTime = DateTime.Now;
      compositor.Walk();
      TimeSpan timeElapsed = DateTime.Now - startTime;
      Console.WriteLine($"// Stress test elapsed: {timeElapsed.TotalSeconds}s.");
    }

    [Test]
    public void Test10_Compositor_UpdateUnigramData()
    {
      string[] readings = "shu4 xin1 feng1".Split(' ');
      string newRawStringLM = TestDataClass.StrLMSampleDataEmoji + "\nshu4-xin1-feng1 樹新風 -9\n";
      System.Text.RegularExpressions.Regex regexToFilter = new(".*(樹|新|風) .*");
      SimpleLM lm = new(regexToFilter.Replace(newRawStringLM, ""));
      Compositor compositor = new(lm);
      foreach (string key in readings)
      {
        Assert.That(compositor.InsertKey(key), Is.True);
      }
      Console.WriteLine(string.Join(", ", compositor.Keys));
      List<string> oldResult = compositor.Walk().Values();
      CollectionAssert.AreEqual(new[] { "樹心", "封" }, oldResult);
      lm.ReConstruct(newRawStringLM);
      compositor.Update(true);
      List<string> newResult = compositor.Walk().Values();
      CollectionAssert.AreEqual(new[] { "樹新風" }, newResult);
    }

    [Test]
    public void Test11_Compositor_VerifyCandidateFetchResultsWithNewAPI()
    {
      SimpleLM theLM = new(TestDataClass.StrLMSampleDataTechGuarden + "\n" + TestDataClass.StrLMSampleDataLitch);
      string rawReadings = "da4 qian2 tian1 zai5 ke1 ji4 gong1 yuan2 chao1 shang1";
      Compositor compositor = new(theLM);
      foreach (string key in rawReadings.Split(' '))
      {
        compositor.InsertKey(key);
      }
      List<string> stack1A = new();
      List<string> stack1B = new();
      List<string> stack2A = new();
      List<string> stack2B = new();
      for (int i = 0; i <= compositor.Keys.Count; i++)
      {
        stack1A.Add(string.Join("-", compositor.FetchCandidatesAt(i, Compositor.CandidateFetchFilter.BeginAt).Select(c => c.Value)));
        stack1B.Add(string.Join("-", compositor.FetchCandidatesAt(i, Compositor.CandidateFetchFilter.EndAt).Select(c => c.Value)));
        stack2A.Add(string.Join("-", compositor.FetchCandidatesDeprecatedAt(i, Compositor.CandidateFetchFilter.BeginAt).Select(c => c.Value)));
        stack2B.Add(string.Join("-", compositor.FetchCandidatesDeprecatedAt(i, Compositor.CandidateFetchFilter.EndAt).Select(c => c.Value)));
      }
      stack1B.RemoveAt(0);
      stack2B.RemoveAt(stack2B.Count - 1);
      CollectionAssert.AreEqual(stack1A, stack2A);
      CollectionAssert.AreEqual(stack1B, stack2B);
    }

    [Test]
    public void Test12_Compositor_FilteringOutCandidatesAcrossingTheCursor()
    {
      // 一號測試。
      {
        string[] readings = "ke1 ji4 gong1 yuan2".Split(' ');
        SimpleLM mockLM = new(TestDataClass.StrLMSampleDataTechGuarden);
        Compositor compositor = new(mockLM);
        foreach (string key in readings)
        {
          compositor.InsertKey(key);
        }
        // 初始爬軌結果。
        List<string> assembledSentence = compositor.Walk().Values().ToList();
        CollectionAssert.AreEqual(new[] { "科技", "公園" }, assembledSentence);
        // 測試候選字詞過濾。
        List<string> gotBeginAt = compositor.FetchCandidatesAt(2, Compositor.CandidateFetchFilter.BeginAt).Select(c => c.Value).ToList();
        List<string> gotEndAt = compositor.FetchCandidatesAt(2, Compositor.CandidateFetchFilter.EndAt).Select(c => c.Value).ToList();
        Assert.That(gotBeginAt.Contains("濟公"), Is.False);
        Assert.That(gotBeginAt.Contains("公園"), Is.True);
        Assert.That(gotEndAt.Contains("公園"), Is.False);
        Assert.That(gotEndAt.Contains("科技"), Is.True);
      }
      // 二號測試。
      {
        string[] readings = "sheng1 sheng1".Split(' ');
        SimpleLM mockLM = new(TestDataClass.StrLMStressData + "\n" + TestDataClass.StrLMSampleDataHutao);
        Compositor compositor = new(mockLM);
        foreach (string key in readings)
        {
          compositor.InsertKey(key);
        }
        int a = compositor.FetchCandidatesAt(1, Compositor.CandidateFetchFilter.BeginAt).Select(c => c.KeyArray.Count).Max();
        int b = compositor.FetchCandidatesAt(1, Compositor.CandidateFetchFilter.EndAt).Select(c => c.KeyArray.Count).Max();
        int c = compositor.FetchCandidatesAt(0, Compositor.CandidateFetchFilter.BeginAt).Select(c => c.KeyArray.Count).Max();
        int d = compositor.FetchCandidatesAt(2, Compositor.CandidateFetchFilter.EndAt).Select(c => c.KeyArray.Count).Max();
        Assert.That($"{a} {b} {c} {d}", Is.EqualTo("1 1 2 2"));
        compositor.Cursor = compositor.Length;
        compositor.InsertKey("jin1");
        a = compositor.FetchCandidatesAt(1, Compositor.CandidateFetchFilter.BeginAt).Select(c => c.KeyArray.Count).Max();
        b = compositor.FetchCandidatesAt(1, Compositor.CandidateFetchFilter.EndAt).Select(c => c.KeyArray.Count).Max();
        c = compositor.FetchCandidatesAt(0, Compositor.CandidateFetchFilter.BeginAt).Select(c => c.KeyArray.Count).Max();
        d = compositor.FetchCandidatesAt(2, Compositor.CandidateFetchFilter.EndAt).Select(c => c.KeyArray.Count).Max();
        Assert.That($"{a} {b} {c} {d}", Is.EqualTo("1 1 2 2"));
      }
    }

    [Test]
    public void Test13_Compositor_WalkAndOverrideWithUnigramAndCursorJump()
    {
      string readings = "chao1 shang1 da4 qian2 tian1 wei2 zhi3 hai2 zai5 mai4 nai3 ji1";
      SimpleLM mockLM = new(TestDataClass.StrLMSampleDataLitch);
      Compositor compositor = new(mockLM);
      foreach (string key in readings.Split(' '))
      {
        compositor.InsertKey(key);
      }
      Assert.That(compositor.Length, Is.EqualTo(12));
      Assert.That(compositor.Length, Is.EqualTo(compositor.Cursor));
      // 初始爬軌結果。
      List<string> assembledSentence = compositor.Walk().Values().ToList();
      CollectionAssert.AreEqual(new[] { "超商", "大前天", "為止", "還", "在", "賣", "荔枝" }, assembledSentence);
      // 測試 DumpDOT。
      string expectedDumpDOT = @"
digraph {
graph [ rankdir=LR ];
BOS;
BOS -> 超;
超;
超 -> 傷;
BOS -> 超商;
超商;
超商 -> 大;
超商 -> 大錢;
超商 -> 大前天;
傷;
傷 -> 大;
傷 -> 大錢;
傷 -> 大前天;
大;
大 -> 前;
大 -> 前天;
大錢;
大錢 -> 添;
大前天;
大前天 -> 為;
大前天 -> 為止;
前;
前 -> 添;
前天;
前天 -> 為;
前天 -> 為止;
添;
添 -> 為;
添 -> 為止;
為;
為 -> 指;
為止;
為止 -> 還;
指;
指 -> 還;
還;
還 -> 在;
在;
在 -> 賣;
賣;
賣 -> 乃;
賣 -> 荔枝;
乃;
乃 -> 雞;
荔枝;
荔枝 -> EOS;
雞;
雞 -> EOS;
EOS;
}
";
      string actualDumpDOT = compositor.DumpDOT();
      Assert.That(expectedDumpDOT.Trim(), Is.EqualTo(actualDumpDOT.Trim()));
      // 單獨測試對最前方的讀音的覆寫。
      {
        Compositor compositorCopy1 = compositor.Copy();
        Assert.That(
            compositorCopy1.OverrideCandidate(new(new List<string> { "ji1" }, "雞"), 11), Is.True
        );
        assembledSentence = compositorCopy1.Walk().Values().ToList();
        CollectionAssert.AreEqual(new[] { "超商", "大前天", "為止", "還", "在", "賣", "乃", "雞" }, assembledSentence);
      }
      // 回到先前的測試，測試對整個詞的覆寫。
      Assert.That(
          compositor.OverrideCandidate(new(new List<string> { "nai3", "ji1" }, "奶雞"), 10), Is.True
      );
      assembledSentence = compositor.Walk().Values().ToList();
      CollectionAssert.AreEqual(new[] { "超商", "大前天", "為止", "還", "在", "賣", "奶雞" }, assembledSentence);
      // 測試游標跳轉。
      compositor.Cursor = 10; // 向後
      Assert.That(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToRear), Is.True);
      Assert.That(compositor.Cursor, Is.EqualTo(9));
      Assert.That(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToRear), Is.True);
      Assert.That(compositor.Cursor, Is.EqualTo(8));
      Assert.That(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToRear), Is.True);
      Assert.That(compositor.Cursor, Is.EqualTo(7));
      Assert.That(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToRear), Is.True);
      Assert.That(compositor.Cursor, Is.EqualTo(5));
      Assert.That(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToRear), Is.True);
      Assert.That(compositor.Cursor, Is.EqualTo(2));
      Assert.That(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToRear), Is.True);
      Assert.That(compositor.Cursor, Is.EqualTo(0));
      Assert.That(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToRear), Is.False);
      Assert.That(compositor.Cursor, Is.EqualTo(0)); // 接下來準備向前
      Assert.That(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToFront), Is.True);
      Assert.That(compositor.Cursor, Is.EqualTo(2));
      Assert.That(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToFront), Is.True);
      Assert.That(compositor.Cursor, Is.EqualTo(5));
      Assert.That(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToFront), Is.True);
      Assert.That(compositor.Cursor, Is.EqualTo(7));
      Assert.That(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToFront), Is.True);
      Assert.That(compositor.Cursor, Is.EqualTo(8));
      Assert.That(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToFront), Is.True);
      Assert.That(compositor.Cursor, Is.EqualTo(9));
      Assert.That(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToFront), Is.True);
      Assert.That(compositor.Cursor, Is.EqualTo(10));
      Assert.That(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToFront), Is.True);
      Assert.That(compositor.Cursor, Is.EqualTo(12));
      Assert.That(compositor.JumpCursorBySpan(Compositor.TypingDirection.ToFront), Is.False);
      Assert.That(compositor.Cursor, Is.EqualTo(12));
    }

    [Test]
    public void Test14_Compositor_WalkAndOverride_AnotherTest()
    {
      string[] readings = "you1 die2 neng2 liu2 yi4 lv3 fang1".Split(' ');
      SimpleLM lm = new(TestDataClass.StrLMSampleDataHutao);
      Compositor compositor = new(lm);
      foreach (string key in readings)
      {
        compositor.InsertKey(key);
      }
      // 初始爬軌結果。
      List<string> assembledSentence = compositor.Walk().Values().ToList();
      CollectionAssert.AreEqual(new[] { "幽蝶", "能", "留意", "呂方" }, assembledSentence);
      // 測試覆寫「留」以試圖打斷「留意」。
      compositor.OverrideCandidate(
          new KeyValuePaired(new List<string> { "liu2" }, "留"), 3, Megrez.Node.OverrideType.HighScore
      );
      // 測試覆寫「一縷」以打斷「留意」與「呂方」。
      compositor.OverrideCandidate(
          new KeyValuePaired(new List<string> { "yi4", "lv3" }, "一縷"), 4, Megrez.Node.OverrideType.HighScore
      );
      assembledSentence = compositor.Walk().Values().ToList();
      CollectionAssert.AreEqual(new[] { "幽蝶", "能", "留", "一縷", "方" }, assembledSentence);
      // 對位置 7 這個最前方的座標位置使用節點覆寫。會在此過程中自動糾正成對位置 6 的覆寫。
      compositor.OverrideCandidate(
          new KeyValuePaired(new List<string> { "fang1" }, "芳"), 7, Megrez.Node.OverrideType.HighScore
      );
      assembledSentence = compositor.Walk().Values().ToList();
      CollectionAssert.AreEqual(new[] { "幽蝶", "能", "留", "一縷", "芳" }, assembledSentence);
      string expectedDOT = @"
digraph {
graph [ rankdir=LR ];
BOS;
BOS -> 優;
優;
優 -> 跌;
BOS -> 幽蝶;
幽蝶;
幽蝶 -> 能;
幽蝶 -> 能留;
跌;
跌 -> 能;
跌 -> 能留;
能;
能 -> 留;
能 -> 留意;
能留;
能留 -> 亦;
能留 -> 一縷;
留;
留 -> 亦;
留 -> 一縷;
留意;
留意 -> 旅;
留意 -> 呂方;
亦;
亦 -> 旅;
亦 -> 呂方;
一縷;
一縷 -> 芳;
旅;
旅 -> 芳;
呂方;
呂方 -> EOS;
芳;
芳 -> EOS;
EOS;
}
";
      Assert.That(expectedDOT.Trim(), Is.EqualTo(compositor.DumpDOT().Trim()));
    }

    [Test]
    public void Test15_Compositor_ResettingFullyOverlappedNodesOnOverride()
    {
      string[] readings = "shui3 guo3 zhi1".Split(' ');
      SimpleLM lm = new(TestDataClass.StrLMSampleDataFruitJuice);
      Compositor compositor = new(lm);
      foreach (string key in readings)
      {
        compositor.InsertKey(key);
      }
      List<Node> result = compositor.Walk();
      List<string> assembledSentence = result.Values().ToList();
      CollectionAssert.AreEqual(new[] { "水果汁" }, result.Values());
      // 測試針對第一個漢字的位置的操作。
      {
        {
          Assert.That(
              compositor.OverrideCandidate(new KeyValuePaired(new List<string> { "shui3" }, "💦"), 0), Is.True
          );
          assembledSentence = compositor.Walk().Values().ToList();
          CollectionAssert.AreEqual(new[] { "💦", "果汁" }, assembledSentence);
        }
        {
          Assert.That(
              compositor.OverrideCandidate(
                  new KeyValuePaired(new List<string> { "shui3", "guo3", "zhi1" }, "水果汁"), 1), Is.True
          );
          assembledSentence = compositor.Walk().Values().ToList();
          CollectionAssert.AreEqual(new[] { "水果汁" }, assembledSentence);
        }
        {
          Assert.That(
              // 再覆寫回來。
              compositor.OverrideCandidate(new KeyValuePaired(new List<string> { "shui3" }, "💦"), 0), Is.True
          );
          assembledSentence = compositor.Walk().Values().ToList();
          CollectionAssert.AreEqual(new[] { "💦", "果汁" }, assembledSentence);
        }
      }

      // 測試針對其他位置的操作。
      {
        {
          Assert.That(
              compositor.OverrideCandidate(new KeyValuePaired(new List<string> { "guo3" }, "裹"), 1), Is.True
          );
          assembledSentence = compositor.Walk().Values().ToList();
          CollectionAssert.AreEqual(new[] { "💦", "裹", "之" }, assembledSentence);
        }
        {
          Assert.That(
              compositor.OverrideCandidate(new KeyValuePaired(new List<string> { "zhi1" }, "知"), 2), Is.True
          );
          assembledSentence = compositor.Walk().Values().ToList();
          CollectionAssert.AreEqual(new[] { "💦", "裹", "知" }, assembledSentence);
        }
        {
          Assert.That(
              // 再覆寫回來。
              compositor.OverrideCandidate(
                  new KeyValuePaired(new List<string> { "shui3", "guo3", "zhi1" }, "水果汁"), 3), Is.True
          );
          assembledSentence = compositor.Walk().Values().ToList();
          CollectionAssert.AreEqual(new[] { "水果汁" }, assembledSentence);
        }
      }
    }

    [Test]
    public void Test16_Compositor_ResettingPartiallyOverlappedNodesOnOverride()
    {
      string[] readings = "ke1 ji4 gong1 yuan2".Split(' ');
      string rawData = TestDataClass.StrLMSampleDataTechGuarden + "\ngong1-yuan2 公猿 -9";
      Compositor compositor = new(new SimpleLM(rawData));
      foreach (string key in readings)
      {
        compositor.InsertKey(key);
      }
      List<Node> result = compositor.Walk();
      CollectionAssert.AreEqual(new[] { "科技", "公園" }, result.Values());

      Assert.That(compositor.OverrideCandidate(
          new KeyValuePaired(new List<string> { "ji4", "gong1" }, "濟公"), 1), Is.True
      );
      result = compositor.Walk();
      CollectionAssert.AreEqual(new[] { "顆", "濟公", "元" }, result.Values());

      Assert.That(compositor.OverrideCandidate(
          new KeyValuePaired(new List<string> { "gong1", "yuan2" }, "公猿"), 2), Is.True
      );
      result = compositor.Walk();
      CollectionAssert.AreEqual(new[] { "科技", "公猿" }, result.Values());

      Assert.That(compositor.OverrideCandidate(
          new KeyValuePaired(new List<string> { "ke1", "ji4" }, "科際"), 0), Is.True
      );
      result = compositor.Walk();
      CollectionAssert.AreEqual(new[] { "科際", "公猿" }, result.Values());
    }

    [Test]
    public void Test17_Compositor_CandidateDisambiguation()
    {
      string[] readings = "da4 shu4 xin1 de5 mi4 feng1".Split(' ');
      System.Text.RegularExpressions.Regex regexToFilter = new("\nshu4-xin1 .*");
      string rawData = regexToFilter.Replace(TestDataClass.StrLMSampleDataEmoji, "");
      Compositor compositor = new(new SimpleLM(rawData));
      foreach (string key in readings)
      {
        compositor.InsertKey(key);
      }
      List<Node> result = compositor.Walk();
      CollectionAssert.AreEqual(new[] { "大樹", "新的", "蜜蜂" }, result.Values());
      int pos = 2;

      Assert.That(compositor.OverrideCandidate(new KeyValuePaired(new List<string> { "xin1" }, "🆕"), pos), Is.True);
      result = compositor.Walk();
      CollectionAssert.AreEqual(new[] { "大樹", "🆕", "的", "蜜蜂" }, result.Values());

      Assert.That(
          compositor.OverrideCandidate(new KeyValuePaired(new List<string> { "xin1", "de5" }, "🆕"), pos), Is.True
      );
      result = compositor.Walk();
      CollectionAssert.AreEqual(new[] { "大樹", "🆕", "蜜蜂" }, result.Values());
    }
  }
}
