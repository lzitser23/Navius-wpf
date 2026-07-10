using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Media;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Maps NaviusRatingItem to role="radio": AutomationControlType.RadioButton plus
/// ISelectionItemProvider, mirroring the contract's roving-tabindex radio-set model.
/// GetNameCore resolves the contract's asymmetric announcement rule: the checked star announces
/// its real (possibly fractional) value, every other star announces the whole value it would
/// select.
/// </summary>
public class NaviusRatingItemAutomationPeer : FrameworkElementAutomationPeer, ISelectionItemProvider
{
    public NaviusRatingItemAutomationPeer(NaviusRatingItem owner) : base(owner)
    {
    }

    private NaviusRatingItem Item => (NaviusRatingItem)Owner;

    private NaviusRating? Group => FindGroup(Item);

    public bool IsSelected =>
        Group is { } group && group.Value is { } value && NaviusRatingMath.FocusIndex(value, group.Max) == Item.Index;

    public IRawElementProviderSimple? SelectionContainer
    {
        get
        {
            if (Group is not { } group)
            {
                return null;
            }

            var peer = FromElement(group) ?? UIElementAutomationPeer.CreatePeerForElement(group);
            return peer is null ? null : ProviderFromPeer(peer);
        }
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.RadioButton;

    protected override string GetClassNameCore() => nameof(NaviusRatingItem);

    protected override string GetNameCore()
    {
        var explicitName = base.GetNameCore();
        if (!string.IsNullOrEmpty(explicitName))
        {
            return explicitName;
        }

        if (Group is not { } group)
        {
            return string.Empty;
        }

        var announced = IsSelected && group.Value is { } value ? value : Item.Index;
        return group.Label?.Invoke(announced) ?? NaviusRating.DefaultLabel(announced);
    }

    public void AddToSelection() => Select();

    public void RemoveFromSelection()
    {
        // No-op: a rating star cannot be individually deselected via automation; clearing the
        // whole group's value (AllowClear) is a group-level operation, not a per-item one.
    }

    public void Select() => Group?.SelectItem(Item);

    private static NaviusRating? FindGroup(DependencyObject start)
    {
        var current = VisualTreeHelper.GetParent(start);
        while (current is not null)
        {
            if (current is NaviusRating rating)
            {
                return rating;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
