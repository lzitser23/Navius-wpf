using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Navius.Wpf.Primitives.Controls.Internal;

namespace Navius.Wpf.Primitives.Controls.Collapsible;

/// <summary>
/// Tier B (lookless custom control). The contract splits Root/Trigger/Panel into three
/// separately addressable elements sharing state through a cascaded CollapsibleContext.
/// WPF's closest native analog (Expander) fuses header and content into one control and
/// cannot preserve that three-part split, so this instead follows the RadioGroup family's
/// pattern: a lookless ContentControl root that owns Open state and discovers its
/// Trigger/Panel descendants via the logical tree (see LogicalTreeWalker) to wire them
/// together, rather than requiring explicit registration.
///
/// Disabled is not reimplemented as its own named parameter: this reuses the root's native
/// IsEnabled. WPF's IsEnabled does not, however, automatically cascade through a
/// ContentControl's logical Content the way it does through a Panel's Children, so
/// SyncDescendants explicitly pushes IsEnabled down onto the Trigger (mirroring how
/// Open/IsPanelOpen are already pushed down), matching the contract's "Disabled blocks
/// RequestSetOpenAsync."
///
/// HiddenUntilFound (the contract's browser in-page-find integration) has no WPF analog
/// and is dropped entirely for this port, per the parity doc's own open question.
/// </summary>
public class NaviusCollapsible : ContentControl
{
    public static readonly DependencyProperty OpenProperty = DependencyProperty.Register(
        nameof(Open),
        typeof(bool),
        typeof(NaviusCollapsible),
        new PropertyMetadata(false, OnOpenChanged));

    public static readonly RoutedEvent OpenChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(OpenChanged),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(NaviusCollapsible));

    static NaviusCollapsible()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusCollapsible),
            new FrameworkPropertyMetadata(typeof(NaviusCollapsible)));
    }

    public NaviusCollapsible()
    {
        AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnDescendantClick));

        // UIElement.IsEnabled does not automatically cascade through a ContentControl's
        // logical Content (only Panel.Children gets that for free), so the Trigger's
        // enabled state is pushed down explicitly here, matching how Open/IsPanelOpen are
        // already pushed down in SyncDescendants.
        IsEnabledChanged += (_, _) => SyncDescendants();
    }

    public bool Open
    {
        get => (bool)GetValue(OpenProperty);
        set => SetValue(OpenProperty, value);
    }

    public event RoutedEventHandler OpenChanged
    {
        add => AddHandler(OpenChangedEvent, value);
        remove => RemoveHandler(OpenChangedEvent, value);
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        SyncDescendants();
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new NaviusCollapsibleAutomationPeer(this);

    private static void OnOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var root = (NaviusCollapsible)d;
        root.SyncDescendants();
        root.RaiseEvent(new RoutedEventArgs(OpenChangedEvent, root));
    }

    private void OnDescendantClick(object sender, RoutedEventArgs e)
    {
        if (!IsEnabled || e.OriginalSource is not NaviusCollapsibleTrigger trigger || !trigger.IsEnabled)
        {
            return;
        }

        Open = !Open;
    }

    private void SyncDescendants()
    {
        foreach (var trigger in LogicalTreeWalker.Descendants<NaviusCollapsibleTrigger>(this))
        {
            trigger.IsPanelOpen = Open;
            trigger.IsEnabled = IsEnabled;
        }

        foreach (var panel in LogicalTreeWalker.Descendants<NaviusCollapsiblePanel>(this))
        {
            panel.IsOpen = Open;
        }
    }
}

/// <summary>
/// Root carries no data-* state attributes at all per the contract ("Base UI
/// Collapsible.Root exposes no data-* state attributes"); it is a plain grouping element,
/// so the automation control type is Group rather than anything expand/collapse-flavored
/// (that lives on the Trigger, see NaviusCollapsibleTrigger's peer).
/// </summary>
internal sealed class NaviusCollapsibleAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusCollapsibleAutomationPeer(NaviusCollapsible owner) : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override string GetClassNameCore() => nameof(NaviusCollapsible);
}
