using System.Windows.Controls;
using Navius.Wpf.Charts;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Self-contained line/bar/pie demos for <see cref="NaviusChart"/> (see
/// docs/adr/0004-chart-library.md). Not wired into gallery navigation. The Gallery project
/// does not reference Navius.Wpf.Charts yet, so this page only compiles once that project
/// reference is added; verified instead by building Navius.Wpf.Charts and its tests standalone.
/// </summary>
public partial class ChartsPage : UserControl
{
    public ChartsPage()
    {
        InitializeComponent();

        var categories = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };

        LineDemo.Categories = categories;
        LineDemo.Series =
        [
            new NaviusChartSeries { Key = "desktop", Label = "Desktop", Values = [186, 305, 237, 173, 209, 260] },
            new NaviusChartSeries { Key = "mobile", Label = "Mobile", Values = [80, 200, 120, 190, 130, 140] },
        ];

        BarDemo.Categories = categories;
        BarDemo.Series =
        [
            // Mixed-sign values demo the destructive-color negative emphasis.
            new NaviusChartSeries { Key = "net", Label = "Net change", Values = [12, -5, 8, -3, 15, -7] },
        ];

        PieDemo.Series =
        [
            new NaviusChartSeries { Key = "chrome", Label = "Chrome", Values = [275] },
            new NaviusChartSeries { Key = "safari", Label = "Safari", Values = [200] },
            new NaviusChartSeries { Key = "firefox", Label = "Firefox", Values = [187] },
        ];
    }
}
