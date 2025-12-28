namespace BetterMountRoulette.Util.Memory;

using System;

internal static class StringViewExtensions
{
    public static bool Contains(this string self, StringView value, StringComparison comparisonType)
    {
        return self.AsSpan().Contains(value.AsSpan(), comparisonType);
    }
}
