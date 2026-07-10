using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.Accordion;

/// <summary>
/// Tier B. A plain lookless grouping element identifying one section; the ancestor
/// NaviusAccordion discovers items via the logical tree and pushes computed open state
/// down into their Trigger/Panel descendants (see NaviusAccordion.SyncDescendants), the
/// same "root owns state, descendants are discovered rather than registered" shape as
/// NaviusCollapsible/NaviusRadioGroup.
///
/// Disabled is not reimplemented as its own property: this item's native IsEnabled is
/// reused directly, but WPF's IsEnabled does NOT automatically cascade through a
/// ContentControl's logical Content (verified empirically; see collapsible.md's parity
/// notes). NaviusAccordion.SyncDescendants explicitly pushes this item's IsEnabled down
/// onto its Trigger (and re-subscribes to IsEnabledChanged), matching the contract's
/// per-item Disabled cascading.
/// </summary>
public class NaviusAccordionItem : ContentControl
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(string),
        typeof(NaviusAccordionItem),
        new PropertyMetadata(string.Empty));

    /// <summary>Zero-based DOM-order index, mirroring the contract's data-index; set by the ancestor NaviusAccordion.</summary>
    public static readonly DependencyProperty IndexProperty = DependencyProperty.Register(
        nameof(Index),
        typeof(int),
        typeof(NaviusAccordionItem),
        new PropertyMetadata(0));

    static NaviusAccordionItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusAccordionItem),
            new FrameworkPropertyMetadata(typeof(NaviusAccordionItem)));
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int Index
    {
        get => (int)GetValue(IndexProperty);
        set => SetValue(IndexProperty, value);
    }
}
