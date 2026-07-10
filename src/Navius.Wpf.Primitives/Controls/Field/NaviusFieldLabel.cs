using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Navius.Wpf.Primitives.Controls.Field;

/// <summary>
/// Tier A for the element itself: a native Label whose Target is wired by the ancestor
/// NaviusField (WireDescendants, run once ChildContent is assigned) -- giving native
/// Alt+mnemonic focus delegation the way the web contract's &lt;label for&gt; gives native
/// click-to-focus delegation. WPF's Label does not focus its Target on a plain
/// (non-mnemonic) click the way an HTML label does, so a MouseLeftButtonDown handler
/// restores that parity explicitly rather than silently dropping it.
/// </summary>
public class NaviusFieldLabel : Label
{
    static NaviusFieldLabel()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusFieldLabel), new FrameworkPropertyMetadata(typeof(NaviusFieldLabel)));
    }

    public NaviusFieldLabel()
    {
        PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
    }

    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Target is UIElement target && !ReferenceEquals(e.OriginalSource, target))
        {
            target.Focus();
        }
    }
}
