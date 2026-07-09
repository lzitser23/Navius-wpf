using System.Windows.Controls;
using System.Windows;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Demonstrates NaviusAlertDialog: always modal, no outside-click dismissal, initial focus on
/// the Cancel action (marked via the attached NaviusAlertDialog.IsCancelButton property).
/// </summary>
public partial class AlertDialogPage : UserControl
{
    public AlertDialogPage()
    {
        InitializeComponent();
    }

    private void OnOpenClick(object sender, RoutedEventArgs e) => MyAlertDialog.Open();
}
