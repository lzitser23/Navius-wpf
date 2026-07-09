using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Surfaces the contract's aria-valuetext (GetValueLabel or the default rounded percentage, absent
/// while indeterminate) via ItemStatus, since ProgressBarAutomationPeer has no first-class value-text
/// slot of its own.
/// </summary>
public class NaviusProgressAutomationPeer : ProgressBarAutomationPeer
{
    public NaviusProgressAutomationPeer(NaviusProgress owner) : base(owner)
    {
    }

    private NaviusProgress Progress => (NaviusProgress)Owner;

    protected override string GetItemStatusCore() => Progress.FormatValueText() ?? string.Empty;
}
