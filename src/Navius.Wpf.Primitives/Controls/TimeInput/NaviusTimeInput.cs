using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using Navius.Wpf.Primitives.Controls.DateInput;
using Navius.Wpf.Primitives.Controls.Internal;

namespace Navius.Wpf.Primitives.Controls.TimeInput;

/// <summary>
/// Tier B (custom lookless control), the time-editing sibling of <see cref="NaviusDateInput"/>.
/// Reuses the exact same shared parts (<see cref="NaviusFieldSegment"/>, <see cref="SegmentMath"/>,
/// <see cref="SegmentLayoutBuilder"/>) rather than duplicating the engine, per time-input.md's open
/// question ("the WPF port should likely design one shared segment-editor base ... rather than
/// duplicating SegmentMath/DateTimeSegment twice") -- resolved here by sharing the engine and the
/// segment/literal parts while keeping two independent root controls (DateInput composes y/m/d,
/// TimeInput composes h/m/s/dayPeriod), since the two roots' layout-building and Compose() targets
/// (DateOnly? vs TimeOnly?) are different enough that a shared root base would mostly be
/// pass-through plumbing.
///
/// Same contract deltas as NaviusDateInput (see that family's "WPF implementation notes"): no
/// NaviusBubbleInput, no ambient NaviusField (FocusFirstSegment() instead), Dir -&gt; FlowDirection.
/// </summary>
[TemplatePart(Name = PartSegments, Type = typeof(Panel))]
public class NaviusTimeInput : Control
{
    private const string PartSegments = "PART_Segments";

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(TimeOnly?), typeof(NaviusTimeInput),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty GranularityProperty = DependencyProperty.Register(
        nameof(Granularity), typeof(string), typeof(NaviusTimeInput),
        new PropertyMetadata("minute", OnLayoutAffectingChanged));

    public static readonly DependencyProperty HourCycleProperty = DependencyProperty.Register(
        nameof(HourCycle), typeof(int?), typeof(NaviusTimeInput), new PropertyMetadata(null, OnLayoutAffectingChanged));

    public static readonly DependencyProperty MinuteStepProperty = DependencyProperty.Register(
        nameof(MinuteStep), typeof(int), typeof(NaviusTimeInput), new PropertyMetadata(1, OnLayoutAffectingChanged));

    public static readonly DependencyProperty SecondStepProperty = DependencyProperty.Register(
        nameof(SecondStep), typeof(int), typeof(NaviusTimeInput), new PropertyMetadata(1, OnLayoutAffectingChanged));

    public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
        nameof(MinValue), typeof(TimeOnly?), typeof(NaviusTimeInput), new PropertyMetadata(null, OnBoundChanged));

    public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
        nameof(MaxValue), typeof(TimeOnly?), typeof(NaviusTimeInput), new PropertyMetadata(null, OnBoundChanged));

    public static readonly DependencyProperty PlaceholderValueProperty = DependencyProperty.Register(
        nameof(PlaceholderValue), typeof(TimeOnly?), typeof(NaviusTimeInput), new PropertyMetadata(null));

    public static readonly DependencyProperty ReadOnlyProperty = DependencyProperty.Register(
        nameof(ReadOnly), typeof(bool), typeof(NaviusTimeInput), new PropertyMetadata(false, OnReadOnlyChanged));

    public static readonly DependencyProperty RequiredProperty = DependencyProperty.Register(
        nameof(Required), typeof(bool), typeof(NaviusTimeInput), new PropertyMetadata(false));

    public static readonly DependencyProperty InvalidProperty = DependencyProperty.Register(
        nameof(Invalid), typeof(bool), typeof(NaviusTimeInput), new PropertyMetadata(false, OnBoundChanged));

    public static readonly DependencyProperty ForceLeadingZerosProperty = DependencyProperty.Register(
        nameof(ForceLeadingZeros), typeof(bool), typeof(NaviusTimeInput), new PropertyMetadata(false, OnFormatChanged));

    public static readonly DependencyProperty CultureProperty = DependencyProperty.Register(
        nameof(Culture), typeof(CultureInfo), typeof(NaviusTimeInput), new PropertyMetadata(null, OnLayoutAffectingChanged));

    public static new readonly DependencyProperty NameProperty = DependencyProperty.Register(
        nameof(Name), typeof(string), typeof(NaviusTimeInput), new PropertyMetadata(null));

    private static readonly DependencyPropertyKey IsFilledPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsFilled), typeof(bool), typeof(NaviusTimeInput), new PropertyMetadata(false));

    public static readonly DependencyProperty IsFilledProperty = IsFilledPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsOutOfRangePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsOutOfRange), typeof(bool), typeof(NaviusTimeInput), new PropertyMetadata(false));

    public static readonly DependencyProperty IsOutOfRangeProperty = IsOutOfRangePropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsInvalidStatePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsInvalidState), typeof(bool), typeof(NaviusTimeInput), new PropertyMetadata(false));

    public static readonly DependencyProperty IsInvalidStateProperty = IsInvalidStatePropertyKey.DependencyProperty;

    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueChanged), RoutingStrategy.Bubble,
        typeof(RoutedPropertyChangedEventHandler<TimeOnly?>), typeof(NaviusTimeInput));

    private readonly System.Collections.Generic.List<DateTimeSegment> _segments = new();
    private readonly System.Collections.Generic.List<NaviusFieldSegment> _cells = new();
    private Panel? _host;
    private bool _isInternalSet;

    static NaviusTimeInput()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusTimeInput), new FrameworkPropertyMetadata(typeof(NaviusTimeInput)));
        FocusableProperty.OverrideMetadata(typeof(NaviusTimeInput), new FrameworkPropertyMetadata(false));
    }

    public TimeOnly? Value
    {
        get => (TimeOnly?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>"hour", "minute", or "second".</summary>
    public string Granularity
    {
        get => (string)GetValue(GranularityProperty);
        set => SetValue(GranularityProperty, value);
    }

    /// <summary>12 or 24; null defaults to the culture's short-time pattern ('H' present =&gt; 24).</summary>
    public int? HourCycle
    {
        get => (int?)GetValue(HourCycleProperty);
        set => SetValue(HourCycleProperty, value);
    }

    public int MinuteStep
    {
        get => (int)GetValue(MinuteStepProperty);
        set => SetValue(MinuteStepProperty, value);
    }

    public int SecondStep
    {
        get => (int)GetValue(SecondStepProperty);
        set => SetValue(SecondStepProperty, value);
    }

    public TimeOnly? MinValue
    {
        get => (TimeOnly?)GetValue(MinValueProperty);
        set => SetValue(MinValueProperty, value);
    }

    public TimeOnly? MaxValue
    {
        get => (TimeOnly?)GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    public TimeOnly? PlaceholderValue
    {
        get => (TimeOnly?)GetValue(PlaceholderValueProperty);
        set => SetValue(PlaceholderValueProperty, value);
    }

    public bool ReadOnly
    {
        get => (bool)GetValue(ReadOnlyProperty);
        set => SetValue(ReadOnlyProperty, value);
    }

    public bool Required
    {
        get => (bool)GetValue(RequiredProperty);
        set => SetValue(RequiredProperty, value);
    }

    public bool Invalid
    {
        get => (bool)GetValue(InvalidProperty);
        set => SetValue(InvalidProperty, value);
    }

    public bool ForceLeadingZeros
    {
        get => (bool)GetValue(ForceLeadingZerosProperty);
        set => SetValue(ForceLeadingZerosProperty, value);
    }

    public CultureInfo? Culture
    {
        get => (CultureInfo?)GetValue(CultureProperty);
        set => SetValue(CultureProperty, value);
    }

    public new string? Name
    {
        get => (string?)GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    public bool IsFilled => (bool)GetValue(IsFilledProperty);

    public bool IsOutOfRange => (bool)GetValue(IsOutOfRangeProperty);

    public bool IsInvalidState => (bool)GetValue(IsInvalidStateProperty);

    public event RoutedPropertyChangedEventHandler<TimeOnly?> ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    private CultureInfo EffectiveCulture => Culture ?? CultureInfo.CurrentCulture;

    /// <summary>Resolved 12/24 in effect right now (an explicit <see cref="HourCycle"/> wins, else culture sniff).</summary>
    public int EffectiveHourCycle => SegmentLayoutBuilder.ResolveHourCycle(EffectiveCulture, HourCycle);

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _host = GetTemplateChild(PartSegments) as Panel;
        RebuildLayout();
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusTimeInputAutomationPeer(this);

    /// <summary>Focuses the first editable segment (this port's stand-in for the contract's Field.FocusControl hookup).</summary>
    public void FocusFirstSegment()
    {
        if (_cells.Count > 0)
        {
            _cells[0].Focus();
        }
    }

    private void RebuildLayout()
    {
        if (_host is null)
        {
            return;
        }

        foreach (var cell in _cells)
        {
            cell.PreviewKeyDown -= OnSegmentPreviewKeyDown;
            cell.ValueRequested -= OnSegmentValueRequested;
        }

        _host.Children.Clear();
        _segments.Clear();
        _cells.Clear();

        var hourCycle = EffectiveHourCycle;
        var layout = SegmentLayoutBuilder.BuildTimeLayout(EffectiveCulture, Granularity, hourCycle);
        foreach (var item in layout)
        {
            if (item.Kind == SegmentLayoutKind.Literal)
            {
                _host.Children.Add(new TextBlock
                {
                    Text = item.Literal,
                    Focusable = false,
                    VerticalAlignment = VerticalAlignment.Center,
                });
                continue;
            }

            var model = SegmentLayoutBuilder.CreateSegment(item.Unit, hourCycle, MinuteStep, SecondStep);
            _segments.Add(model);

            var cell = new NaviusFieldSegment { Unit = item.Unit, IsSegmentReadOnly = ReadOnly };
            AutomationProperties.SetName(cell, SegmentAutomationName(item.Unit));
            cell.PreviewKeyDown += OnSegmentPreviewKeyDown;
            cell.ValueRequested += OnSegmentValueRequested;

            _cells.Add(cell);
            _host.Children.Add(cell);
        }

        SeedFromValue();
        RefreshAllCells();
        UpdateComputedState(Value);
    }

    private static string SegmentAutomationName(SegmentUnit unit) => unit switch
    {
        SegmentUnit.Hour => "hour",
        SegmentUnit.Minute => "minute",
        SegmentUnit.Second => "second",
        SegmentUnit.DayPeriod => "AM/PM",
        _ => unit.ToString(),
    };

    private void SeedFromValue()
    {
        var value = Value;
        var hourCycle = EffectiveHourCycle;

        foreach (var segment in _segments)
        {
            if (value is null)
            {
                segment.Value = null;
                segment.TypeBuffer = string.Empty;
                continue;
            }

            var time = value.Value;
            segment.Value = segment.Unit switch
            {
                SegmentUnit.Hour => hourCycle == 24 ? time.Hour : ToTwelveHour(time.Hour),
                SegmentUnit.Minute => time.Minute,
                SegmentUnit.Second => time.Second,
                SegmentUnit.DayPeriod => time.Hour >= 12 ? 1 : 0,
                _ => segment.Value,
            };
            segment.TypeBuffer = string.Empty;
        }
    }

    private static int ToTwelveHour(int hour24)
    {
        var h = hour24 % 12;
        return h == 0 ? 12 : h;
    }

    private void RefreshAllCells()
    {
        for (var i = 0; i < _segments.Count; i++)
        {
            RefreshCell(i);
        }
    }

    private void RefreshCell(int index)
    {
        var segment = _segments[index];
        var cell = _cells[index];
        cell.ValueNow = segment.Value;
        cell.Minimum = segment.Min;
        cell.Maximum = segment.Max;
        cell.IsPlaceholder = !segment.Filled;
        cell.DisplayText = SegmentFormat.FormatValue(segment, ForceLeadingZeros);
    }

    private TimeOnly? ComposeValue()
    {
        var hour = _segments.FirstOrDefault(s => s.Unit == SegmentUnit.Hour);
        if (hour is null)
        {
            return null;
        }

        var minute = _segments.FirstOrDefault(s => s.Unit == SegmentUnit.Minute);
        var second = _segments.FirstOrDefault(s => s.Unit == SegmentUnit.Second);
        var dayPeriod = _segments.FirstOrDefault(s => s.Unit == SegmentUnit.DayPeriod);
        return TimeSegmentComposer.Compose(hour, minute, second, dayPeriod, EffectiveHourCycle);
    }

    private int ResolvePlaceholderBasis(SegmentUnit unit)
    {
        var basis = PlaceholderValue ?? TimeOnly.FromDateTime(DateTime.Now);
        var hourCycle = EffectiveHourCycle;
        return unit switch
        {
            SegmentUnit.Hour => hourCycle == 24 ? basis.Hour : ToTwelveHour(basis.Hour),
            SegmentUnit.Minute => basis.Minute,
            SegmentUnit.Second => basis.Second,
            SegmentUnit.DayPeriod => basis.Hour >= 12 ? 1 : 0,
            _ => 0,
        };
    }

    private void Commit()
    {
        var composed = ComposeValue();
        if (Equals(composed, Value))
        {
            UpdateComputedState(composed);
            return;
        }

        _isInternalSet = true;
        try
        {
            SetCurrentValue(ValueProperty, composed);
        }
        finally
        {
            _isInternalSet = false;
        }
    }

    private void UpdateComputedState(TimeOnly? value)
    {
        var anyFilled = _segments.Any(s => s.Filled);
        SetValue(IsFilledPropertyKey, anyFilled);

        var outOfRange = value is not null &&
            ((MinValue is { } min && value < min) || (MaxValue is { } max && value > max));
        SetValue(IsOutOfRangePropertyKey, outOfRange);
        SetValue(IsInvalidStatePropertyKey, Invalid || outOfRange);
    }

    private void FocusSegment(int index)
    {
        if (index >= 0 && index < _cells.Count)
        {
            _cells[index].Focus();
        }
    }

    private void OnSegmentPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!IsEnabled || ReadOnly)
        {
            return;
        }

        var cell = (NaviusFieldSegment)sender;
        var index = _cells.IndexOf(cell);
        if (index < 0)
        {
            return;
        }

        var (key, digit) = SegmentKeyMapper.Map(e.Key);
        if (key == SegmentKey.None)
        {
            return;
        }

        var rtl = FlowDirection == FlowDirection.RightToLeft;
        var basis = ResolvePlaceholderBasis(_segments[index].Unit);
        var result = SegmentMath.HandleKey(_segments[index], key, digit, basis, rtl);

        if (!result.Handled)
        {
            return;
        }

        e.Handled = true;

        if (result.Changed)
        {
            RefreshAllCells();
            Commit();
        }

        switch (result.Focus)
        {
            case SegmentFocusMove.Previous:
                FocusSegment(index - 1);
                break;
            case SegmentFocusMove.Next:
                FocusSegment(index + 1);
                break;
        }
    }

    private void OnSegmentValueRequested(object? sender, int value)
    {
        if (!IsEnabled || ReadOnly || sender is not NaviusFieldSegment cell)
        {
            return;
        }

        var index = _cells.IndexOf(cell);
        if (index < 0)
        {
            return;
        }

        var segment = _segments[index];
        segment.Value = SegmentMath.Clamp(value, segment.Min, segment.Max);
        segment.TypeBuffer = string.Empty;

        RefreshAllCells();
        Commit();
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var input = (NaviusTimeInput)d;
        var newValue = (TimeOnly?)e.NewValue;

        if (!input._isInternalSet)
        {
            input.SeedFromValue();
            input.RefreshAllCells();
        }

        input.UpdateComputedState(newValue);
        input.RaiseEvent(new RoutedPropertyChangedEventArgs<TimeOnly?>((TimeOnly?)e.OldValue, newValue, ValueChangedEvent));
    }

    private static void OnLayoutAffectingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusTimeInput)d).RebuildLayout();

    private static void OnBoundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusTimeInput)d).UpdateComputedState(((NaviusTimeInput)d).Value);

    private static void OnFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusTimeInput)d).RefreshAllCells();

    private static void OnReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var input = (NaviusTimeInput)d;
        foreach (var cell in input._cells)
        {
            cell.IsSegmentReadOnly = input.ReadOnly;
        }
    }
}

/// <summary>
/// Maps the root to role="group" plus a read-only ValuePattern surfacing the composed time text
/// (same rationale as NaviusDateInputAutomationPeer / the M3 gate note referenced by time-picker.md).
/// </summary>
public sealed class NaviusTimeInputAutomationPeer : FrameworkElementAutomationPeer, IValueProvider
{
    public NaviusTimeInputAutomationPeer(NaviusTimeInput owner) : base(owner)
    {
    }

    private NaviusTimeInput Input => (NaviusTimeInput)Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override string GetClassNameCore() => nameof(NaviusTimeInput);

    public override object? GetPattern(PatternInterface patternInterface) =>
        patternInterface == PatternInterface.Value ? this : base.GetPattern(patternInterface);

    public bool IsReadOnly => true;

    public string Value => Input.Value?.ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty;

    public void SetValue(string value) =>
        throw new InvalidOperationException("NaviusTimeInput is read-only over ValuePattern; change the value via keyboard.");
}
