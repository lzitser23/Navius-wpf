using System.Windows.Controls;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Interaction logic for DatePickerPage.xaml. The demo instance is declared in markup; the
/// AutomationId ("DatePickerDemo") is set there on the real control so an external FlaUI
/// harness can look it up.
/// </summary>
public partial class DatePickerPage : UserControl
{
    public DatePickerPage()
    {
        InitializeComponent();
    }
}
