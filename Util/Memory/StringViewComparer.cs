namespace BetterMountRoulette.Util.Memory;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

internal sealed class StringViewComparer : IComparer<StringView>, IEqualityComparer<StringView>
{
    private readonly StringComparison _comparisonType;

    public static StringViewComparer InvariantCultureIgnoreCase => field ??= new(StringComparison.InvariantCultureIgnoreCase);

    private StringViewComparer(StringComparison comparisonType)
    {
        _comparisonType = comparisonType;
    }

    public int Compare(StringView x, StringView y)
    {
        return x.AsSpan().CompareTo(y.AsSpan(), _comparisonType);
    }

    public bool Equals(StringView x, StringView y)
    {
        return x.Equals(y, _comparisonType);
    }

    public int GetHashCode([DisallowNull] StringView obj)
    {
        return string.GetHashCode(obj.AsSpan(), _comparisonType);
    }
}
