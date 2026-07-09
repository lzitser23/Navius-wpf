using System;
using System.Windows.Controls;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>Demonstrates NaviusDateInput states: default, pre-filled, month granularity, forced leading zeros, read-only, and disabled.</summary>
public partial class DateInputPage : UserControl
{
    public DateInputPage()
    {
        InitializeComponent();

        // DateOnly? has no XAML TypeConverter, so pre-filled demo values are set here.
        FilledInput.Value = new DateOnly(2026, 7, 9);
        MonthGranularityInput.Value = new DateOnly(2026, 7, 1);
        LeadingZerosInput.Value = new DateOnly(2026, 1, 5);
        ReadOnlyInput.Value = new DateOnly(2026, 7, 9);
        DisabledInput.Value = new DateOnly(2026, 7, 9);
    }
}
