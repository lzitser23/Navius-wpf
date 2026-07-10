using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.Menubar;

/// <summary>
/// Tier A: derives from the native <see cref="MenuItem"/> but non-interactive
/// (Focusable=false, IsHitTestVisible=false), styled as a muted heading. Also stands in for the
/// contract's NaviusMenubarGroup: since native WPF menus have no wrapping-container concept that
/// preserves child Role/keyboard participation (see NaviusMenubarRadioGroup's remarks), grouping
/// is expressed the idiomatic native way, a Label followed by its items followed by a
/// NaviusMenubarSeparator, with no wrapping element. No NaviusMenubarGroup type is implemented.
/// </summary>
public class NaviusMenubarLabel : MenuItem
{
    static NaviusMenubarLabel()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusMenubarLabel),
            new FrameworkPropertyMetadata(typeof(NaviusMenubarLabel)));
    }

    public NaviusMenubarLabel()
    {
        Focusable = false;
        IsHitTestVisible = false;
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusMenubarLabelAutomationPeer(this);
}

internal sealed class NaviusMenubarLabelAutomationPeer : MenuItemAutomationPeer
{
    public NaviusMenubarLabelAutomationPeer(NaviusMenubarLabel owner) : base(owner)
    {
    }

    protected override string GetClassNameCore() => nameof(NaviusMenubarLabel);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

    protected override bool IsControlElementCore() => true;
}
