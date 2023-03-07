// CSharpened and further development by (c) 2022 and onwards The vChewing Project (MIT License).
// Was initially rebranded from (c) Lukhnos Liu's C++ library "Gramambular 2" (MIT License).
// ====================
// This code is released under the MIT license (SPDX-License-Identifier: MIT)

using System.Collections.Generic;
using System.Linq;
using static System.String;

namespace Megrez.Tests;

public class SimpleLM : LangModelProtocol {
  private Dictionary<string, List<Unigram>> _database = new();
  public SimpleLM(string input, bool swapKeyValue = false) {
    List<string> sStream = new(input.Split('\n'));
    foreach (string line in sStream) {
      if (IsNullOrEmpty(line) || line.FirstOrDefault().CompareTo('#') == 0) continue;
      List<string> lineStream = new(line.Split(' '));
      if (lineStream.Count >= 2) {
        string col0 = lineStream[0];  // 假設其不為 nil
        string col1 = lineStream[1];  // 假設其不為 nil
        double col2 = 0;              // 防呆
        if (lineStream.Count >= 3 && double.TryParse(lineStream[2], out double number)) col2 = number;
        string key;
        string value;
        if (swapKeyValue) {
          key = col1;
          value = col0;
        } else {
          key = col0;
          value = col1;
        }
        Unigram u = new(value);
        u.Score = col2;
        if (!_database.ContainsKey(key)) _database.Add(key, new());
        _database[key].Add(u);
      }
    }
  }
  public bool HasUnigramsFor(List<string> keyArray) => _database.ContainsKey(keyArray.Joined());
  public List<Unigram> UnigramsFor(List<string> keyArray) => _database.ContainsKey(keyArray.Joined())
                                                                 ? _database[keyArray.Joined()]
                                                                 : new();
  public void Trim(string key, string value) {
    if (!_database.TryGetValue(key, out List<Unigram>? arr)) return;
    if (arr is not {} theArr) return;
    theArr = theArr.Where(x => x.Value != value).ToList();
    if (theArr.IsEmpty()) {
      _database.Remove(key);
      return;
    }
    _database[key] = theArr;
  }
}

public class MockLM : LangModelProtocol {
  public bool HasUnigramsFor(List<string> keyArray) => !IsNullOrEmpty(keyArray.Joined());
  public List<Unigram> UnigramsFor(List<string> keyArray) => new() { new(value: keyArray.Joined(), score: -1) };
}

public class TestLM : LangModelProtocol {
  public bool HasUnigramsFor(List<string> keyArray) => keyArray.Joined() == "foo";
  public List<Unigram> UnigramsFor(List<string> keyArray) => keyArray.Joined() == "foo"
                                                                 ? new() { new(keyArray.Joined(), -1) }
                                                                 : new();
}

public class TestLMForRanked : LangModelProtocol {
  public bool HasUnigramsFor(List<string> keyArray) => keyArray.Joined() == "foo";
  public List<Unigram> UnigramsFor(List<string> keyArray) => keyArray.Joined() == "foo"
                                                                 ? new() { new("middle", -5), new("highest", -2),
                                                                           new("lowest", -10) }
                                                                 : new();
}

public class TestDataClass {
  public static string StrStressData =
      @"
yi1 一 -2.08170692
yi1-yi1 一一 -4.38468400

    ";

  public static string StrEmojiSampleData =
      @"
gao1 高 -2.9396
re4 熱 -3.6024
gao1re4 高熱 -6.526
huo3 火 -3.6966
huo3 🔥 -8
yan4 焰 -5.4466
huo3yan4 火焰 -5.6231
huo3yan4 🔥 -8
wei2 危 -3.9832
xian3 險 -3.7810
wei2xian3 危險 -4.2623
mi4feng1 蜜蜂 -3.6231
mi4 蜜 -4.6231
feng1 蜂 -4.6231
feng1 🐝 -11
mi4feng1 🐝 -11

      ";

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