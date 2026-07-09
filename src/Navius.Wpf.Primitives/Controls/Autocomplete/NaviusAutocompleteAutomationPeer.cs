using System.Windows.Automation.Peers;

namespace Navius.Wpf.Primitives.Controls.Autocomplete;

/// <summary>
/// Minimal automation peer: reports the root as a ComboBox and exposes the current
/// <see cref="NaviusAutocompleteBase.Value"/> as its name. It deliberately does NOT model the web's
/// <c>aria-activedescendant</c> virtual focus: WPF/UIA has no first-class virtual-focus primitive
/// (the contract's own Open Questions flag this as the biggest translation gap), so a faithful
/// custom peer is documented as explicitly deferred in docs/parity/autocomplete.md rather than
/// half-built here. The result-count "role=status" announcement is delivered instead via
/// <c>RaiseNotificationEvent</c> on this peer (see <see cref="NaviusAutocompleteBase"/>).
/// </summary>
public class NaviusAutocompleteAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusAutocompleteAutomationPeer(NaviusAutocompleteBase owner)
        : base(owner)
    {
    }

    private NaviusAutocompleteBase Autocomplete => (NaviusAutocompleteBase)Owner;

    protected override string GetClassNameCore() => nameof(NaviusAutocompleteBase);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ComboBox;

    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        if (!string.IsNullOrEmpty(name))
        {
            return name;
        }

        return Autocomplete.Value ?? string.Empty;
    }
}
