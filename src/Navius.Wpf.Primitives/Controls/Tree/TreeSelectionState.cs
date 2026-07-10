namespace Navius.Wpf.Primitives.Controls.Tree;

/// <summary>
/// Pure, framework-free port of the web contract's TreeContext selection/navigation math (see
/// docs/parity/tree.md's "WPF strategy": "TreeContext ... is itself framework-agnostic pure C#
/// and ports almost unchanged: VisibleOrder, MoveAsync, SelectSpanAsync, TypeaheadAsync, etc. all
/// operate on plain object graphs with no Blazor dependency"). Operates directly on
/// NaviusTreeNode (itself dependency-free beyond WPF's INotifyPropertyChanged) so every method
/// here is unit-testable without an STA thread, an Application, or a live visual tree.
/// </summary>
public static class TreeSelectionState
{
    /// <summary>Pre-order DFS of the mounted subtree, descending only into expanded nodes.</summary>
    public static List<NaviusTreeNode> VisibleOrder(IReadOnlyList<NaviusTreeNode> roots)
    {
        var result = new List<NaviusTreeNode>();
        Walk(roots);
        return result;

        void Walk(IReadOnlyList<NaviusTreeNode> nodes)
        {
            foreach (var node in nodes)
            {
                result.Add(node);
                if (node.HasChildren && node.IsExpanded)
                {
                    Walk(node.Children!);
                }
            }
        }
    }

    /// <summary>Next enabled index at or after from + delta; -1 if none (APG tree: Up/Down do not wrap).</summary>
    public static int NextEnabledIndex(IReadOnlyList<NaviusTreeNode> visible, int from, int delta)
    {
        var i = from;
        while (true)
        {
            i += delta;
            if (i < 0 || i >= visible.Count)
            {
                return -1;
            }

            if (!visible[i].Disabled)
            {
                return i;
            }
        }
    }

    public static NaviusTreeNode? FirstEnabledFrom(IReadOnlyList<NaviusTreeNode> visible, int start, int delta)
    {
        for (var i = start; i >= 0 && i < visible.Count; i += delta)
        {
            if (!visible[i].Disabled)
            {
                return visible[i];
            }
        }

        return null;
    }

    public static HashSet<object> ReplaceSelection(NaviusTreeNode node) =>
        node.Disabled ? new HashSet<object>() : new HashSet<object> { node.Value };

    public static HashSet<object> ToggleSelection(IReadOnlyCollection<object> current, NaviusTreeNode node)
    {
        var next = new HashSet<object>(current);
        if (node.Disabled)
        {
            return next;
        }

        if (!next.Remove(node.Value))
        {
            next.Add(node.Value);
        }

        return next;
    }

    /// <summary>Adds [from..to] (inclusive, order-independent) into the existing selection, skipping disabled nodes.</summary>
    public static HashSet<object> SelectSpan(
        IReadOnlyCollection<object> current, IReadOnlyList<NaviusTreeNode> visible, NaviusTreeNode from, NaviusTreeNode to)
    {
        var next = new HashSet<object>(current);
        var i = IndexOf(visible, from);
        var j = IndexOf(visible, to);
        if (i < 0 || j < 0)
        {
            return next;
        }

        var lo = Math.Min(i, j);
        var hi = Math.Max(i, j);
        for (var k = lo; k <= hi; k++)
        {
            if (!visible[k].Disabled)
            {
                next.Add(visible[k].Value);
            }
        }

        return next;
    }

    public static HashSet<object> ToggleSelectAll(IReadOnlyCollection<object> current, IReadOnlyList<NaviusTreeNode> visible)
    {
        var selectable = visible.Where(v => !v.Disabled).ToList();
        var allSelected = selectable.Count > 0 && selectable.All(n => current.Contains(n.Value));
        return allSelected ? new HashSet<object>() : new HashSet<object>(selectable.Select(n => n.Value));
    }

    /// <summary>Typeahead match: same-char repeats cycle to the next match on that first letter, else prefix-matches the whole buffer.</summary>
    public static NaviusTreeNode? Typeahead(string buffer, NaviusTreeNode current, IReadOnlyList<NaviusTreeNode> visible)
    {
        if (visible.Count == 0 || buffer.Length == 0)
        {
            return null;
        }

        var query = buffer;
        if (buffer.Length > 1 && buffer.Distinct().Count() == 1)
        {
            query = buffer[..1];
        }

        var start = IndexOf(visible, current);
        var count = visible.Count;

        for (var step = 1; step <= count; step++)
        {
            var idx = ((start < 0 ? -1 : start) + step) % count;
            var candidate = visible[idx];
            if (candidate.Disabled)
            {
                continue;
            }

            if (candidate.Label.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>Sibling nodes (same parent as <paramref name="node"/>) that have children, for the "*" expand-siblings key.</summary>
    public static IReadOnlyList<NaviusTreeNode> ExpandableSiblings(NaviusTreeNode node, IReadOnlyList<NaviusTreeNode> roots)
    {
        var siblings = node.Parent?.Children ?? roots;
        return siblings.Where(n => n.HasChildren).ToList();
    }

    private static int IndexOf(IReadOnlyList<NaviusTreeNode> list, NaviusTreeNode node)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (ReferenceEquals(list[i], node))
            {
                return i;
            }
        }

        return -1;
    }
}
