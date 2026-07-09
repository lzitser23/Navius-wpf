using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.ContextMenu;

/// <summary>
/// Tier B: a lookless surface (ContentControl, matching the contract's plain `&lt;div&gt;`
/// with no ARIA) that wraps arbitrary child content and hosts a NaviusContextMenuPopup as
/// its native ContextMenu. Assigning ContextMenu is all that's needed for right-click,
/// Shift+F10, and the keyboard Apps/Menu key to open the popup - WPF's ContextMenuService
/// already implements the contract's "open anchored at the pointer, or at the trigger's own
/// rect for the keyboard path" behavior for free.
///
/// Not reimplemented: long-press-to-open on touch has no ContextMenuService equivalent
/// (would need a custom TouchDown/Stylus timer); see docs/parity/context-menu.md.
/// </summary>
public class NaviusContextMenuTrigger : ContentControl
{
    public static readonly DependencyProperty MenuProperty = DependencyProperty.Register(
        nameof(Menu),
        typeof(NaviusContextMenuPopup),
        typeof(NaviusContextMenuTrigger),
        new PropertyMetadata(null, OnMenuChanged));

    /// <summary>Suppresses context-menu open on right-click, long-press, and the keyboard menu key.</summary>
    public static readonly DependencyProperty DisabledProperty = DependencyProperty.Register(
        nameof(Disabled),
        typeof(bool),
        typeof(NaviusContextMenuTrigger),
        new PropertyMetadata(false, OnDisabledChanged));

    static NaviusContextMenuTrigger()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusContextMenuTrigger),
            new FrameworkPropertyMetadata(typeof(NaviusContextMenuTrigger)));
    }

    public NaviusContextMenuPopup? Menu
    {
        get => (NaviusContextMenuPopup?)GetValue(MenuProperty);
        set => SetValue(MenuProperty, value);
    }

    public bool Disabled
    {
        get => (bool)GetValue(DisabledProperty);
        set => SetValue(DisabledProperty, value);
    }

    private static void OnMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusContextMenuTrigger)d).ContextMenu = e.NewValue as System.Windows.Controls.ContextMenu;

    private static void OnDisabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ContextMenuService.SetIsEnabled(d, !(bool)e.NewValue);
}
