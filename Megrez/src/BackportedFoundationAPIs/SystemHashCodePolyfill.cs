#if !(NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER || NET5_0_OR_GREATER)

using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace System {
  /// <summary>
  /// 針對舊版 .NET 平台提供的 <see cref="HashCode"/> Polyfill。
  /// </summary>
  public struct HashCode {
    private const int Seed = 17;
    private const int Factor = 31;

    private int _value;
    private bool _initialized;

    /// <summary>
    /// 加入新的哈希值來源。
    /// </summary>
    /// <typeparam name="T">來源型別。</typeparam>
    /// <param name="value">來源內容。</param>
    public void Add<T>(T value) {
      Add(value, EqualityComparer<T>.Default);
    }

    /// <summary>
    /// 加入新的哈希值來源。
    /// </summary>
    /// <typeparam name="T">來源型別。</typeparam>
    /// <param name="value">來源內容。</param>
    /// <param name="comparer">自訂比較子。</param>
    public void Add<T>(T value, IEqualityComparer<T>? comparer) {
      int next = value is null ? 0 : (comparer ?? EqualityComparer<T>.Default).GetHashCode(value);
      unchecked {
        if (!_initialized) {
          _value = Seed + next;
          _initialized = true;
        } else {
          _value = _value * Factor + next;
        }
      }
    }

    /// <summary>
    /// 輸出最終哈希值。
    /// </summary>
    /// <returns>整數哈希值。</returns>
    public int ToHashCode() => _initialized ? _value : 0;
  }
}
#endif
