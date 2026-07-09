using System.Windows.Automation.Peers;

namespace Navius.Wpf.Primitives.Controls.Sortable;

/// <summary>
/// Maps <see cref="NaviusSortable"/> to role="list": AutomationControlType.List. Announcements are
/// raised as UIA notification events from the control itself (see NaviusSortable.Announce), which is
/// why this peer carries no LiveSetting of its own.
/// </summary>
public class NaviusSortableAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusSortableAutomationPeer(NaviusSortable owner) : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;

    protected override string GetClassNameCore() => nameof(NaviusSortable);
}

/// <summary>
/// Maps <see cref="NaviusSortableItem"/> to role="listitem": AutomationControlType.ListItem with the
/// contract's aria-roledescription="sortable item" surfaced via GetLocalizedControlType, and the
/// accessible name resolved from Label/Value. PositionInSet/SizeOfSet are pushed onto the item as
/// attached properties by the owning NaviusSortable (see RefreshItemStates), so the default peer
/// reports them without extra work here.
/// </summary>
public class NaviusSortableItemAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusSortableItemAutomationPeer(NaviusSortableItem owner) : base(owner)
    {
    }

    private NaviusSortableItem Item => (NaviusSortableItem)Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;

    protected override string GetClassNameCore() => nameof(NaviusSortableItem);

    protected override string GetLocalizedControlTypeCore() => "sortable item";

    protected override string GetNameCore()
    {
        var explicitName = base.GetNameCore();
        return string.IsNullOrEmpty(explicitName) ? Item.AccessibleLabel : explicitName;
    }
}
