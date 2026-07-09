using System;
using System.Windows.Controls;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>Demonstrates NaviusTimePicker states: default, second granularity, 24-hour cycle, minute step, and disabled.</summary>
public partial class TimePickerPage : UserControl
{
    public TimePickerPage()
    {
        InitializeComponent();

        // TimeOnly? has no XAML TypeConverter, so pre-filled demo values are set here.
        SecondsPicker.Value = new TimeOnly(9, 30, 15);
        TwentyFourHourPicker.Value = new TimeOnly(14, 45);
        SteppedPicker.Value = new TimeOnly(9, 15);
        DisabledPicker.Value = new TimeOnly(9, 30);
    }
}
