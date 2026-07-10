using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls.Primitives;

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
