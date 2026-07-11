using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace Navius.Wpf.Ui.Sidebar;

/// <summary>
/// A single nav row: an icon slot plus a label (the inherited Content) plus an active-state Accent
/// indicator. Derives ButtonBase directly (its own control, not NaviusButton) so it can carry
/// <see cref="Icon"/>/<see cref="IsActive"/> and collapse cleanly to an icon-only rail item, matching
/// the codebase's precedent of dedicated item types for composite anatomies (NaviusToggleGroupItem,
/// NaviusButtonGroupItem).
/// </summary>
public class NaviusSidebarItem : ButtonBase
{
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon), typeof(object), typeof(NaviusSidebarItem), new PropertyMetadata(null));

    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive), typeof(bool), typeof(NaviusSidebarItem), new PropertyMetadata(false, OnIsActiveChanged));

    static NaviusSidebarItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusSidebarItem),
            new FrameworkPropertyMetadata(typeof(NaviusSidebarItem)));
    }

    /// <summary>Icon content, shown at a fixed width whether the sidebar is collapsed or expanded.</summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>True for the current page/section; renders the Accent indicator bar and wash.</summary>
    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusSidebarItemAutomationPeer(this);

    /// <summary>
    /// Routes a UIA Invoke through ButtonBase.OnClick so it both raises Click and executes the bound
    /// Command, matching how the native ButtonAutomationPeer activates a button.
    /// </summary>
    internal void AutomationInvoke() => OnClick();

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
        {
            AutomationProperties.SetItemStatus(d, "current");
        }
        else
        {
            d.ClearValue(AutomationProperties.ItemStatusProperty);
        }
    }
}

/// <summary>
/// Reports role="button" plus UIA InvokePattern for this bare ButtonBase item, so its activation
/// surface (and the ItemStatus="current" the control sets on the active row) are actually reachable
/// by assistive tech; deriving ButtonBase directly skips ButtonAutomationPeer, leaving no control
/// type or invoke pattern otherwise. Same shape as NaviusBreadcrumbItem's peer; Invoke honors
/// IsEnabled and throws ElementNotEnabledException when disabled, the repo convention shared with
/// NaviusCollapsibleTriggerAutomationPeer / NaviusNumberFieldAutomationPeer.
/// </summary>
internal sealed class NaviusSidebarItemAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
{
    private readonly NaviusSidebarItem _owner;

    public NaviusSidebarItemAutomationPeer(NaviusSidebarItem owner) : base(owner) => _owner = owner;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

    protected override string GetClassNameCore() => nameof(NaviusSidebarItem);

    public override object? GetPattern(PatternInterface patternInterface) =>
        patternInterface == PatternInterface.Invoke ? this : base.GetPattern(patternInterface);

    void IInvokeProvider.Invoke()
    {
        if (!_owner.IsEnabled)
        {
            throw new ElementNotEnabledException();
        }

        // The UIA IInvokeProvider.Invoke contract requires this call to return immediately, so queue
        // the activation onto the owner's dispatcher rather than running it inline, matching WPF's
        // native ButtonAutomationPeer. This keeps the UIA client from blocking when activation opens
        // a modal dialog or does other synchronous work.
        _owner.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(_owner.AutomationInvoke));
    }
}
