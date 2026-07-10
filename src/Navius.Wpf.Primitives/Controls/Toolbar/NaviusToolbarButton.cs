using System.Windows;

namespace Navius.Wpf.Primitives.Controls.Toolbar;

/// <summary>
/// Tier A: derives from NaviusButton directly (per docs/parity/toolbar.md's parameter table,
/// which lists only Disabled/ChildContent/Attributes -- no toolbar-specific behavior beyond what
/// NaviusButton already provides), inheriting its soft-disabled mode, OnClick funnel, and
/// NaviusButtonAutomationPeer. The only addition is IToolbarItem, so the owning NaviusToolbar's
/// roving-focus scan picks it up alongside links and toggle items.
/// </summary>
public class NaviusToolbarButton : NaviusButton, IToolbarItem
{
    static NaviusToolbarButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusToolbarButton),
            new FrameworkPropertyMetadata(typeof(NaviusToolbarButton)));
    }
}
