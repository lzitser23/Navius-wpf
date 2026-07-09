using System.Windows.Controls;
using System.Windows;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Demonstrates NaviusDialog: a modal, focus-trapped, Esc/outside-click-dismissible dialog whose
/// body buttons request close via the shared NaviusOverlaySurfaceBase.CloseCommand.
/// </summary>
public partial class DialogPage : UserControl
{
    public DialogPage()
    {
        InitializeComponent();
    }

    private void OnOpenClick(object sender, RoutedEventArgs e) => MyDialog.Open();
}
