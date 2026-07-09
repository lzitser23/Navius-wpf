using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Surfaces the contract's aria-valuetext (GetValueLabel or the default rounded percentage) via
/// ItemStatus, mirroring NaviusProgressAutomationPeer. WPF's ProgressBarAutomationPeer already
/// reports its IRangeValueProvider as read-only (SetValue throws), matching the contract's
/// "Meter is a read-only display, no keyboard, no focus management". AutomationControlType stays
/// the inherited ProgressBar value, since WPF has no distinct Meter automation type -- the same
/// gap the contract's own "Open questions" section already flags.
/// </summary>
public class NaviusMeterAutomationPeer : ProgressBarAutomationPeer
{
    public NaviusMeterAutomationPeer(NaviusMeter owner) : base(owner)
    {
    }

    private NaviusMeter Meter => (NaviusMeter)Owner;

    protected override string GetItemStatusCore() => Meter.FormatValueText();
}
