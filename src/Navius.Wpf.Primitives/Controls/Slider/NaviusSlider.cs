using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Tier A: derives from the native <see cref="Slider"/>, inheriting RangeBase value coercion,
/// pointer-drag machinery (Track/Thumb), and SliderAutomationPeer's mapping to UIA
/// RangeValuePattern (aria-valuemin/max/now parity).
///
/// Single-thumb only for M1: multi-thumb range sliders have no first-class WPF control and are
/// deferred, see docs/parity/slider.md "WPF implementation notes".
/// </summary>
[TemplatePart(Name = PartTrack, Type = typeof(Track))]
[TemplatePart(Name = PartRange, Type = typeof(RepeatButton))]
[TemplatePart(Name = PartThumb, Type = typeof(Thumb))]
public class NaviusSlider : Slider
{
    private const string PartTrack = "PART_Track";
    private const string PartRange = "PART_Range";
    private const string PartThumb = "PART_Thumb";

    public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
        nameof(Step), typeof(double), typeof(NaviusSlider),
        new PropertyMetadata(1.0, OnStepChanged));

    public static readonly DependencyProperty LargeStepProperty = DependencyProperty.Register(
        nameof(LargeStep), typeof(double), typeof(NaviusSlider),
        new PropertyMetadata(0.0));

    /// <summary>
    /// Navius extension for multi-thumb range sliders (minimum step separation between adjacent
    /// thumbs). Exposed for API parity; a no-op in this single-thumb M1 build since there is no
    /// adjacent thumb to separate from.
    /// </summary>
    public static readonly DependencyProperty MinStepsBetweenThumbsProperty = DependencyProperty.Register(
        nameof(MinStepsBetweenThumbs), typeof(int), typeof(NaviusSlider),
        new PropertyMetadata(0));

    public static readonly RoutedEvent ValueCommittedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueCommitted),
        RoutingStrategy.Bubble,
        typeof(RoutedPropertyChangedEventHandler<double>),
        typeof(NaviusSlider));

    private Thumb? _thumb;

    static NaviusSlider()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusSlider),
            new FrameworkPropertyMetadata(typeof(NaviusSlider)));

        // Contract default is Max=100; native Slider defaults to 10.
        MaximumProperty.OverrideMetadata(typeof(NaviusSlider), new FrameworkPropertyMetadata(100.0));

        // "snap to Step" applies to pointer drag; keyboard edits are snapped explicitly in OnKeyDown.
        IsSnapToTickEnabledProperty.OverrideMetadata(typeof(NaviusSlider), new FrameworkPropertyMetadata(true));
    }

    /// <summary>Keyboard/drag increment. Kept in sync with the native SmallChange and TickFrequency.</summary>
    public double Step
    {
        get => (double)GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    /// <summary>0 means the contract heuristic (max(Step, 10% of range snapped to Step)) is used.</summary>
    public double LargeStep
    {
        get => (double)GetValue(LargeStepProperty);
        set => SetValue(LargeStepProperty, value);
    }

    public int MinStepsBetweenThumbs
    {
        get => (int)GetValue(MinStepsBetweenThumbsProperty);
        set => SetValue(MinStepsBetweenThumbsProperty, value);
    }

    /// <summary>Fires on pointer-up (Thumb.DragCompleted) or immediately on any keyboard edit.</summary>
    public event RoutedPropertyChangedEventHandler<double> ValueCommitted
    {
        add => AddHandler(ValueCommittedEvent, value);
        remove => RemoveHandler(ValueCommittedEvent, value);
    }

    public double EffectiveLargeStep =>
        NaviusSliderKeyboard.ComputeEffectiveLargeStep(LargeStep, Step, Minimum, Maximum);

    public override void OnApplyTemplate()
    {
        if (_thumb is not null)
        {
            _thumb.DragCompleted -= OnThumbDragCompleted;
        }

        base.OnApplyTemplate();

        _thumb = GetTemplateChild(PartThumb) as Thumb;
        if (_thumb is not null)
        {
            _thumb.DragCompleted += OnThumbDragCompleted;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

        if (!NaviusSliderKeyboard.TryGetTargetValue(
                e.Key, shift, IsDirectionReversed, Value, Minimum, Maximum, Step, EffectiveLargeStep,
                out var targetValue))
        {
            base.OnKeyDown(e);
            return;
        }

        Value = targetValue;
        e.Handled = true;
        RaiseValueCommitted();
    }

    private void OnThumbDragCompleted(object sender, DragCompletedEventArgs e) => RaiseValueCommitted();

    private void RaiseValueCommitted() =>
        RaiseEvent(new RoutedPropertyChangedEventArgs<double>(Value, Value, ValueCommittedEvent));

    private static void OnStepChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var slider = (NaviusSlider)d;
        var step = (double)e.NewValue;
        slider.SmallChange = step;
        slider.TickFrequency = step;
    }
}
