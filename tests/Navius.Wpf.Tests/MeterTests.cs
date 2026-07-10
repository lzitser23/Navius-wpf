using System.Windows;
using System.Windows.Media;
using Navius.Wpf.Primitives.Controls;

namespace Navius.Wpf.Tests;

public class MeterTests
{
    static MeterTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        // Guarded try/catch (rather than a bare null-check) because xunit runs test classes in
        // parallel on separate STA threads: another test class's static ctor can win the race.
        if (Application.Current is null)
        {
            try
            {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            catch (InvalidOperationException)
            {
                // Another test class's static ctor already created the process-wide Application.
            }
        }
    }

    [StaFact]
    public void Defaults_MatchContract()
    {
        var meter = new NaviusMeter();

        Assert.Equal(0, meter.Value);
        Assert.Equal(0, meter.Minimum);
        Assert.Equal(100, meter.Maximum);
        Assert.False(meter.IsIndeterminate);
    }

    [StaFact]
    public void Maximum_LessThanOrEqualToMinimum_FallsBackToMinimumPlus100()
    {
        var meter = new NaviusMeter { Minimum = 20, Maximum = 20 };

        Assert.Equal(120, meter.Maximum);
    }

    [StaFact]
    public void Value_IsClampedIntoRange_UnlikeProgress()
    {
        // Deviation from NaviusProgress's "validate, don't clamp": Meter genuinely clamps,
        // relying on RangeBase's own default coercion (no CoerceValueCallback override needed).
        var meter = new NaviusMeter { Maximum = 100, Value = 999 };

        Assert.Equal(100, meter.Value);
        Assert.False(meter.IsIndeterminate);
    }

    [StaFact]
    public void Value_Negative_IsClampedToMinimum()
    {
        var meter = new NaviusMeter { Minimum = 0, Value = -50 };

        Assert.Equal(0, meter.Value);
    }

    [StaFact]
    public void IsIndeterminate_AlwaysCoercedToFalse()
    {
        var meter = new NaviusMeter();

        meter.IsIndeterminate = true;

        Assert.False(meter.IsIndeterminate);
    }

    [StaFact]
    public void Fraction_IsMinAware()
    {
        var meter = new NaviusMeter { Minimum = 50, Maximum = 150, Value = 100 };

        Assert.Equal(0.5, meter.Fraction);
        Assert.Equal(50, meter.Percentage);
    }

    [StaFact]
    public void FormatValueText_DefaultsToRoundedPercentage()
    {
        var meter = new NaviusMeter { Maximum = 100, Value = 33 };

        Assert.Equal("33%", meter.FormatValueText());
    }

    [StaFact]
    public void FormatValueText_UsesGetValueLabelWhenSet()
    {
        var meter = new NaviusMeter
        {
            Maximum = 100,
            Value = 40,
            GetValueLabel = value => $"{value} of 100 GB",
        };

        Assert.Equal("40 of 100 GB", meter.FormatValueText());
    }

    [StaFact]
    public void AutomationPeer_ItemStatus_MatchesFormattedValueText()
    {
        var meter = new NaviusMeter { Maximum = 100, Value = 75 };
        var peer = new NaviusMeterAutomationPeer(meter);

        Assert.Equal("75%", peer.GetItemStatus());
    }

    // --- NaviusMeterValue: attachable part wired via Source, no visual-tree dependency ---

    [StaFact]
    public void MeterValue_TracksSourceDefaultText()
    {
        var meter = new NaviusMeter { Maximum = 100, Value = 20 };
        var valueText = new NaviusMeterValue { Source = meter };

        Assert.Equal("20%", valueText.Text);

        meter.Value = 80;

        Assert.Equal("80%", valueText.Text);
    }

    [StaFact]
    public void MeterValue_TextOverride_WinsOverDefault()
    {
        var meter = new NaviusMeter { Maximum = 100, Value = 20 };
        var valueText = new NaviusMeterValue { Source = meter, TextOverride = "custom" };

        Assert.Equal("custom", valueText.Text);
    }

    [StaFact]
    public void MeterLabel_IsTextBlock()
    {
        var label = new NaviusMeterLabel { Text = "Disk usage" };

        Assert.Equal("Disk usage", label.Text);
    }

    // --- Template ---

    [StaFact]
    public void Template_AppliesAndExposesTrackAndIndicatorParts()
    {
        var dictionary = new ResourceDictionary
        {
            Source = new Uri(
                "pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Meter.xaml",
                UriKind.Absolute),
        };
        Application.Current.Resources.MergedDictionaries.Add(dictionary);

        try
        {
            var meter = new NaviusMeter { Maximum = 100, Value = 40 };
            // Elements outside a live visual/logical tree don't automatically pick up an implicit
            // (TargetType-keyed) style; wire it explicitly, same as WPF does internally once an
            // element is parented.
            meter.SetResourceReference(FrameworkElement.StyleProperty, typeof(NaviusMeter));
            meter.ApplyTemplate();

            Assert.NotNull(meter.Template);
            Assert.NotNull(FindByName(meter, "PART_Track"));
            Assert.NotNull(FindByName(meter, "PART_Indicator"));
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    private static FrameworkElement? FindByName(DependencyObject root, string name)
    {
        var childCount = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is FrameworkElement { } element && element.Name == name)
            {
                return element;
            }

            var descendant = FindByName(child, name);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }
}
