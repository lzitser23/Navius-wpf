using System.Windows.Controls;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Demonstrates NaviusProgress states: determinate, complete, indeterminate, and a labeled example
/// pairing NaviusProgressLabel/NaviusProgressValue with a custom GetValueLabel formatter.
/// </summary>
public partial class ProgressPage : UserControl
{
    public ProgressPage()
    {
        InitializeComponent();

        LabeledProgress.GetValueLabel = (value, max) => $"{value:0} of {max:0} MB";
    }
}
