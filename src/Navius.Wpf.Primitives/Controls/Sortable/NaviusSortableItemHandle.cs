using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.Sortable;

/// <summary>
/// Optional drag-handle marker placed inside a <see cref="NaviusSortableItem"/>'s content. When one
/// or more of these are present, pointer drag is scoped to start only from a handle (the contract's
/// <c>[data-navius-sortable-handle]</c> selector); with no handle, the whole item is the drag
/// source. Keyboard reordering always acts on the whole item, so the handle is marked
/// aria-hidden (AccessibilityView.Raw here) to keep it out of the assistive-tech tree.
///
/// A plain lookless ContentControl: it carries no behavior of its own beyond being detectable in
/// the item's visual tree; the owning <see cref="NaviusSortableItem"/> checks mouse-down ancestry
/// against it.
/// </summary>
public class NaviusSortableItemHandle : ContentControl
{
    static NaviusSortableItemHandle()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusSortableItemHandle), new FrameworkPropertyMetadata(typeof(NaviusSortableItemHandle)));
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusSortableItemHandleAutomationPeer(this);
}

/// <summary>
/// aria-hidden equivalent: keeps the mouse-only handle out of the UIA tree (Raw view) since keyboard
/// reordering operates on the whole item, not the handle.
/// </summary>
public class NaviusSortableItemHandleAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusSortableItemHandleAutomationPeer(NaviusSortableItemHandle owner) : base(owner)
    {
    }

    protected override string GetClassNameCore() => nameof(NaviusSortableItemHandle);

    protected override bool IsControlElementCore() => false;

    protected override bool IsContentElementCore() => false;
}
