using System;
using System.Windows;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls.NavigationMenu;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Demonstrates the NavigationMenu family's per-item popup mode: hover/click to open a
/// Trigger+Content pair, a plain top-level Link with no disclosure, and the
/// UseSharedViewport stub throwing NotSupportedException.
/// </summary>
public partial class NavigationMenuPage : UserControl
{
    public NavigationMenuPage()
    {
        InitializeComponent();
    }

    private void OnSharedViewportClick(object sender, RoutedEventArgs e)
    {
        try
        {
            _ = new NaviusNavigationMenu { UseSharedViewport = true };
        }
        catch (NotSupportedException ex)
        {
            StatusText.Text = ex.Message;
        }
    }
}
