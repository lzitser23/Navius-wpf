using System.Windows.Controls;
using System.Windows;
using Navius.Wpf.Primitives.Controls.Drawer;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Demonstrates NaviusDrawer docked to each of its four sides, sliding in/out on open/close.
/// </summary>
public partial class DrawerPage : UserControl
{
    public DrawerPage()
    {
        InitializeComponent();
    }

    private void OnOpenLeftClick(object sender, RoutedEventArgs e) => OpenFrom(NaviusDrawerSide.Left);

    private void OnOpenRightClick(object sender, RoutedEventArgs e) => OpenFrom(NaviusDrawerSide.Right);

    private void OnOpenTopClick(object sender, RoutedEventArgs e) => OpenFrom(NaviusDrawerSide.Top);

    private void OnOpenBottomClick(object sender, RoutedEventArgs e) => OpenFrom(NaviusDrawerSide.Bottom);

    private void OpenFrom(NaviusDrawerSide side)
    {
        MyDrawer.Side = side;
        MyDrawer.Open();
    }
}
