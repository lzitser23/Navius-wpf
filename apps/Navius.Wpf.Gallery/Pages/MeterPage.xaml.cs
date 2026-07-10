using System.Windows.Controls;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Demonstrates NaviusMeter states: low, full, and a labeled example pairing NaviusMeterLabel/
/// NaviusMeterValue with a custom GetValueLabel formatter.
/// </summary>
public partial class MeterPage : UserControl
{
    public MeterPage()
    {
        InitializeComponent();

        LabeledMeter.GetValueLabel = value => $"{value:0} of {LabeledMeter.Maximum:0} GB";
    }
}
