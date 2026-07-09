using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Tier A: derives from the native ProgressBar, restyled to a non-interactive static gauge (see
/// docs/parity/meter.md "WPF strategy"). Unlike NaviusProgress, Meter is never indeterminate, so
/// IsIndeterminate is coerced to always false. Value is genuinely clamped into [Minimum, Maximum]
/// -- RangeBase's own default coercion already does this, so no CoerceValueCallback override is
/// needed here, unlike NaviusProgress's "validate, don't clamp" deviation. Maximum &lt;= Minimum
/// falls back to Minimum + 100 (the contract's own rule, distinct from Progress's
/// "Max &lt;= 0 -&gt; 100").
/// </summary>
[TemplatePart(Name = PartTrack, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartIndicator, Type = typeof(FrameworkElement))]
public class NaviusMeter : ProgressBar
{
    private const string PartTrack = "PART_Track";
    private const string PartIndicator = "PART_Indicator";

    public static readonly DependencyProperty GetValueLabelProperty = DependencyProperty.Register(
        nameof(GetValueLabel), typeof(Func<double, string?>), typeof(NaviusMeter));

    /// <summary>
    /// Raised whenever Value, Minimum, or Maximum change, i.e. whenever the derived display state
    /// (percentage text) needs to be recomputed. Consumed by NaviusMeterValue to refresh its text
    /// without a cascading context, mirroring NaviusProgress.StateChanged.
    /// </summary>
    public static readonly RoutedEvent StateChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(StateChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NaviusMeter));

    static NaviusMeter()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusMeter), new FrameworkPropertyMetadata(typeof(NaviusMeter)));

        ValueProperty.OverrideMetadata(typeof(NaviusMeter), new FrameworkPropertyMetadata(0.0, OnStateChanged));
        MinimumProperty.OverrideMetadata(typeof(NaviusMeter), new FrameworkPropertyMetadata(0.0, OnStateChanged));
        MaximumProperty.OverrideMetadata(
            typeof(NaviusMeter), new FrameworkPropertyMetadata(100.0, OnStateChanged, CoerceMaximum));

        // Meter is always a static, determinate readout -- never indeterminate, unlike Progress.
        IsIndeterminateProperty.OverrideMetadata(
            typeof(NaviusMeter), new FrameworkPropertyMetadata(false, null, CoerceIsIndeterminate));
    }

    /// <summary>Builds the aria-valuetext equivalent, surfaced via the automation peer's ItemStatus. Defaults to the rounded percentage.</summary>
    public Func<double, string?>? GetValueLabel
    {
        get => (Func<double, string?>?)GetValue(GetValueLabelProperty);
        set => SetValue(GetValueLabelProperty, value);
    }

    /// <summary>0..1, min-aware. Maps to the contract's MeterContext.Fraction.</summary>
    public double Fraction => Maximum > Minimum ? (Value - Minimum) / (Maximum - Minimum) : 0;

    public double Percentage => Fraction * 100;

    public event RoutedEventHandler StateChanged
    {
        add => AddHandler(StateChangedEvent, value);
        remove => RemoveHandler(StateChangedEvent, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusMeterAutomationPeer(this);

    /// <summary>GetValueLabel or the default rounded percentage + "%".</summary>
    public string FormatValueText() => GetValueLabel?.Invoke(Value) ?? $"{Math.Round(Percentage)}%";

    private static object CoerceMaximum(DependencyObject d, object baseValue)
    {
        var meter = (NaviusMeter)d;
        var max = (double)baseValue;
        return max <= meter.Minimum ? meter.Minimum + 100 : max;
    }

    private static object CoerceIsIndeterminate(DependencyObject d, object baseValue) => false;

    private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusMeter)d).RaiseEvent(new RoutedEventArgs(StateChangedEvent));
}
