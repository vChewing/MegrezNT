// CSharpened and further development by (c) 2022 and onwards The vChewing Project (MIT License).
// Was initially rebranded from (c) Lukhnos Liu's C++ library "Gramambular 2" (MIT License).
// ====================
// This code is released under the MIT license (SPDX-License-Identifier: MIT)
#pragma warning disable CS1591
using System.Collections.Generic;

namespace Megrez {
public interface LangModelProtocol {
  public List<Unigram> UnigramsFor(List<string> keyArray);
  public bool HasUnigramsFor(List<string> keyArray);
}

public partial struct Compositor {
  public class LangModelRanked : LangModelProtocol {
    private LangModelProtocol langModel;
    public LangModelRanked(LangModelProtocol withLM) { langModel = withLM; }
    public List<Unigram> UnigramsFor(List<string> keyArray) =>
        langModel.UnigramsFor(keyArray).StableSorted((x, y) => y.Score.CompareTo(x.Score));
    public bool HasUnigramsFor(List<string> keyArray) => langModel.HasUnigramsFor(keyArray);
  }
}
}  // namespace Megrez
