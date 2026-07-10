using System.Windows.Controls;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Interaction logic for CalendarPage.xaml. The demo instances are declared in markup
/// (NaviusCalendar is non-generic); the AutomationId ("CalendarDemo") is set there on the
/// real control so an external FlaUI harness can look it up.
/// </summary>
public partial class CalendarPage : UserControl
{
    public CalendarPage()
    {
        InitializeComponent();
    }
}
