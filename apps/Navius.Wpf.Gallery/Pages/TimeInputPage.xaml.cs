using System;
using System.Windows.Controls;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>Demonstrates NaviusTimeInput states: default, second granularity, 24-hour cycle, minute step, read-only, and disabled.</summary>
public partial class TimeInputPage : UserControl
{
    public TimeInputPage()
    {
        InitializeComponent();

        // TimeOnly? has no XAML TypeConverter, so pre-filled demo values are set here.
        SecondsInput.Value = new TimeOnly(9, 30, 15);
        TwentyFourHourInput.Value = new TimeOnly(14, 45);
        SteppedInput.Value = new TimeOnly(9, 15);
        ReadOnlyInput.Value = new TimeOnly(9, 30);
        DisabledInput.Value = new TimeOnly(9, 30);
    }
}
