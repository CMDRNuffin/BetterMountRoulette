namespace BetterMountRoulette.Util;

internal static class Util
{
    internal static void Toggle<T>(ref T? field, T value)
    {
        field = Equals(field, value) ? default : value;
    }
}
