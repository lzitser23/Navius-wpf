using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.Tabs;

/// <summary>
/// Tier A: derives from the native TabItem, unifying the contract's separate
/// NaviusTabsTab (trigger) and NaviusTabsPanel (content) into one object, since
/// TabItem.Header (the trigger's rendered content) and TabItem.Content (the panel's
/// rendered content) already live together. TabItemAutomationPeer already implements
/// ISelectionItemProvider / role="tab" and TabItem.IsEnabled already maps to the
/// contract's per-tab Disabled, so no custom automation peer is needed.
/// </summary>
public class NaviusTabItem : TabItem
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(string),
        typeof(NaviusTabItem),
        new PropertyMetadata(string.Empty));

    static NaviusTabItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusTabItem),
            new FrameworkPropertyMetadata(typeof(NaviusTabItem)));
    }

    /// <summary>Identifies this tab; used by NaviusTabs to derive/sync its Value.</summary>
    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
}
