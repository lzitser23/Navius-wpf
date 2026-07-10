namespace Navius.Wpf.Primitives.Controls.Sortable;

/// <summary>Which way a keyboard-grabbed item is being moved through the order.</summary>
public enum SortableMove
{
    /// <summary>ArrowDown / ArrowRight while grabbing: one enabled slot toward the end.</summary>
    Forward,

    /// <summary>ArrowUp / ArrowLeft while grabbing: one enabled slot toward the start.</summary>
    Backward,

    /// <summary>Home while grabbing: to the first enabled slot.</summary>
    First,

    /// <summary>End while grabbing: to the last enabled slot.</summary>
    Last,
}

/// <summary>The outcome of a keyboard grab-move: the new key order and the grabbed item's new index.</summary>
public readonly record struct SortableMoveResult(IReadOnlyList<string> Order, int GrabbedIndex, bool Moved);

/// <summary>
/// Pure, framework-agnostic APG "grab and move" reducer for <see cref="NaviusSortable"/>, factored
/// out of the control so it is unit-testable without an STA Application (mirrors NaviusRatingMath's
/// factoring in the Rating family, see docs/parity/sortable.md "WPF strategy"). Operates on an
/// ordered list of string keys plus a "is this key disabled" predicate; never touches WPF types.
///
/// Disabled rows are skipped by roving navigation entirely and are jumped over (never landed on)
/// during a grab-move, so keyboard focus and grabbed items only ever rest on enabled slots.
/// </summary>
public static class SortableKeyboardReducer
{
    /// <summary>
    /// First enabled index strictly after <paramref name="fromIndex"/>, or -1 if none. Pass -1 to
    /// scan from the start inclusive (this is how <see cref="FirstEnabled"/> is expressed).
    /// </summary>
    public static int NextEnabled(IReadOnlyList<string> order, Func<string, bool> isDisabled, int fromIndex)
    {
        for (var i = Math.Max(fromIndex, 0) + (fromIndex < 0 ? 0 : 1); i < order.Count; i++)
        {
            if (!isDisabled(order[i]))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Last enabled index strictly before <paramref name="fromIndex"/>, or -1 if none. Pass
    /// <c>order.Count</c> to scan from the end inclusive (as <see cref="LastEnabled"/> does).
    /// </summary>
    public static int PrevEnabled(IReadOnlyList<string> order, Func<string, bool> isDisabled, int fromIndex)
    {
        for (var i = Math.Min(fromIndex, order.Count) - 1; i >= 0; i--)
        {
            if (!isDisabled(order[i]))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Index of the first enabled key, or -1 if every key is disabled.</summary>
    public static int FirstEnabled(IReadOnlyList<string> order, Func<string, bool> isDisabled) =>
        NextEnabled(order, isDisabled, -1);

    /// <summary>Index of the last enabled key, or -1 if every key is disabled.</summary>
    public static int LastEnabled(IReadOnlyList<string> order, Func<string, bool> isDisabled) =>
        PrevEnabled(order, isDisabled, order.Count);

    /// <summary>
    /// Roving-focus target when NOT grabbing: the next/previous/first/last enabled index relative to
    /// <paramref name="fromIndex"/>. Returns <paramref name="fromIndex"/> unchanged when no other
    /// enabled slot exists in that direction (focus does not wrap).
    /// </summary>
    public static int Rove(IReadOnlyList<string> order, Func<string, bool> isDisabled, int fromIndex, SortableMove move)
    {
        var target = move switch
        {
            SortableMove.Forward => NextEnabled(order, isDisabled, fromIndex),
            SortableMove.Backward => PrevEnabled(order, isDisabled, fromIndex),
            SortableMove.First => FirstEnabled(order, isDisabled),
            SortableMove.Last => LastEnabled(order, isDisabled),
            _ => -1,
        };

        return target < 0 ? fromIndex : target;
    }

    /// <summary>
    /// Moves the grabbed item (at <paramref name="grabbedIndex"/>) one enabled slot in the requested
    /// direction (or all the way to the first/last enabled slot). The grabbed item jumps over any
    /// disabled rows in its path; disabled rows keep their relative positions. Returns the unchanged
    /// order with <c>Moved = false</c> when there is no enabled slot to move into.
    /// </summary>
    public static SortableMoveResult Move(
        IReadOnlyList<string> order, Func<string, bool> isDisabled, int grabbedIndex, SortableMove move)
    {
        if (grabbedIndex < 0 || grabbedIndex >= order.Count)
        {
            return new SortableMoveResult(order, grabbedIndex, false);
        }

        var grabbedKey = order[grabbedIndex];
        var list = new List<string>(order);

        switch (move)
        {
            case SortableMove.Forward:
            {
                var next = NextEnabled(order, isDisabled, grabbedIndex);
                if (next < 0)
                {
                    return new SortableMoveResult(order, grabbedIndex, false);
                }

                list.RemoveAt(grabbedIndex);
                var insertAt = next; // removal shifted the target down by one, so insert AT its old index puts us after it
                list.Insert(insertAt, grabbedKey);
                return new SortableMoveResult(list, insertAt, true);
            }

            case SortableMove.Backward:
            {
                var prev = PrevEnabled(order, isDisabled, grabbedIndex);
                if (prev < 0)
                {
                    return new SortableMoveResult(order, grabbedIndex, false);
                }

                list.RemoveAt(grabbedIndex);
                list.Insert(prev, grabbedKey);
                return new SortableMoveResult(list, prev, true);
            }

            case SortableMove.First:
            {
                var first = FirstEnabled(order, isDisabled);
                if (first < 0 || first == grabbedIndex)
                {
                    return new SortableMoveResult(order, grabbedIndex, false);
                }

                list.RemoveAt(grabbedIndex);
                var target = FirstEnabled(list, isDisabled);
                var insertAt = target < 0 ? 0 : target;
                list.Insert(insertAt, grabbedKey);
                return new SortableMoveResult(list, insertAt, insertAt != grabbedIndex);
            }

            case SortableMove.Last:
            {
                var last = LastEnabled(order, isDisabled);
                if (last < 0 || last == grabbedIndex)
                {
                    return new SortableMoveResult(order, grabbedIndex, false);
                }

                list.RemoveAt(grabbedIndex);
                var target = LastEnabled(list, isDisabled);
                var insertAt = target < 0 ? list.Count : target + 1;
                list.Insert(insertAt, grabbedKey);
                return new SortableMoveResult(list, insertAt, insertAt != grabbedIndex);
            }

            default:
                return new SortableMoveResult(order, grabbedIndex, false);
        }
    }
}
