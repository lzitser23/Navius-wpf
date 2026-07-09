using System;
using System.Collections.Generic;

namespace Navius.Wpf.Primitives.Controls.Autocomplete;

/// <summary>
/// Pure, deterministic autocomplete logic with zero WPF/STA/Application dependency, so it can be
/// unit tested directly. Mirrors the Blazor contract's filtering and highlight-movement semantics
/// (see docs/parity/autocomplete.md). Both members are the WPF port's realization of the web
/// AutocompleteContext's filtering plus <c>MoveHighlightAsync</c>.
/// </summary>
public static class AutocompleteEngine
{
    /// <summary>
    /// Filters <paramref name="items"/> by <paramref name="query"/>. An empty or whitespace query
    /// shows all items (contract: "empty query shows all"). When <paramref name="filter"/> is null,
    /// the default is a case-insensitive substring match against <paramref name="itemToString"/>.
    /// </summary>
    public static IReadOnlyList<T> Filter<T>(
        IReadOnlyList<T> items,
        string? query,
        Func<T, string> itemToString,
        Func<T, string, bool>? filter)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemToString);

        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<T>(items);
        }

        var predicate = filter ?? DefaultContains;
        var result = new List<T>(items.Count);
        foreach (var item in items)
        {
            if (predicate(item, query))
            {
                result.Add(item);
            }
        }

        return result;

        bool DefaultContains(T item, string q) =>
            (itemToString(item) ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Computes the next highlighted index. <paramref name="current"/> is <c>-1</c> when nothing is
    /// highlighted; from there a positive <paramref name="delta"/> lands on the first row and a
    /// negative one on the last (the ArrowUp-opens-on-last behavior). With <paramref name="loop"/>
    /// false (the contract default) the result clamps into <c>[0, count-1]</c> without wrapping;
    /// an empty list always yields <c>-1</c>.
    /// </summary>
    public static int MoveHighlight(int current, int count, int delta, bool loop = false)
    {
        if (count <= 0)
        {
            return -1;
        }

        int next;
        if (current < 0)
        {
            next = delta >= 0 ? 0 : count - 1;
        }
        else
        {
            next = current + delta;
        }

        if (loop)
        {
            return ((next % count) + count) % count;
        }

        if (next < 0)
        {
            return 0;
        }

        return next > count - 1 ? count - 1 : next;
    }
}
