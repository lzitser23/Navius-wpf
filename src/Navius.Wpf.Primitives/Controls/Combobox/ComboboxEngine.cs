using System;
using System.Collections.Generic;
using System.Linq;

namespace Navius.Wpf.Primitives.Controls.Combobox;

/// <summary>
/// Pure, STA-free state helpers for the Combobox. Every method here is a total function over its
/// arguments with no dependency on a WPF Application, Dispatcher, or the visual tree, so the whole
/// filter / selection / highlight state machine is directly unit-testable without a UI thread.
///
/// The one contract-critical invariant these encode: selection edits (<see cref="RemoveValue"/>,
/// <see cref="ToggleMultiple"/>, <see cref="RemoveLast"/>) operate on the FULL committed-values
/// list by VALUE IDENTITY, never by any positional index into the currently filtered/displayed
/// rows. This is the historical web regression the port explicitly guards against: a chip removed
/// by its displayed index would delete the wrong value once a filter has narrowed the visible rows.
/// </summary>
public static class ComboboxEngine
{
    /// <summary>
    /// Filters <paramref name="items"/> by <paramref name="query"/>. An empty/whitespace query
    /// shows all items. When <paramref name="filter"/> is null, the default predicate is a
    /// case-insensitive substring match on <c>itemToString(item)</c>.
    /// </summary>
    public static IReadOnlyList<T> Filter<T>(
        IReadOnlyList<T> items,
        string? query,
        Func<T, string> itemToString,
        Func<T, string, bool>? filter)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemToString);

        if (string.IsNullOrEmpty(query))
        {
            return items.ToList();
        }

        var predicate = filter ?? DefaultFilter(itemToString);
        return items.Where(item => predicate(item, query)).ToList();
    }

    /// <summary>The default case-insensitive substring predicate over <c>itemToString(item)</c>.</summary>
    public static Func<T, string, bool> DefaultFilter<T>(Func<T, string> itemToString)
    {
        ArgumentNullException.ThrowIfNull(itemToString);
        return (item, query) =>
            (itemToString(item) ?? string.Empty).IndexOf(query ?? string.Empty, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// Removes <paramref name="toRemove"/> from the full committed-values list by equality, not by
    /// index. Returns a new list; the input is not mutated. This is the chip-remove-by-value path.
    /// </summary>
    public static IReadOnlyList<T> RemoveValue<T>(
        IReadOnlyList<T> values,
        T toRemove,
        IEqualityComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(values);
        var cmp = comparer ?? EqualityComparer<T>.Default;
        return values.Where(v => !cmp.Equals(v, toRemove)).ToList();
    }

    /// <summary>
    /// Toggles <paramref name="value"/> in a multi-select list: removes it (by equality) if present,
    /// otherwise appends it. Returns a new list; the input is not mutated.
    /// </summary>
    public static IReadOnlyList<T> ToggleMultiple<T>(
        IReadOnlyList<T> current,
        T value,
        IEqualityComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(current);
        var cmp = comparer ?? EqualityComparer<T>.Default;

        if (current.Any(v => cmp.Equals(v, value)))
        {
            return current.Where(v => !cmp.Equals(v, value)).ToList();
        }

        return current.Append(value).ToList();
    }

    /// <summary>
    /// Removes the LAST committed value (the Backspace-with-empty-filter behavior). Returns a new
    /// list; an empty input returns an empty list.
    /// </summary>
    public static IReadOnlyList<T> RemoveLast<T>(IReadOnlyList<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0)
        {
            return values.ToList();
        }

        return values.Take(values.Count - 1).ToList();
    }

    /// <summary>
    /// Moves the highlighted-row pointer with NO wrap, clamped to <c>[0, count - 1]</c>, per the
    /// contract's keyboard table. A starting index of -1 (nothing highlighted) resolves to the first
    /// row when moving down and the last row when moving up. An empty list yields -1.
    /// </summary>
    public static int MoveHighlight(int current, int count, int delta)
    {
        if (count <= 0)
        {
            return -1;
        }

        if (current < 0)
        {
            return delta > 0 ? 0 : count - 1;
        }

        var next = current + delta;
        if (next < 0)
        {
            return 0;
        }

        if (next >= count)
        {
            return count - 1;
        }

        return next;
    }
}
