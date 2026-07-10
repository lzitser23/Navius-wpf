using System;
using System.Collections.Generic;

namespace Navius.Wpf.Primitives.Controls.Select;

/// <summary>
/// Pure, STA-free state-machine helpers for the Select control family: highlight movement,
/// multi-select toggling, single-commit resolution, and type-ahead matching. Every method is a
/// plain static function over primitives/collections so it can be unit-tested directly without a
/// WPF Application or an STA thread (the parity contract's "filter/selection logic as pure
/// testable methods" rule applies to Select's toggle/commit/highlight machines just as it does to
/// a filterable list).
/// </summary>
public static class SelectSelectionEngine
{
    /// <summary>
    /// Moves a highlight index by <paramref name="delta"/> over <paramref name="count"/> options.
    /// With <paramref name="loop"/> false the result is clamped at the ends (the Select spec
    /// default); with loop true it wraps. Returns -1 when there are no options. A negative
    /// <paramref name="current"/> (nothing highlighted yet) is treated as "before the first"
    /// so a +1 step lands on 0 and a -1 step lands on the last option.
    /// </summary>
    public static int MoveHighlight(int current, int count, int delta, bool loop)
    {
        if (count <= 0)
        {
            return -1;
        }

        if (current < 0)
        {
            return delta >= 0 ? 0 : count - 1;
        }

        var next = current + delta;

        if (loop)
        {
            return ((next % count) + count) % count;
        }

        return Math.Clamp(next, 0, count - 1);
    }

    /// <summary>
    /// Toggle-in-set for multi-select: returns a new list with <paramref name="value"/> removed if
    /// already present, otherwise appended. The input is never mutated.
    /// </summary>
    public static IReadOnlyList<T> ToggleMultiple<T>(IReadOnlyList<T> current, T value, IEqualityComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(current);
        comparer ??= EqualityComparer<T>.Default;

        var result = new List<T>(current);
        var index = result.FindIndex(x => comparer.Equals(x, value));
        if (index >= 0)
        {
            result.RemoveAt(index);
        }
        else
        {
            result.Add(value);
        }

        return result;
    }

    /// <summary>
    /// Resolves the value a single-select commit should apply: the <paramref name="previous"/>
    /// value if the item's cancelable Select was <paramref name="prevented"/>, otherwise the
    /// <paramref name="proposed"/> value.
    /// </summary>
    public static T? ResolveSingleCommit<T>(T? previous, T proposed, bool prevented) =>
        prevented ? previous : proposed;

    /// <summary>
    /// First-character type-ahead: searches forward (wrapping) from the option after
    /// <paramref name="currentIndex"/> for the first entry in <paramref name="texts"/> whose first
    /// character matches <paramref name="ch"/> (case-insensitive). Returns the matching index or
    /// null when nothing matches. A <paramref name="currentIndex"/> of -1 starts the search at 0.
    /// </summary>
    public static int? FindTypeaheadMatch(IReadOnlyList<string> texts, int currentIndex, char ch)
    {
        ArgumentNullException.ThrowIfNull(texts);
        if (texts.Count == 0)
        {
            return null;
        }

        var target = char.ToLowerInvariant(ch);
        for (var step = 1; step <= texts.Count; step++)
        {
            var index = (currentIndex + step) % texts.Count;
            if (index < 0)
            {
                index += texts.Count;
            }

            var text = texts[index];
            if (!string.IsNullOrEmpty(text) && char.ToLowerInvariant(text[0]) == target)
            {
                return index;
            }
        }

        return null;
    }
}
