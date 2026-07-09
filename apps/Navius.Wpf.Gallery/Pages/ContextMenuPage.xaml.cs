using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Interaction logic for ContextMenuPage.xaml
/// </summary>
public partial class ContextMenuPage : UserControl
{
    public ContextMenuPage()
    {
        InitializeComponent();
    }

    private void OnOpenAtClick(object sender, RoutedEventArgs e) =>
        Popup.RequestOpenAt(OpenAtButton, new Point(0, OpenAtButton.ActualHeight));
}
