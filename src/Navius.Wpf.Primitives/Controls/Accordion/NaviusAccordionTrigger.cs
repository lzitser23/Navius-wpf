using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

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

    void IInvokeProvider.Invoke() => RaiseClick();

    void IExpandCollapseProvider.Expand()
    {
        if (!_owner.IsPanelOpen)
        {
            RaiseClick();
        }
    }

    void IExpandCollapseProvider.Collapse()
    {
        if (_owner.IsPanelOpen)
        {
            RaiseClick();
        }
    }

    private void RaiseClick() => _owner.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, _owner));
}
