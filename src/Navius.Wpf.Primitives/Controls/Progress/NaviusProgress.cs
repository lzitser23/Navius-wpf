using System.Diagnostics;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Tier A: derives from the native <see cref="ProgressBar"/>, retemplated to expose PART_Track /
/// PART_Indicator (ProgressBar's own required part names, which line up with the contract's Track
/// and Indicator parts for free). IsIndeterminate maps directly to the native property.
///
/// The contract's Value is "validate, don't clamp": negative/&gt;Max flips IsIndeterminate rather
/// than being coerced into range, which deliberately replaces RangeBase's default clamp-to-range
/// coercion. NaN/Infinity are rejected outright by the base Value property before reaching this
/// class (see docs/parity/progress.md "WPF implementation notes").
/// </summary>
[TemplatePart(Name = PartTrack, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartIndicator, Type = typeof(FrameworkElement))]
public class NaviusProgress : ProgressBar
{
    private const string PartTrack = "PART_Track";
    private const string PartIndicator = "PART_Indicator";

    public static readonly DependencyProperty GetValueLabelProperty = DependencyProperty.Register(
        nameof(GetValueLabel), typeof(Func<double, double, string?>), typeof(NaviusProgress));

    private static readonly DependencyPropertyKey IsCompletePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsComplete), typeof(bool), typeof(NaviusProgress), new PropertyMetadata(false));

    public static readonly DependencyProperty IsCompleteProperty = IsCompletePropertyKey.DependencyProperty;

    /// <summary>
    /// Raised whenever Value, Maximum, or IsIndeterminate change, i.e. whenever the derived
    /// display state (percentage text, complete/progressing) needs to be recomputed. Consumed by
    /// <see cref="NaviusProgressValue"/> to refresh its text without a cascading context.
    /// </summary>
    public static readonly RoutedEvent StateChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(StateChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NaviusProgress));

    static NaviusProgress()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusProgress),
            new FrameworkPropertyMetadata(typeof(NaviusProgress)));

        ValueProperty.OverrideMetadata(
            typeof(NaviusProgress),
            new FrameworkPropertyMetadata(0.0, OnStateChanged, CoerceValue));

        MaximumProperty.OverrideMetadata(
            typeof(NaviusProgress),
            new FrameworkPropertyMetadata(100.0, OnStateChanged, CoerceMaximum));

        IsIndeterminateProperty.OverrideMetadata(
            typeof(NaviusProgress),
            new FrameworkPropertyMetadata(false, OnStateChanged));
    }

    /// <summary>
    /// Builds the aria-valuetext equivalent (surfaced via the automation peer's ItemStatus) for a
    /// determinate value. Defaults to the rounded percentage when null. Never invoked while indeterminate.
    /// </summary>
    public Func<double, double, string?>? GetValueLabel
    {
        get => (Func<double, double, string?>?)GetValue(GetValueLabelProperty);
        set => SetValue(GetValueLabelProperty, value);
    }

    /// <summary>True when determinate and Value >= Maximum. Maps to the contract's data-complete.</summary>
    public bool IsComplete => (bool)GetValue(IsCompleteProperty);

    /// <summary>True when determinate and Value &lt; Maximum. Maps to the contract's data-progressing.</summary>
    public bool IsProgressing => !IsIndeterminate && !IsComplete;

    public event RoutedEventHandler StateChanged
    {
        add => AddHandler(StateChangedEvent, value);
        remove => RemoveHandler(StateChangedEvent, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusProgressAutomationPeer(this);

    /// <summary>The formatted value text: null while indeterminate, otherwise GetValueLabel or the default rounded percentage.</summary>
    public string? FormatValueText()
    {
        if (IsIndeterminate)
        {
            return null;
        }

        return GetValueLabel?.Invoke(Value, Maximum) ?? $"{Math.Round(Value / Maximum * 100)}%";
    }

    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        var progress = (NaviusProgress)d;
        var value = (double)baseValue;

        // NaN/Infinity are excluded here: RangeBase.ValueProperty's own ValidateValueCallback
        // rejects them with an ArgumentException before coercion ever runs, so only
        // negative/>Max is reachable and portable to indeterminate. See docs/parity/progress.md
        // "WPF implementation notes".
        if (value < 0 || value > progress.Maximum)
        {
            Debug.WriteLine(
                $"Navius.Wpf: NaviusProgress.Value {value} is outside [0, {progress.Maximum}]; treating as indeterminate.");
            progress.SetCurrentValue(IsIndeterminateProperty, true);
            return value;
        }

        if (progress.IsIndeterminate)
        {
            progress.SetCurrentValue(IsIndeterminateProperty, false);
        }

        return value;
    }

    private static object CoerceMaximum(DependencyObject d, object baseValue)
    {
        var max = (double)baseValue;
        return max <= 0 ? 100.0 : max;
    }

    private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var progress = (NaviusProgress)d;
        var isComplete = !progress.IsIndeterminate && progress.Value >= progress.Maximum;
        progress.SetValue(IsCompletePropertyKey, isComplete);
        progress.RaiseEvent(new RoutedEventArgs(StateChangedEvent));
    }
}
