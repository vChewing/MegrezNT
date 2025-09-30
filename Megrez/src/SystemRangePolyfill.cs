#if !(NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER)
#pragma warning disable CS1591
namespace System {
  /// <summary>
  /// 自訂的 <see cref="Index"/> 實作，用於補足舊版 .NET Framework 未提供的功能。
  /// 僅涵蓋 MegrezNT 專案所需的最小 API 表面。
  /// </summary>
  public readonly struct Index : IEquatable<Index> {
    private readonly int _value;

    /// <summary>
    /// 取得索引的整數值。
    /// </summary>
    public int Value => _value;

    /// <summary>
    /// 指示該索引是否自序列末端計算。
    /// </summary>
    public bool IsFromEnd { get; }

    public static Index Start => new(0, false);
    public static Index End => new(0, true);

    public Index(int value, bool fromEnd = false) {
      if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
      _value = value;
      IsFromEnd = fromEnd;
    }

    public static implicit operator Index(int value) => new(value);

    public int GetOffset(int length) {
      if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
      if (!IsFromEnd) return _value;
      if (_value > length) throw new ArgumentOutOfRangeException(nameof(length));
      return length - _value;
    }

    public bool Equals(Index other) => _value == other._value && IsFromEnd == other.IsFromEnd;

    public override bool Equals(object obj) => obj is Index other && Equals(other);

    public override int GetHashCode() => unchecked((_value * 397) ^ IsFromEnd.GetHashCode());

    public override string ToString() => IsFromEnd ? $"^{_value}" : _value.ToString();
  }

  /// <summary>
  /// 自訂的 <see cref="Range"/> 實作，用於補足舊版 .NET Framework 未提供的功能。
  /// 僅涵蓋 MegrezNT 專案所需的最小 API 表面。
  /// </summary>
  public readonly struct Range : IEquatable<Range> {
    public Index Start { get; }
    public Index End { get; }

    public Range(Index start, Index end) {
      Start = start;
      End = end;
    }

    public static Range StartAt(Index start) => new(start, Index.End);

    public static Range EndAt(Index end) => new(Index.Start, end);

    public bool Equals(Range other) => Start.Equals(other.Start) && End.Equals(other.End);

    public override bool Equals(object obj) => obj is Range other && Equals(other);

    public override int GetHashCode() => unchecked((Start.GetHashCode() * 397) ^ End.GetHashCode());

    public override string ToString() => $"{Start}..{End}";
  }
}
#pragma warning restore CS1591
#endif
