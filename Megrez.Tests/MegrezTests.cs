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

using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;

namespace Megrez.Tests;

public class MegrezTests {
  [Test]
  public void TestInput() {
    Console.WriteLine("// 開始測試語言文字輸入處理");
    SimpleLM lmTestInput = new(TestClass.StrSampleData);
    Compositor theCompositor = new(lmTestInput);
    List<Megrez.NodeAnchor> walked = new();

    void Walk() { walked = theCompositor.Walk(0, 0.0); }

    // 模擬輸入法的行為，每次敲字或選字都重新 walk。;
    theCompositor.InsertReadingAtCursor("gao1");
    Walk();
    theCompositor.InsertReadingAtCursor("ji4");
    Walk();
    theCompositor.CursorIndex = 1;
    theCompositor.InsertReadingAtCursor("ke1");
    Walk();
    theCompositor.CursorIndex = 1;
    theCompositor.DeleteReadingToTheFrontOfCursor();
    Walk();
    theCompositor.InsertReadingAtCursor("ke1");
    Walk();
    theCompositor.CursorIndex = 0;
    theCompositor.DeleteReadingToTheFrontOfCursor();
    Walk();
    theCompositor.InsertReadingAtCursor("gao1");
    Walk();
    theCompositor.CursorIndex = theCompositor.Length;
    theCompositor.InsertReadingAtCursor("gong1");
    Walk();
    theCompositor.InsertReadingAtCursor("si1");
    Walk();
    theCompositor.InsertReadingAtCursor("de5");
    Walk();
    theCompositor.InsertReadingAtCursor("nian2");
    Walk();
    theCompositor.InsertReadingAtCursor("zhong1");
    Walk();
    theCompositor.Grid.FixNodeSelectedCandidate(7, "年終");
    Walk();
    theCompositor.InsertReadingAtCursor("jiang3");
    Walk();
    theCompositor.InsertReadingAtCursor("jin1");
    Walk();
    theCompositor.InsertReadingAtCursor("ni3");
    Walk();
    theCompositor.InsertReadingAtCursor("zhe4");
    Walk();
    theCompositor.InsertReadingAtCursor("yang4");
    Walk();

    // 這裡模擬一個輸入法的常見情況：每次敲一個字詞都會
    // walk，然後你回頭編輯完一些內容之後又會立刻重新 walk。
    // 如果只在這裡測試第一遍 walk 的話，測試通過了也無法測試之後再次 walk
    // 是否會正常。
    theCompositor.CursorIndex = 1;
    theCompositor.DeleteReadingToTheFrontOfCursor();

    // 於是咱們 walk 第二遍
    Walk();
    Assert.False(walked.Count == 0);

    // 做好第三遍的準備，這次咱們來一次插入性編輯。
    // 重點測試這句是否正常，畢竟是在 walked 過的節點內進行插入編輯。
    theCompositor.InsertReadingAtCursor("ke1");

    // 於是咱們 walk 第三遍。
    // 這一遍會直接曝露「上述修改是否有對 TheCompositor 造成了破壞性的損失」，
    // 所以很重要。
    Walk();
    Assert.False(walked.Count == 0);

    List<string> composed = new();
    foreach (Megrez.NodeAnchor phrase in walked) {
      if (phrase.Node != null) composed.Add(phrase.Node.CurrentKeyValue.Value);
    }
    Console.WriteLine(string.Join("_", composed));
    List<string> correctResult = new List<string> { "高科技", "公司", "的", "年終", "獎金", "你", "這樣" };
    Console.WriteLine(" - 上述列印結果理應於下面這行一致：");
    Console.WriteLine(string.Join("_", correctResult));
    Assert.AreEqual(string.Join("_", correctResult), string.Join("_", composed));

    // 測試 DumpDOT
    theCompositor.CursorIndex = theCompositor.Length;
    theCompositor.DeleteReadingAtTheRearOfCursor();
    theCompositor.DeleteReadingAtTheRearOfCursor();
    theCompositor.DeleteReadingAtTheRearOfCursor();
    string expectedDumpDot =
        "digraph {\ngraph [ rankdir=LR ];\nBOS;\nBOS -> 高;\n高;\n高 -> 科;\n高 -> 科技;\nBOS -> 高科技;\n高科技;\n高科技 -> 工;\n高科技 -> 公司;\n科;\n科 -> 際;\n科 -> 濟公;\n科技;\n科技 -> 工;\n科技 -> 公司;\n際;\n際 -> 工;\n際 -> 公司;\n濟公;\n濟公 -> 斯;\n工;\n工 -> 斯;\n公司;\n公司 -> 的;\n斯;\n斯 -> 的;\n的;\n的 -> 年;\n的 -> 年終;\n年;\n年 -> 中;\n年終;\n年終 -> 獎;\n年終 -> 獎金;\n中;\n中 -> 獎;\n中 -> 獎金;\n獎;\n獎 -> 金;\n獎金;\n獎金 -> EOS;\n金;\n金 -> EOS;\nEOS;\n}\n";
    Assert.AreEqual(expectedDumpDot, theCompositor.Grid.DumpDot());
  }

  [Test]
  public void TestWordSegmentation() {
    Console.WriteLine("// 開始測試語句分節處理");
    SimpleLM lmTestInput = new(TestClass.StrSampleData, true);
    Compositor theCompositor = new(lmTestInput, separator: "");
    List<Megrez.NodeAnchor> walked = new();

    void Walk(int location) { walked = theCompositor.Walk(location, 0.0); }

    // 模擬輸入法的行為，每次敲字或選字都重新 walk。;
    theCompositor.InsertReadingAtCursor("高");
    theCompositor.InsertReadingAtCursor("科");
    theCompositor.InsertReadingAtCursor("技");
    theCompositor.InsertReadingAtCursor("公");
    theCompositor.InsertReadingAtCursor("司");
    theCompositor.InsertReadingAtCursor("的");
    theCompositor.InsertReadingAtCursor("年");
    theCompositor.InsertReadingAtCursor("終");
    theCompositor.InsertReadingAtCursor("獎");
    theCompositor.InsertReadingAtCursor("金");

    Walk(location: 0);
    List<string> segmented = new();
    foreach (Megrez.NodeAnchor phrase in walked) {
      if (phrase.Node != null) segmented.Add(phrase.Node.CurrentKeyValue.Key);
    }
    Console.WriteLine(string.Join("_", segmented));
    List<string> correctResult = new List<string> { "高科技", "公司", "的", "年終", "獎金" };
    Console.WriteLine(" - 上述列印結果理應於下面這行一致：");
    Console.WriteLine(string.Join("_", correctResult));
    Assert.AreEqual(string.Join("_", correctResult), string.Join("_", segmented));
  }
}

public class SimpleLM : LanguageModel {
  private Dictionary<string, List<Unigram>> _database = new();
  public SimpleLM(string input, bool swapKeyValue = false) {
    List<string> sStream = new(input.Split('\n'));
    foreach (string line in sStream) {
      if (line.Length == 0 || line.FirstOrDefault().CompareTo('#') == 0) continue;
      List<string> lineStream = new(line.Split(' '));
      if (lineStream.Count >= 2) {
        string col0 = lineStream[0];  // 假設其不為 nil
        string col1 = lineStream[1];  // 假設其不為 nil
        double col2 = 0;              // 防呆
        if (lineStream.Count >= 3 && Double.TryParse(lineStream[2], out double number)) col2 = number;
        Unigram u = new(new KeyValuePaired(), 0);
        if (swapKeyValue)
          u.KeyValue = new(col1, col0);
        else
          u.KeyValue = new(col0, col1);
        u.Score = col2;
        if (!_database.ContainsKey(u.KeyValue.Key)) _database.Add(u.KeyValue.Key, new List<Unigram> {});
        _database[u.KeyValue.Key].Add(u);
      }
    }
  }
  public List<Bigram> BigramsForKeys(string precedingKey, string key) { return new(); }
  public List<Unigram> UnigramsFor(string key) => _database.ContainsKey(key) ? _database[key] : new();
  public bool HasUnigramsFor(string key) => _database.ContainsKey(key);
}

public class TestClass {
  public static string StrSampleData =
      @"
#
# 下述詞頻資料取自 libTaBE 資料庫 (http://sourceforge.net/projects/libtabe/)
# (2002 最終版). 該專案於 1999 年由 Pai-Hsiang Hsiao 發起、以 BSD 授權發行。
#

ni3 你 -6.000000 // Non-LibTaBE
zhe4 這 -6.000000 // Non-LibTaBE
yang4 樣 -6.000000 // Non-LibTaBE
si1 絲 -9.495858
si1 思 -9.006414
si1 私 -99.000000
si1 斯 -8.091803
si1 司 -99.000000
si1 嘶 -13.513987
si1 撕 -12.259095
gao1 高 -7.171551
ke1 顆 -10.574273
ke1 棵 -11.504072
ke1 刻 -10.450457
ke1 科 -7.171052
ke1 柯 -99.000000
gao1 膏 -11.928720
gao1 篙 -13.624335
gao1 糕 -12.390804
de5 的 -3.516024
di2 的 -3.516024
di4 的 -3.516024
zhong1 中 -5.809297
de5 得 -7.427179
gong1 共 -8.381971
gong1 供 -8.501463
ji4 既 -99.000000
jin1 今 -8.034095
gong1 紅 -8.858181
ji4 際 -7.608341
ji4 季 -99.000000
jin1 金 -7.290109
ji4 騎 -10.939895
zhong1 終 -99.000000
ji4 記 -99.000000
ji4 寄 -99.000000
jin1 斤 -99.000000
ji4 繼 -9.715317
ji4 計 -7.926683
ji4 暨 -8.373022
zhong1 鐘 -9.877580
jin1 禁 -10.711079
gong1 公 -7.877973
gong1 工 -7.822167
gong1 攻 -99.000000
gong1 功 -99.000000
gong1 宮 -99.000000
zhong1 鍾 -9.685671
ji4 繫 -10.425662
gong1 弓 -99.000000
gong1 恭 -99.000000
ji4 劑 -8.888722
ji4 祭 -10.204425
jin1 浸 -11.378321
zhong1 盅 -99.000000
ji4 忌 -99.000000
ji4 技 -8.450826
jin1 筋 -11.074890
gong1 躬 -99.000000
ji4 冀 -12.045357
zhong1 忠 -99.000000
ji4 妓 -99.000000
ji4 濟 -9.517568
ji4 薊 -12.021587
jin1 巾 -99.000000
jin1 襟 -12.784206
nian2 年 -6.086515
jiang3 講 -9.164384
jiang3 獎 -8.690941
jiang3 蔣 -10.127828
nian2 黏 -11.336864
nian2 粘 -11.285740
jiang3 槳 -12.492933
gong1si1 公司 -6.299461
ke1ji4 科技 -6.736613
ji4gong1 濟公 -13.336653
jiang3jin1 獎金 -10.344678
nian2zhong1 年終 -11.668947
nian2zhong1 年中 -11.373044
gao1ke1ji4 高科技 -9.842421
zhe4yang4 這樣 -6.000000 // Non-LibTaBE
ni3zhe4 你這 -9.000000 // Non-LibTaBE
jiao4 教 -3.676169
jiao4 較 -3.24869962
jiao4yu4 教育 -3.32220565
yu4 育 -3.30192952
  ";
}