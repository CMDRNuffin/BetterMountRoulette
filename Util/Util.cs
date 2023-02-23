namespace BetterMountRoulette.Util;

internal static class Util
{
    internal static void Toggle<T>(ref T? field, T value)
    {
        if (Equals(field, value))
        {
            field = default;
        }
        else
        {
            field = value;
        }
    }
}
