using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls.Internal;

namespace Navius.Wpf.Primitives.Controls.Fieldset;

/// <summary>
/// Tier A (derives from the native disabled-cascade behavior, not literally GroupBox).
/// WPF's IsEnabled already inherits down the visual/logical tree the same way the web
/// contract's native &lt;fieldset disabled&gt; "disables every contained control for free",
/// so Disabled maps straight onto IsEnabled instead of reimplementing propagation. A plain
/// ContentControl (not GroupBox) is used because GroupBox's built-in ControlTemplate pins
/// the header inline with the border, whereas the contract's Legend is deliberately a
/// free-floating element "for positioning freedom" -- ChildContent (ordinary WPF Content)
/// already gives that freedom without a re-templated GroupBox.
///
/// Contract delta: the web NaviusFieldsetLegend code comment claims it is "automatically
/// associated as the fieldset's label" but wires no id/aria-labelledby to make that true
/// (see fieldset.md Open Questions). Rather than porting that gap, this control actively
/// wires AutomationProperties.LabeledBy to the first NaviusFieldsetLegend descendant it
/// finds, so the WPF port's accessible-name association is real, per the "APG + native WPF
/// over suspicious contract lines" tiebreak.
/// </summary>
public class NaviusFieldset : ContentControl
{
    public static readonly DependencyProperty DisabledProperty = DependencyProperty.Register(
        nameof(Disabled),
        typeof(bool),
        typeof(NaviusFieldset),
        new PropertyMetadata(false, OnDisabledChanged));

    static NaviusFieldset()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusFieldset),
            new FrameworkPropertyMetadata(typeof(NaviusFieldset)));
    }

    public NaviusFieldset()
    {
        IsEnabled = !Disabled;
    }

    public bool Disabled
    {
        get => (bool)GetValue(DisabledProperty);
        set => SetValue(DisabledProperty, value);
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        WireLegend();
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusFieldsetAutomationPeer(this);

    private static void OnDisabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var fieldset = (NaviusFieldset)d;

        // Sets local IsEnabled; WPF's own IsEnabled coercion still ANDs this against any
        // ancestor's disabled state, so an outer disabled scope still wins.
        fieldset.IsEnabled = !(bool)e.NewValue;
    }

    private void WireLegend()
    {
        var legend = LogicalTreeWalker.Descendants<NaviusFieldsetLegend>(this).FirstOrDefault();
        if (legend is not null)
        {
            AutomationProperties.SetLabeledBy(this, legend);
        }
    }
}

internal sealed class NaviusFieldsetAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusFieldsetAutomationPeer(NaviusFieldset owner) : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override string GetClassNameCore() => nameof(NaviusFieldset);
}
