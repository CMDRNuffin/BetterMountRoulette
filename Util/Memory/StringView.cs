namespace BetterMountRoulette.Util.Memory;

using System;

internal readonly struct StringView
{
    private readonly string _value;
    private readonly int _offset;

    public StringView(string value, int offset, int length) : this(value, offset, length, validate: true)
    {
    }

    public StringView(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value;
        _offset = 0;
        Length = value.Length;
    }

    private StringView(string value, int offset, int length, bool validate)
    {
        if (validate)
        {
            ArgumentNullException.ThrowIfNull(value);
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, value.Length);
            ArgumentOutOfRangeException.ThrowIfNegative(length);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(length, value.Length - offset);
        }

        _value = value;
        _offset = offset;
        Length = length;
    }

    public int Length { get; }

    public bool IsEmpty => Length == 0;

    public override string ToString()
    {
        if (Length == 0)
        {
            return string.Empty;
        }

        return _value.Substring(_offset, Length);
    }

    public static implicit operator StringView(string value)
    {
        return new(value);
    }

    public StringView Trim()
    {
        if (Length == 0)
        {
            return new();
        }

        // the initial newOffset is chosen so we don't scan the entire string twice
        // when there are no non-whitespace characters
        int end = _offset + Length;
        int newOffset = end;
        for (int i = _offset; i < end; ++i)
        {
            if (!char.IsWhiteSpace(_value[i]))
            {
                newOffset = i;
                break;
            }
        }

        int newLength = 0;
        for (int i = end; i > newOffset; --i)
        {
            if (!char.IsWhiteSpace(_value[i - 1]))
            {
                newLength = i - newOffset;
                break;
            }
        }

        return new(_value, newOffset, newLength, validate: false);
    }

    public bool Equals(StringView other, StringComparison comparisonType)
    {
        if (comparisonType > StringComparison.OrdinalIgnoreCase)
        {
            throw new ArgumentException("Invalid string comparison value");
        }

        if (Length == 0)
        {
            return other.Length == 0;
        }

        if (other.Length == 0)
        {
            return false;
        }

        return AsSpan().Equals(other.AsSpan(), comparisonType);
    }

    public ReadOnlySpan<char> AsSpan()
    {
        return _value.AsSpan()[_offset..(_offset + Length)];
    }
}
