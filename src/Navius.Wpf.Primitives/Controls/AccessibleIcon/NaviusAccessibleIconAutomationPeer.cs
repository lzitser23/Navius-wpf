using System.Windows.Automation.Peers;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Maps NaviusAccessibleIcon's "wrap decorative content, expose one accessible name" contract
/// onto UIA. When Label is set, the wrapper reports as an AutomationControlType.Image whose name
/// is Label. When Label is null, the wrapper is excluded from both the control and content views
/// of the UIA tree (IsControlElementCore/IsContentElementCore both false) so it does not surface
/// as an unnamed, decorative node -- the WPF analog of the contract never rendering a hidden
/// label span in that case (see docs/parity/accessible-icon.md "WPF strategy").
/// </summary>
public class NaviusAccessibleIconAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusAccessibleIconAutomationPeer(NaviusAccessibleIcon owner) : base(owner)
    {
    }

    private NaviusAccessibleIcon Icon => (NaviusAccessibleIcon)Owner;

    protected override string GetNameCore() => Icon.Label ?? string.Empty;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Image;

    protected override string GetClassNameCore() => nameof(NaviusAccessibleIcon);

    protected override bool IsControlElementCore() => !string.IsNullOrEmpty(Icon.Label);

    protected override bool IsContentElementCore() => !string.IsNullOrEmpty(Icon.Label);
}
