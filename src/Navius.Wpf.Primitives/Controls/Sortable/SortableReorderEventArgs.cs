using System.Windows;

namespace Navius.Wpf.Primitives.Controls.Sortable;

/// <summary>
/// Carries the committed reorder's old/new index (mirrors the contract's
/// <c>record SortableReorderEventArgs(int OldIndex, int NewIndex)</c>). A RoutedEventArgs subclass
/// so <see cref="NaviusSortable.OnReorder"/> can be a bubbling RoutedEvent, matching this
/// codebase's convention (see NaviusRating.ValueChanged).
/// </summary>
public class SortableReorderEventArgs : RoutedEventArgs
{
    public SortableReorderEventArgs(RoutedEvent routedEvent, object source, int oldIndex, int newIndex)
        : base(routedEvent, source)
    {
        OldIndex = oldIndex;
        NewIndex = newIndex;
    }

    /// <summary>Zero-based position of the moved item before the reorder committed.</summary>
    public int OldIndex { get; }

    /// <summary>Zero-based position of the moved item after the reorder committed.</summary>
    public int NewIndex { get; }
}
