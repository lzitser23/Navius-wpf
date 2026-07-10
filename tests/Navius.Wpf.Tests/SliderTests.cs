using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Navius.Wpf.Primitives.Controls;

namespace Navius.Wpf.Tests;

public class SliderTests
{
    static SliderTests()
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
    public void Value_ClampsToMaximum()
    {
        var slider = new NaviusSlider { Minimum = 0, Maximum = 100, Value = 150 };

        Assert.Equal(100, slider.Value);
    }

    [StaFact]
    public void Value_ClampsToMinimum()
    {
        var slider = new NaviusSlider { Minimum = 0, Maximum = 100, Value = -10 };

        Assert.Equal(0, slider.Value);
    }

    [StaFact]
    public void Maximum_DefaultsTo100()
    {
        var slider = new NaviusSlider();

        Assert.Equal(100, slider.Maximum);
    }

    [StaFact]
    public void Step_SyncsSmallChangeAndTickFrequency()
    {
        var slider = new NaviusSlider { Step = 5 };

        Assert.Equal(5, slider.SmallChange);
        Assert.Equal(5, slider.TickFrequency);
    }

    [StaFact]
    public void EffectiveLargeStep_UsesExplicitValueWhenSet()
    {
        var slider = new NaviusSlider { Minimum = 0, Maximum = 100, Step = 1, LargeStep = 25 };

        Assert.Equal(25, slider.EffectiveLargeStep);
    }

    [StaFact]
    public void EffectiveLargeStep_FallsBackToTenPercentHeuristicWhenZero()
    {
        var slider = new NaviusSlider { Minimum = 0, Maximum = 100, Step = 1, LargeStep = 0 };

        // 10% of [0,100] is 10, snapped to Step=1 stays 10, max(step, snapped) = 10.
        Assert.Equal(10, slider.EffectiveLargeStep);
    }

    // --- NaviusSliderKeyboard: pure decision logic, tested directly (no simulated KeyEventArgs) ---

    [Fact]
    public void Keyboard_ArrowRight_AddsStep()
    {
        var handled = NaviusSliderKeyboard.TryGetTargetValue(
            Key.Right, shiftPressed: false, isDirectionReversed: false,
            value: 40, minimum: 0, maximum: 100, step: 5, effectiveLargeStep: 20,
            out var target);

        Assert.True(handled);
        Assert.Equal(45, target);
    }

    [Fact]
    public void Keyboard_ArrowLeft_SubtractsStep()
    {
        var handled = NaviusSliderKeyboard.TryGetTargetValue(
            Key.Left, shiftPressed: false, isDirectionReversed: false,
            value: 40, minimum: 0, maximum: 100, step: 5, effectiveLargeStep: 20,
            out var target);

        Assert.True(handled);
        Assert.Equal(35, target);
    }

    [Fact]
    public void Keyboard_ArrowKeys_FlipUnderDirectionReversed()
    {
        var right = NaviusSliderKeyboard.TryGetTargetValue(
            Key.Right, shiftPressed: false, isDirectionReversed: true,
            value: 40, minimum: 0, maximum: 100, step: 5, effectiveLargeStep: 20,
            out var rightTarget);
        var left = NaviusSliderKeyboard.TryGetTargetValue(
            Key.Left, shiftPressed: false, isDirectionReversed: true,
            value: 40, minimum: 0, maximum: 100, step: 5, effectiveLargeStep: 20,
            out var leftTarget);

        Assert.True(right);
        Assert.Equal(35, rightTarget);
        Assert.True(left);
        Assert.Equal(45, leftTarget);
    }

    [Fact]
    public void Keyboard_ShiftArrow_UsesLargeStep()
    {
        var handled = NaviusSliderKeyboard.TryGetTargetValue(
            Key.Right, shiftPressed: true, isDirectionReversed: false,
            value: 40, minimum: 0, maximum: 100, step: 5, effectiveLargeStep: 20,
            out var target);

        Assert.True(handled);
        Assert.Equal(60, target);
    }

    [Fact]
    public void Keyboard_PageUp_AddsLargeStep()
    {
        var handled = NaviusSliderKeyboard.TryGetTargetValue(
            Key.PageUp, shiftPressed: false, isDirectionReversed: false,
            value: 40, minimum: 0, maximum: 100, step: 5, effectiveLargeStep: 20,
            out var target);

        Assert.True(handled);
        Assert.Equal(60, target);
    }

    [Fact]
    public void Keyboard_PageDown_SubtractsLargeStep()
    {
        var handled = NaviusSliderKeyboard.TryGetTargetValue(
            Key.PageDown, shiftPressed: false, isDirectionReversed: false,
            value: 40, minimum: 0, maximum: 100, step: 5, effectiveLargeStep: 20,
            out var target);

        Assert.True(handled);
        Assert.Equal(20, target);
    }

    [Fact]
    public void Keyboard_PageKeys_DoNotFlipUnderDirectionReversed()
    {
        var handled = NaviusSliderKeyboard.TryGetTargetValue(
            Key.PageUp, shiftPressed: false, isDirectionReversed: true,
            value: 40, minimum: 0, maximum: 100, step: 5, effectiveLargeStep: 20,
            out var target);

        Assert.True(handled);
        Assert.Equal(60, target);
    }

    [Fact]
    public void Keyboard_Home_JumpsToMinimum()
    {
        var handled = NaviusSliderKeyboard.TryGetTargetValue(
            Key.Home, shiftPressed: false, isDirectionReversed: false,
            value: 40, minimum: 5, maximum: 100, step: 5, effectiveLargeStep: 20,
            out var target);

        Assert.True(handled);
        Assert.Equal(5, target);
    }

    [Fact]
    public void Keyboard_End_JumpsToMaximum()
    {
        var handled = NaviusSliderKeyboard.TryGetTargetValue(
            Key.End, shiftPressed: false, isDirectionReversed: false,
            value: 40, minimum: 0, maximum: 100, step: 5, effectiveLargeStep: 20,
            out var target);

        Assert.True(handled);
        Assert.Equal(100, target);
    }

    [Fact]
    public void Keyboard_ClampsResultToRange()
    {
        var handled = NaviusSliderKeyboard.TryGetTargetValue(
            Key.Right, shiftPressed: true, isDirectionReversed: false,
            value: 90, minimum: 0, maximum: 100, step: 5, effectiveLargeStep: 20,
            out var target);

        Assert.True(handled);
        Assert.Equal(100, target);
    }

    [Fact]
    public void Keyboard_UnrelatedKey_ReturnsFalse()
    {
        var handled = NaviusSliderKeyboard.TryGetTargetValue(
            Key.A, shiftPressed: false, isDirectionReversed: false,
            value: 40, minimum: 0, maximum: 100, step: 5, effectiveLargeStep: 20,
            out _);

        Assert.False(handled);
    }

    // --- Template ---

    [StaFact]
    public void Template_AppliesAndExposesTrackAndThumbParts()
    {
        var dictionary = new ResourceDictionary
        {
            Source = new Uri(
                "pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Slider.xaml",
                UriKind.Absolute),
        };
        Application.Current.Resources.MergedDictionaries.Add(dictionary);

        try
        {
            var slider = new NaviusSlider();
            // Elements outside a live visual/logical tree don't automatically pick up an implicit
            // (TargetType-keyed) style; wire it explicitly, same as WPF does internally once an
            // element is parented.
            slider.SetResourceReference(FrameworkElement.StyleProperty, typeof(NaviusSlider));
            slider.ApplyTemplate();

            Assert.NotNull(slider.Template);
            Assert.NotNull(FindDescendant<Track>(slider));
            Assert.NotNull(FindDescendant<Thumb>(slider));
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        var childCount = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                return match;
            }

            var descendant = FindDescendant<T>(child);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }
}
