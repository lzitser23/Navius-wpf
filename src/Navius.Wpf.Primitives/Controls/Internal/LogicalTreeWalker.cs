using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.Internal;

/// <summary>
/// Shared logical-tree descendant search used by the lookless Tier B families
/// (RadioGroup, Collapsible, Accordion, ToggleGroup) whose root discovers its
/// context-driven parts (triggers/panels/items) without a live visual tree or
/// layout pass. Walking the logical tree (not visual) means descendants are
/// discoverable immediately after Content is assigned, which matters both for
/// unit tests and for XAML parse-time ordering.
/// </summary>
internal static class LogicalTreeWalker
{
    public static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is not DependencyObject childObj)
            {
                continue;
            }

            if (childObj is T match)
            {
                yield return match;
            }

            foreach (var descendant in Descendants<T>(childObj))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>Walks upward through the logical tree to find the nearest ancestor of type T.</summary>
    public static T? Ancestor<T>(DependencyObject node) where T : DependencyObject
    {
        var current = LogicalTreeHelper.GetParent(node);
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = LogicalTreeHelper.GetParent(current);
        }

        return null;
    }
}
