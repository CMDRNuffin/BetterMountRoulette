namespace BetterMountRoulette.Config;

using System;

[AttributeUsage(AttributeTargets.Property)]
internal sealed class VersionsAttribute : Attribute
{
    public VersionsAttribute(int introduced, int removed = 0)
    {
        Introduced = introduced;
        Removed = removed;
    }

    public int Introduced { get; }

    public int Removed { get; }
}
