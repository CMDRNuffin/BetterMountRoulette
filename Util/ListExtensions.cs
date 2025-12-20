namespace BetterMountRoulette.Util;

using System;
using System.Collections.Generic;

internal static class ListExtensions
{
    /// <summary>
    /// Filters an unsorted list in place (to prevent garbage collection), but only if any items remain.
    /// This is accomplished by sorting items matching the filter to the front of the list and then deleting
    /// all items past the last item that matched if and only if any item actually did.
    /// </summary>
    /// <typeparam name="T">The list's item type</typeparam>
    /// <param name="list">The list to filter</param>
    /// <param name="condition">The condition based on which the list gets filtered</param>
    public static void NonClearingUnsortedFindAllInPlace<T>(this List<T> list, Predicate<T> condition) where T : class
    {
        int insertIndex = 0;
        for (int i = 0; i < list.Count; ++i)
        {
            T item = list[i];
            if (condition(item))
            {
                // yes, this also happens if insertIndex == i
                // this is not a problem because swapping an item with itself is safe
                T temp = list[insertIndex];
                list[insertIndex] = item;
                list[i] = temp;

                insertIndex++;
            }
        }

        if (list.Count >= insertIndex && insertIndex > 0)
        {
            list.RemoveRange(insertIndex, list.Count - insertIndex);
        }
    }
}
