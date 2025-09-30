// (c) 2022 and onwards The vChewing Project (LGPL v3.0 License or later).
// ====================
// This code is released under the SPDX-License-Identifier: `LGPL-3.0-or-later`.

using System;
using System.Collections.Generic;
using System.Linq;

using static System.String;
// ReSharper disable InconsistentNaming

namespace Megrez.Tests {
  public class SimpleLM : LangModelProtocol {
    private Dictionary<string, List<Unigram>> _database = new();
    public string separator { get; set; }
    public SimpleLM(string input, bool swapKeyValue = false, string separator = "-") {
      this.separator = separator;
      this.ReConstruct(input, swapKeyValue, separator);
    }

    public void ReConstruct(string input, bool swapKeyValue = false, string? separator = null) {
      this.separator = separator ?? this.separator;
      List<string> sStream = new(input.Split('\n'));
      sStream.ForEach(line => {
        if (IsNullOrEmpty(line) || line.FirstOrDefault().CompareTo('#') == 0)
          return;
        List<string> lineStream = new(line.Split(' '));
        if (lineStream.Count != 3)
          return;
        string col0 = lineStream[0];  // 假設其不為 nil
        string col1 = lineStream[1];  // 假設其不為 nil
        double col2 = 0;              // 防呆
        if (lineStream.Count >= 3 && double.TryParse(lineStream[2], out double number))
          col2 = number;
        string key;
        string value;
        if (swapKeyValue) {
          key = col1;
          value = col0;
        } else {
          key = col0;
          value = col1;
        }
        List<string> keyArray = SplitKey(key);
        Unigram u = new(keyArray, value, col2);
        if (!_database.ContainsKey(key))
          _database.Add(key, new());
        _database[key].Add(u);
      });
    }

    public bool HasUnigramsFor(List<string> keyArray) => _database.ContainsKey(keyArray.Joined(separator: separator));
    public List<Unigram> UnigramsFor(List<string> keyArray) =>
        _database.ContainsKey(keyArray.Joined(separator: separator)) ? _database[keyArray.Joined(separator: separator)]
                                                                     : new();
    public void Trim(string key, string value) {
      if (!_database.TryGetValue(key, out List<Unigram>? arr))
        return;

      if (arr is not { } theArr)
        return;
      theArr = theArr.Where(x => x.Value != value).ToList();
      if (theArr.IsEmpty()) {
        _database.Remove(key);
        return;
      }
      _database[key] = theArr;
    }

    private List<string> SplitKey(string key) {
      if (string.IsNullOrEmpty(separator)) {
        return key.Select(c => c.ToString()).ToList();
      }
      return key.Split(separator, StringSplitOptions.RemoveEmptyEntries).ToList();
    }
  }

  public class MockLM : LangModelProtocol {
    public bool HasUnigramsFor(List<string> keyArray) => !IsNullOrEmpty(keyArray.Joined());
    public List<Unigram> UnigramsFor(List<string> keyArray) =>
        new() { new(keyArray, value: keyArray.Joined(), score: -1) };
  }
}
