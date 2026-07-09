using System.Windows.Automation.Peers;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Overrides IsEnabledCore so a soft-disabled NaviusButton (Disabled=true with
/// FocusableWhenDisabled=true) reports as disabled to UIA while remaining natively
/// focusable/enabled -- the WPF analog of the contract's aria-disabled="true" rendered only in
/// the focusable-while-disabled mode (see docs/parity/button.md "Accessibility").
/// </summary>
public class NaviusButtonAutomationPeer : ButtonAutomationPeer
{
    public NaviusButtonAutomationPeer(NaviusButton owner) : base(owner)
    {
    }

    private NaviusButton Button => (NaviusButton)Owner;

    protected override bool IsEnabledCore() => !Button.IsSoftDisabled && base.IsEnabledCore();
}
