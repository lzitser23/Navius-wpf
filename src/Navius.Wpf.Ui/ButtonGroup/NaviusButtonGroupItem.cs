using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace Navius.Wpf.Ui.ButtonGroup;

/// <summary>
/// A single segment of a <see cref="NaviusButtonGroup"/>. Derives from ButtonBase directly (not
/// Navius.Wpf.Primitives' NaviusButton, whose template hardcodes the full token radius) because a
/// segment stays a plain square: the rounded silhouette is masked in by the clip on the group's own
/// container, and the only per-item variation is the hairline BorderThickness that the
/// <see cref="NaviusButtonGroup.IsLastItemProperty"/>/Orientation style triggers in
/// Themes/ButtonGroup.xaml drive.
/// </summary>
public class NaviusButtonGroupItem : ButtonBase
{
    static NaviusButtonGroupItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusButtonGroupItem),
            new FrameworkPropertyMetadata(typeof(NaviusButtonGroupItem)));
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusButtonGroupItemAutomationPeer(this);

    /// <summary>
    /// Routes a UIA Invoke through ButtonBase.OnClick so it both raises Click and executes the bound
    /// Command, matching how the native ButtonAutomationPeer activates a button.
    /// </summary>
    internal void AutomationInvoke() => OnClick();
}

/// <summary>
/// Reports role="button" plus UIA InvokePattern for this bare ButtonBase item, which otherwise
/// exposes no control type or invoke surface at all (deriving ButtonBase directly, not the native
/// Button, skips ButtonAutomationPeer). Same shape as NaviusBreadcrumbItem's peer; Invoke honors
/// IsEnabled and throws ElementNotEnabledException when disabled, the repo convention shared with
/// NaviusCollapsibleTriggerAutomationPeer / NaviusNumberFieldAutomationPeer.
/// </summary>
internal sealed class NaviusButtonGroupItemAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
{
    private readonly NaviusButtonGroupItem _owner;

    public NaviusButtonGroupItemAutomationPeer(NaviusButtonGroupItem owner) : base(owner) => _owner = owner;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

    protected override string GetClassNameCore() => nameof(NaviusButtonGroupItem);

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
