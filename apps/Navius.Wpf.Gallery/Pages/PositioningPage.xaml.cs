using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Interaction logic for PositioningPage.xaml. Standalone demo of NaviusAnchoredPopup;
/// not wired into MainWindow navigation.
/// </summary>
public partial class PositioningPage : UserControl
{
    public PositioningPage()
    {
        InitializeComponent();
    }

    private void OnToggleTop(object sender, RoutedEventArgs e) => TopPopup.IsOpen = !TopPopup.IsOpen;

    private void OnToggleBottom(object sender, RoutedEventArgs e) => BottomPopup.IsOpen = !BottomPopup.IsOpen;

    private void OnToggleLeft(object sender, RoutedEventArgs e) => LeftPopup.IsOpen = !LeftPopup.IsOpen;

    private void OnToggleRight(object sender, RoutedEventArgs e) => RightPopup.IsOpen = !RightPopup.IsOpen;

    private void OnToggleEdge(object sender, RoutedEventArgs e) => EdgePopup.IsOpen = !EdgePopup.IsOpen;
}
