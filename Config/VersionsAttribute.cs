namespace BetterMountRoulette.Config;

using System;

[AttributeUsage(AttributeTargets.Property)]
internal sealed class VersionsAttribute(int introduced, int removed = 0) : Attribute
{
    public int Introduced { get; } = introduced;

    public int Removed { get; } = removed;
}
