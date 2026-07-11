using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Navius.Wpf.Primitives.Controls.Accordion;

/// <summary>
/// Tier A for the button itself (derives from the native Button, so Space/Enter already
/// invoke Click for free); Tier B for open-state reflection: <see cref="IsPanelOpen"/> is
/// set by the ancestor NaviusAccordion (mirroring the contract's data-panel-open, itself
/// derived from AccordionContext.IsOpen), the same "ancestor pushes state down" shape as
/// NaviusCollapsibleTrigger.IsPanelOpen. The ancestor listens for the bubbled
/// ButtonBase.ClickEvent to know a trigger fired, rather than this type owning its own
/// toggle logic or a reference back to the item/context.
/// </summary>
public class NaviusAccordionTrigger : Button
{
    public static readonly DependencyProperty IsPanelOpenProperty = DependencyProperty.Register(
        nameof(IsPanelOpen),
        typeof(bool),
        typeof(NaviusAccordionTrigger),
        new PropertyMetadata(false));

    static NaviusAccordionTrigger()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusAccordionTrigger),
            new FrameworkPropertyMetadata(typeof(NaviusAccordionTrigger)));
    }

    /// <summary>Mirrors the contract's data-panel-open; set by the ancestor NaviusAccordion, not by this control.</summary>
    public bool IsPanelOpen
    {
        get => (bool)GetValue(IsPanelOpenProperty);
        set => SetValue(IsPanelOpenProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new NaviusAccordionTriggerAutomationPeer(this);

    /// <summary>
    /// Routes a UIA activation through ButtonBase.OnClick so it both raises Click (the bubbled
    /// ButtonBase.ClickEvent the ancestor NaviusAccordion listens for to toggle the panel) and
    /// executes the bound Command. A bare RaiseEvent(ClickEvent) raises Click but skips the command
    /// execution that lives in OnClick, so the Command would never run from UIA. Called by the peer.
    /// </summary>
    internal void AutomationInvoke() => OnClick();
}

/// <summary>
/// Exposes both UIA InvokePattern (native Button behavior) and ExpandCollapsePattern
/// (the contract's aria-expanded), the same dual-pattern shape as
/// NaviusCollapsibleTrigger's peer and WPF's own ExpanderAutomationPeer.
/// </summary>
internal sealed class NaviusAccordionTriggerAutomationPeer
    : FrameworkElementAutomationPeer, IInvokeProvider, IExpandCollapseProvider
{
    private readonly NaviusAccordionTrigger _owner;

    public NaviusAccordionTriggerAutomationPeer(NaviusAccordionTrigger owner) : base(owner) =>
        _owner = owner;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

    protected override string GetClassNameCore() => nameof(NaviusAccordionTrigger);

    public override object? GetPattern(PatternInterface patternInterface) =>
        patternInterface is PatternInterface.Invoke or PatternInterface.ExpandCollapse
            ? this
            : base.GetPattern(patternInterface);

    ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState =>
        _owner.IsPanelOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    void IInvokeProvider.Invoke()
    {
        ThrowIfDisabled();
        QueueInvoke();
    }

    void IExpandCollapseProvider.Expand()
    {
        ThrowIfDisabled();
        if (!_owner.IsPanelOpen)
        {
            _owner.AutomationInvoke();
        }
    }

    void IExpandCollapseProvider.Collapse()
    {
        ThrowIfDisabled();
        if (_owner.IsPanelOpen)
        {
            _owner.AutomationInvoke();
        }
    }

    // A disabled trigger must not be operable through UIA, matching NaviusNumberFieldAutomationPeer,
    // which throws when its owner is not enabled. IsEnabledCore already reports the disabled state
    // (inherited IsEnabled), but the pattern providers must also refuse to act on it. The guard stays
    // synchronous, as does the IsPanelOpen state check above, so a no-op Expand/Collapse queues nothing.
    private void ThrowIfDisabled()
    {
        if (!_owner.IsEnabled)
        {
            throw new ElementNotEnabledException();
        }
    }

    // UIA Invoke must return immediately, so queue activation onto the owner's dispatcher, matching
    // WPF's native peers. Expand/Collapse are deliberately synchronous: their provider contract requires
    // the requested state to be reached before they return. Both paths route through OnClick so the bound
    // Command executes (a bare RaiseEvent would not).
    private void QueueInvoke() =>
        _owner.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(_owner.AutomationInvoke));
}
