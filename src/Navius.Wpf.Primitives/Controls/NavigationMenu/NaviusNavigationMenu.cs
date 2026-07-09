using System;
using System.Windows;

namespace Navius.Wpf.Primitives.Controls.NavigationMenu;

/// <summary>
/// Tier B (custom lookless control): the root "nav" landmark. See
/// <see cref="NavigationMenuHostBase"/> for the shared open-value/hover-delay/dismiss machinery.
///
/// <see cref="UseSharedViewport"/> is a stub for the contract's shared morphing viewport (one
/// popup that resizes/re-anchors as the active item changes): this M2 port only implements the
/// per-item popup mode (each Content owns its own NaviusAnchoredPopup). Setting
/// UseSharedViewport=true throws immediately, an honest failure instead of a broken imitation of
/// the morph animation; it is tracked as an M3+ follow-up in the parity doc's WPF notes.
/// </summary>
public class NaviusNavigationMenu : NavigationMenuHostBase
{
    public static readonly DependencyProperty UseSharedViewportProperty = DependencyProperty.Register(
        nameof(UseSharedViewport), typeof(bool), typeof(NaviusNavigationMenu),
        new PropertyMetadata(false, OnUseSharedViewportChanged));

    static NaviusNavigationMenu()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusNavigationMenu),
            new FrameworkPropertyMetadata(typeof(NaviusNavigationMenu)));
    }

    /// <summary>
    /// When true, requests the shared/morphing-viewport layout. Not implemented in this M2 port
    /// (see class remarks); setting it throws <see cref="NotSupportedException"/>.
    /// </summary>
    public bool UseSharedViewport
    {
        get => (bool)GetValue(UseSharedViewportProperty);
        set => SetValue(UseSharedViewportProperty, value);
    }

    private static void OnUseSharedViewportChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
        {
            throw new NotSupportedException(
                "NaviusNavigationMenu.UseSharedViewport=true (the shared morphing viewport) is not " +
                "implemented in this WPF port. Use the default per-item popup mode " +
                "(UseSharedViewport=false); see docs/parity/navigation-menu.md \"WPF implementation notes\".");
        }
    }
}
