using System.Windows.Automation.Peers;

namespace Navius.Wpf.Primitives.Controls.Toolbar;

/// <summary>
/// Custom peer subclass rather than reusing WPF's native ToolBarAutomationPeer: per
/// docs/parity/toolbar.md's "Open questions", ToolBarAutomationPeer belongs to a native ToolBar
/// control with overflow-menu behavior this component intentionally does not have, so borrowing
/// it would be semantically misleading even though it is the closest built-in match. This peer
/// only reports the two things the web contract's root actually renders: role="toolbar"
/// (AutomationControlType.ToolBar) and aria-orientation.
/// </summary>
public class NaviusToolbarAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusToolbarAutomationPeer(NaviusToolbar owner) : base(owner)
    {
    }

    private NaviusToolbar Toolbar => (NaviusToolbar)Owner;

    protected override string GetClassNameCore() => nameof(NaviusToolbar);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ToolBar;

    protected override AutomationOrientation GetOrientationCore() =>
        string.Equals(Toolbar.Orientation, "vertical", StringComparison.OrdinalIgnoreCase)
            ? AutomationOrientation.Vertical
            : AutomationOrientation.Horizontal;
}
