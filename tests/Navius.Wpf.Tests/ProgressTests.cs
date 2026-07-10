using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Navius.Wpf.Primitives.Controls;

namespace Navius.Wpf.Tests;

public class ProgressTests
{
    static ProgressTests()
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
    public void Maximum_DefaultsTo100()
    {
        var progress = new NaviusProgress();

        Assert.Equal(100, progress.Maximum);
    }

    [StaFact]
    public void Maximum_NonPositiveFallsBackTo100()
    {
        var progress = new NaviusProgress { Maximum = -5 };

        Assert.Equal(100, progress.Maximum);
    }

    [StaFact]
    public void Value_InRange_IsDeterminate()
    {
        var progress = new NaviusProgress { Maximum = 100, Value = 40 };

        Assert.False(progress.IsIndeterminate);
        Assert.Equal(40, progress.Value);
    }

    [StaFact]
    public void Value_AboveMaximum_BecomesIndeterminateWithoutClamping()
    {
        var progress = new NaviusProgress { Maximum = 100, Value = 999 };

        Assert.True(progress.IsIndeterminate);
        // Validate, don't clamp: the raw out-of-range value is preserved, unlike native RangeBase.
        Assert.Equal(999, progress.Value);
    }

    [StaFact]
    public void Value_Negative_BecomesIndeterminate()
    {
        var progress = new NaviusProgress { Maximum = 100, Value = -1 };

        Assert.True(progress.IsIndeterminate);
    }

    [StaFact]
    public void Value_NaN_IsRejectedByNativeValidation()
    {
        // Deviation from the contract: RangeBase.ValueProperty's own ValidateValueCallback
        // rejects NaN/Infinity outright, before NaviusProgress's coercion ever runs. Only
        // negative/>Max make it through to become indeterminate; see
        // docs/parity/progress.md "WPF implementation notes".
        var progress = new NaviusProgress { Maximum = 100 };

        Assert.Throws<ArgumentException>(() => progress.Value = double.NaN);
    }

    [StaFact]
    public void Value_ReenteringValidRange_ClearsIndeterminate()
    {
        var progress = new NaviusProgress { Maximum = 100, Value = 999 };
        Assert.True(progress.IsIndeterminate);

        progress.Value = 50;

        Assert.False(progress.IsIndeterminate);
    }

    [StaFact]
    public void IsComplete_TrueWhenValueReachesMaximum()
    {
        var progress = new NaviusProgress { Maximum = 100, Value = 100 };

        Assert.True(progress.IsComplete);
        Assert.False(progress.IsProgressing);
    }

    [StaFact]
    public void IsProgressing_TrueWhenDeterminateAndBelowMaximum()
    {
        var progress = new NaviusProgress { Maximum = 100, Value = 40 };

        Assert.True(progress.IsProgressing);
        Assert.False(progress.IsComplete);
    }

    [StaFact]
    public void IsComplete_FalseWhileIndeterminate()
    {
        var progress = new NaviusProgress { IsIndeterminate = true };

        Assert.False(progress.IsComplete);
        Assert.False(progress.IsProgressing);
    }

    [StaFact]
    public void FormatValueText_DefaultsToRoundedPercentage()
    {
        var progress = new NaviusProgress { Maximum = 100, Value = 33 };

        Assert.Equal("33%", progress.FormatValueText());
    }

    [StaFact]
    public void FormatValueText_UsesGetValueLabelWhenSet()
    {
        var progress = new NaviusProgress
        {
            Maximum = 200,
            Value = 50,
            GetValueLabel = (value, max) => $"{value} of {max} MB",
        };

        Assert.Equal("50 of 200 MB", progress.FormatValueText());
    }

    [StaFact]
    public void FormatValueText_NullWhileIndeterminate()
    {
        var progress = new NaviusProgress { IsIndeterminate = true };

        Assert.Null(progress.FormatValueText());
    }

    [StaFact]
    public void AutomationPeer_ItemStatus_MatchesFormattedValueText()
    {
        var progress = new NaviusProgress { Maximum = 100, Value = 75 };
        var peer = new NaviusProgressAutomationPeer(progress);

        Assert.Equal("75%", peer.GetItemStatus());
    }

    [StaFact]
    public void AutomationPeer_ItemStatus_EmptyWhileIndeterminate()
    {
        var progress = new NaviusProgress { IsIndeterminate = true };
        var peer = new NaviusProgressAutomationPeer(progress);

        Assert.Equal(string.Empty, peer.GetItemStatus());
    }

    // --- NaviusProgressValue: attachable part wired via Source, no visual-tree dependency ---

    [StaFact]
    public void ProgressValue_TracksSourceDefaultText()
    {
        var progress = new NaviusProgress { Maximum = 100, Value = 20 };
        var valueText = new NaviusProgressValue { Source = progress };

        Assert.Equal("20%", valueText.Text);

        progress.Value = 80;

        Assert.Equal("80%", valueText.Text);
    }

    [StaFact]
    public void ProgressValue_EmptyWhileSourceIndeterminate()
    {
        var progress = new NaviusProgress { IsIndeterminate = true };
        var valueText = new NaviusProgressValue { Source = progress };

        Assert.Equal(string.Empty, valueText.Text);
    }

    [StaFact]
    public void ProgressValue_TextOverride_WinsOverDefault()
    {
        var progress = new NaviusProgress { Maximum = 100, Value = 20 };
        var valueText = new NaviusProgressValue { Source = progress, TextOverride = "custom" };

        Assert.Equal("custom", valueText.Text);
    }

    [StaFact]
    public void ProgressLabel_IsTextBlock()
    {
        var label = new NaviusProgressLabel { Text = "Download progress" };

        Assert.Equal("Download progress", label.Text);
    }

    // --- Template ---

    [StaFact]
    public void Template_AppliesAndExposesTrackAndIndicatorParts()
    {
        var dictionary = new ResourceDictionary
        {
            Source = new Uri(
                "pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Progress.xaml",
                UriKind.Absolute),
        };
        Application.Current.Resources.MergedDictionaries.Add(dictionary);

        try
        {
            var progress = new NaviusProgress { Maximum = 100, Value = 40 };
            // Elements outside a live visual/logical tree don't automatically pick up an implicit
            // (TargetType-keyed) style; wire it explicitly, same as WPF does internally once an
            // element is parented.
            progress.SetResourceReference(FrameworkElement.StyleProperty, typeof(NaviusProgress));
            progress.ApplyTemplate();

            Assert.NotNull(progress.Template);
            Assert.NotNull(FindByName(progress, "PART_Track"));
            Assert.NotNull(FindByName(progress, "PART_Indicator"));
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
