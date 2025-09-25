using System;
using System.Collections.Generic;
using System.Linq;

namespace Megrez.Tests {
  internal static class Utils {
    /// <summary>
    /// 返回在當前位置的所有候選字詞（以詞音配對的形式）。<para/>如果組字器內有幅節、且游標
    /// 位於組字器的（文字輸入順序的）最前方（也就是游標位置的數值是最大合規數值）的
    /// 話，那麼這裡會用到 location - 1、以免去在呼叫該函式後再處理的麻煩。
    /// </summary>
    /// <param name="self">組字器。</param>
    /// <param name="location">游標位置。</param>
    /// <param name="filter">候選字音配對陣列。</param>
    /// <returns></returns>
    public static List<KeyValuePaired> FetchCandidatesDeprecatedAt(
        this Compositor self, int location,
        Compositor.CandidateFetchFilter filter = Compositor.CandidateFetchFilter.All) {
      List<KeyValuePaired> result = new();
      if (self.Keys.IsEmpty())
        return result;
      location = Math.Max(0, Math.Min(location, self.Keys.Count - 1));
      // 按照讀音的長度（幅節長度）來給節點排序。
      List<Compositor.NodeWithLocation> anchors = self.FetchOverlappingNodesAt(location);
      string keyAtCursor = self.Keys[location];
      anchors.ForEach(anchor => {
        anchor.Node.Unigrams.ForEach(gram => {
          switch (filter) {
            case Compositor.CandidateFetchFilter.All:
              if (!anchor.Node.KeyArray.Contains(keyAtCursor))
                return;
              break;
            case Compositor.CandidateFetchFilter.BeginAt:
              if (anchor.Node.KeyArray.First() != keyAtCursor)
                return;
              break;
            case Compositor.CandidateFetchFilter.EndAt:
              if (anchor.Node.KeyArray.Last() != keyAtCursor)
                return;
              break;
          }
          result.Add(new(anchor.Node.KeyArray, gram.Value));
        });
      });
      return result;
    }
  }
}
