using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Maps NaviusRating to role="radiogroup": AutomationControlType.Group (the same choice
/// NaviusRadioGroupAutomationPeer makes, since WPF has no built-in radiogroup-of-buttons peer)
/// plus ISelectionProvider so assistive tech can enumerate the checked star.
/// </summary>
public class NaviusRatingAutomationPeer : FrameworkElementAutomationPeer, ISelectionProvider
{
    public NaviusRatingAutomationPeer(NaviusRating owner) : base(owner)
    {
    }

    private NaviusRating Rating => (NaviusRating)Owner;

    public bool CanSelectMultiple => false;

    public bool IsSelectionRequired => Rating.Required;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override string GetClassNameCore() => nameof(NaviusRating);

    public IRawElementProviderSimple[] GetSelection()
    {
        if (Rating.Value is null)
        {
            return Array.Empty<IRawElementProviderSimple>();
        }

        var index = NaviusRatingMath.FocusIndex(Rating.Value, Rating.Max);
        var item = Rating.GetItem(index);
        if (item is null)
        {
            return Array.Empty<IRawElementProviderSimple>();
        }

        var peer = FromElement(item) ?? CreatePeerForElement(item);
        return peer is null ? Array.Empty<IRawElementProviderSimple>() : new[] { ProviderFromPeer(peer) };
    }
}
