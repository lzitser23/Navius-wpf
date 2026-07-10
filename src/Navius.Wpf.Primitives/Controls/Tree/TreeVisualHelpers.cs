using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Navius.Wpf.Primitives.Controls.Tree;

internal static class TreeVisualHelpers
{
    /// <summary>Walks up the visual (falling back to logical) tree from start looking for the nearest ancestor of type T.</summary>
    public static T? FindAncestor<T>(DependencyObject? start) where T : DependencyObject
    {
        var current = start;
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = current is Visual or Visual3D
                ? VisualTreeHelper.GetParent(current)
                : LogicalTreeHelper.GetParent(current);
        }

        return null;
    }
}
