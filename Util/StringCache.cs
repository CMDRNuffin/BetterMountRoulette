namespace BetterMountRoulette.Util;

using System;
using System.Collections.Generic;
using System.Text;

internal static class StringCache
{
    private static readonly Lazy<StringDictionary<ulong>> _characters = new(() => new());
    public static IStringDictionary<ulong> Characters => _characters.Value;

    private static readonly Lazy<StringDictionary<uint>> _mainCommands = new(() => new());
    public static IStringDictionary<uint> MainCommands => _mainCommands.Value;

    private static readonly Lazy<StringDictionary<int>> _pages = new(() => new());
    public static IStringDictionary<int> Pages => _pages.Value;

    private static readonly Lazy<StringDictionary<uint>> _mounts = new(() => new());
    public static IStringDictionary<uint> Mounts => _mounts.Value;

    private static readonly Lazy<StringDictionary<string>> _named = new(() => new());
    public static IStringDictionary<string> Named => _named.Value;

    public interface IStringDictionary<in T>
        where T : notnull
    {
        ReadOnlySpan<byte> this[T id, Func<string> factory] { get; }
        ReadOnlySpan<byte> this[T id, Func<byte[]> factory] { get; }
    }

    private sealed class StringDictionary<T> : IStringDictionary<T>
        where T : notnull
    {
        private readonly Dictionary<T, byte[]> _values = new();
        public ReadOnlySpan<byte> this[T id, Func<string> factory]
        {
            get
            {
                if (!_values.TryGetValue(id, out byte[]? value))
                {
                    string text = factory();
                    value = new byte[Encoding.UTF8.GetByteCount(text)];
                    _values[id] = value;
                    _ = Encoding.UTF8.GetBytes(text, value.AsSpan());
                }

                return value;
            }
        }

        public ReadOnlySpan<byte> this[T id, Func<byte[]> factory]
        {
            get
            {
                if (!_values.TryGetValue(id, out byte[]? value))
                {
                    value = factory();
                    _values[id] = value;
                }

                return value;
            }
        }
    }
}
