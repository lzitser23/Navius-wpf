using System.Windows.Controls;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Manual-inspection page for the M6 RTL wave (see docs/adr/0006-rtl-dpi-hardening.md): hosts a
/// FlowDirection=RightToLeft panel with Slider, Progress, Rating, DateInput, Tabs, and Breadcrumb
/// so their mirrored (or, for DateInput, deliberately un-mirrored) layout can be eyeballed.
/// Self-contained, not wired into MainWindow's navigation.
/// </summary>
public partial class RtlPage : UserControl
{
    public RtlPage()
    {
        InitializeComponent();
    }
}
