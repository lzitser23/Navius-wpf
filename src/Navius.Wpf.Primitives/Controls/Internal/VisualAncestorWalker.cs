using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Navius.Wpf.Primitives.Controls.Internal;

/// <summary>
/// Shared ancestor lookup used by the Field/Fieldset/Form/PasswordToggleField families,
/// whose parts (Label, Control, Toggle, Icon, Slot, Input) need to find their owning root
/// without a Blazor-style cascading-parameter mechanism. Walks the visual tree first
/// (parts are realized inside a live template by the time <c>Loaded</c> fires) and falls
/// back to the logical tree, since a part built directly in code (as the OTP/PasswordToggle
/// composites do) may not have a visual parent yet.
/// </summary>
internal static class VisualAncestorWalker
{
    public static T? FindAncestor<T>(DependencyObject? start) where T : DependencyObject
    {
        var current = start;
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            var next = (current is Visual or Visual3D) ? VisualTreeHelper.GetParent(current) : null;
            current = next ?? LogicalTreeHelper.GetParent(current);
        }

        return null;
    }
}
