using System.Windows.Controls;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Showcases the eleven Navius.Wpf.Ui display items in one scrollable page: Card, Alert, Badge,
/// Skeleton, Spinner, Kbd, Typography, Empty, Item, Table and Timeline. Self-contained (its own
/// dictionaries are merged in the XAML, not through Generic.xaml) and not wired into MainWindow's
/// navigation list.
/// </summary>
public partial class UiDisplayPage : UserControl
{
    public UiDisplayPage()
    {
        InitializeComponent();

        TableList.ItemsSource = new[]
        {
            new TableRow("navius-wpf", "Building", "2 min ago"),
            new TableRow("navius-docs", "Deployed", "1 hour ago"),
            new TableRow("zits-helm", "Queued", "3 hours ago"),
        };
    }

    private sealed record TableRow(string Project, string Status, string Updated);
}
