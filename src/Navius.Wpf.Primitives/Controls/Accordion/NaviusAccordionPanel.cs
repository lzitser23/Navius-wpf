using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls.Internal;

namespace Navius.Wpf.Primitives.Controls.Accordion;

/// <summary>
/// Tier B. Shares its open/close height animation with NaviusCollapsiblePanel via
/// PanelHeightAnimator (see that class), rather than the two families literally composing
/// one control: Accordion's panel additionally needs role="region" automation semantics
/// and per-item disabled/index cascading that would be awkward to thread through
/// Collapsible's simpler two-state context, so only the animation logic is shared, not
/// the control itself (see accordion.md's WPF implementation notes for the full
/// rationale).
///
/// KeepMounted behaves identically to NaviusCollapsiblePanel: content is cached and
/// cleared once the close animation finishes (default), or left in place with only
/// Visibility toggling when KeepMounted is true.
/// </summary>
public class NaviusAccordionPanel : ContentControl
{
    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen),
        typeof(bool),
        typeof(NaviusAccordionPanel),
        new PropertyMetadata(false, OnIsOpenChanged));

    public static readonly DependencyProperty KeepMountedProperty = DependencyProperty.Register(
        nameof(KeepMounted),
        typeof(bool),
        typeof(NaviusAccordionPanel),
        new PropertyMetadata(false));

    private object? _cachedContent;

    static NaviusAccordionPanel()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusAccordionPanel),
            new FrameworkPropertyMetadata(typeof(NaviusAccordionPanel)));
    }

    public NaviusAccordionPanel()
    {
        Visibility = Visibility.Collapsed;
    }

    /// <summary>Set by the ancestor NaviusAccordion; mirrors the contract's data-open/data-closed.</summary>
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public bool KeepMounted
    {
        get => (bool)GetValue(KeepMountedProperty);
        set => SetValue(KeepMountedProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new NaviusAccordionPanelAutomationPeer(this);

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusAccordionPanel)d).ApplyOpenState((bool)e.NewValue);

    private void ApplyOpenState(bool isOpen)
    {
        if (isOpen)
        {
            if (Content is null && _cachedContent is not null)
            {
                Content = _cachedContent;
                _cachedContent = null;
            }

            PanelHeightAnimator.Open(this);
        }
        else
        {
            PanelHeightAnimator.Close(this, onCollapsed: KeepMounted ? null : UnmountContent);
        }
    }

    private void UnmountContent()
    {
        _cachedContent = Content;
        Content = null;
    }
}

/// <summary>
/// Approximates the contract's role="region": WPF's AutomationControlType has no direct
/// "region" value, so this reports Group (the closest generic UIA control type also used
/// by NaviusCollapsible's root), documented as a known gap rather than a false claim of an
/// exact landmark match.
/// </summary>
internal sealed class NaviusAccordionPanelAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusAccordionPanelAutomationPeer(NaviusAccordionPanel owner) : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override string GetClassNameCore() => nameof(NaviusAccordionPanel);
}
