// CSharpened and further development by (c) 2022 and onwards The vChewing Project (MIT License).
// Was initially rebranded from (c) Lukhnos Liu's C++ library "Gramambular 2" (MIT License).
// ====================
// This code is released under the MIT license (SPDX-License-Identifier: MIT)
#pragma warning disable CS1591
using System;

namespace Megrez {
public struct Unigram {
  public string Value { get; set; }
  public double Score { get; set; }
  public Unigram(string value = "", double score = 0) {
    Value = value;
    Score = score;
  }
  public override bool Equals(object obj) => obj is Unigram unigram && Value == unigram.Value
                                             && Math.Abs(Score - unigram.Score) < 0.0000001f;
  public override int GetHashCode() => HashCode.Combine(Value, Score);
  public override string ToString() => $"({Value},{Score})";
  public static bool operator ==(Unigram lhs, Unigram rhs) => lhs.Value == rhs.Value
                                                              && Math.Abs(lhs.Score - rhs.Score) < 0.0000001f;
  public static bool operator !=(Unigram lhs, Unigram rhs) => lhs.Value != rhs.Value
                                                              || Math.Abs(lhs.Score - rhs.Score) > 0.0000001f;
  public static bool operator<(Unigram lhs, Unigram rhs) => lhs.Score < rhs.Score
                                                            || string.Compare(lhs.Value, rhs.Value,
                                                                              StringComparison.Ordinal) < 0;
  public static bool operator>(Unigram lhs, Unigram rhs) => lhs.Score > rhs.Score
                                                            || string.Compare(lhs.Value, rhs.Value,
                                                                              StringComparison.Ordinal) > 0;
  public static bool operator <=(Unigram lhs, Unigram rhs) => lhs.Score <= rhs.Score
                                                              || string.Compare(lhs.Value, rhs.Value,
                                                                                StringComparison.Ordinal) <= 0;
  public static bool operator >=(Unigram lhs, Unigram rhs) => lhs.Score >= rhs.Score
                                                              || string.Compare(lhs.Value, rhs.Value,
                                                                                StringComparison.Ordinal) >= 0;
}
}  // namespace Megrez
