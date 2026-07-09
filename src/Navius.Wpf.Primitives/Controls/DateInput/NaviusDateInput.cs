using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using Navius.Wpf.Primitives.Controls.Internal;

namespace Navius.Wpf.Primitives.Controls.DateInput;

/// <summary>
/// Tier B (custom lookless control): no native WPF control edits a date as independently
/// focusable/steppable segments (WPF's own DatePicker edits as one masked text run). This is a
/// lookless Control whose ControlTemplate composes one <see cref="NaviusFieldSegment"/> per
/// year/month/day unit plus non-focusable literal separators (a Panel built directly in code,
/// same "root owns an internal collection of parts" precedent as
/// NaviusOneTimePasswordField.RebuildCells), driven by the pure <see cref="DateTimeSegment"/>/
/// <see cref="SegmentMath"/> engine in Controls/Internal/SegmentEngine.cs.
///
/// Contract deltas from docs/parity/date-input.md, recorded in full under that doc's
/// "WPF implementation notes":
/// - No NaviusBubbleInput/hidden form-mirror input (this repo's Select precedent already drops
///   native-form wiring for Tier B controls; Name stays a marker-only property).
/// - No ambient NaviusField integration (that family isn't ported yet); FocusFirstSegment() is
///   exposed directly instead of Field.FocusControl.
/// - Dir is WPF's native FlowDirection, not a custom string parameter.
/// - Segment DisplayText renders a unit-shorthand placeholder token ("yyyy"/"mm"/"dd") instead of
///   the literal "Empty" aria-valuetext string, WPF's native masked-input idiom.
/// </summary>
[TemplatePart(Name = PartSegments, Type = typeof(Panel))]
public class NaviusDateInput : Control
{
    private const string PartSegments = "PART_Segments";

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(DateOnly?), typeof(NaviusDateInput),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty GranularityProperty = DependencyProperty.Register(
        nameof(Granularity), typeof(string), typeof(NaviusDateInput),
        new PropertyMetadata("day", OnLayoutAffectingChanged));

    public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
        nameof(MinValue), typeof(DateOnly?), typeof(NaviusDateInput), new PropertyMetadata(null, OnBoundChanged));

    public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
        nameof(MaxValue), typeof(DateOnly?), typeof(NaviusDateInput), new PropertyMetadata(null, OnBoundChanged));

    public static readonly DependencyProperty PlaceholderValueProperty = DependencyProperty.Register(
        nameof(PlaceholderValue), typeof(DateOnly?), typeof(NaviusDateInput), new PropertyMetadata(null));

    public static readonly DependencyProperty ReadOnlyProperty = DependencyProperty.Register(
        nameof(ReadOnly), typeof(bool), typeof(NaviusDateInput), new PropertyMetadata(false, OnReadOnlyChanged));

    public static readonly DependencyProperty RequiredProperty = DependencyProperty.Register(
        nameof(Required), typeof(bool), typeof(NaviusDateInput), new PropertyMetadata(false));

    public static readonly DependencyProperty InvalidProperty = DependencyProperty.Register(
        nameof(Invalid), typeof(bool), typeof(NaviusDateInput), new PropertyMetadata(false, OnBoundChanged));

    public static readonly DependencyProperty ForceLeadingZerosProperty = DependencyProperty.Register(
        nameof(ForceLeadingZeros), typeof(bool), typeof(NaviusDateInput), new PropertyMetadata(false, OnFormatChanged));

    public static readonly DependencyProperty CultureProperty = DependencyProperty.Register(
        nameof(Culture), typeof(CultureInfo), typeof(NaviusDateInput), new PropertyMetadata(null, OnLayoutAffectingChanged));

    /// <summary>Marker only (no native-form mirror), matching NaviusSelectBase.Name's precedent.</summary>
    public static new readonly DependencyProperty NameProperty = DependencyProperty.Register(
        nameof(Name), typeof(string), typeof(NaviusDateInput), new PropertyMetadata(null));

    private static readonly DependencyPropertyKey IsFilledPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsFilled), typeof(bool), typeof(NaviusDateInput), new PropertyMetadata(false));

    public static readonly DependencyProperty IsFilledProperty = IsFilledPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsOutOfRangePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsOutOfRange), typeof(bool), typeof(NaviusDateInput), new PropertyMetadata(false));

    public static readonly DependencyProperty IsOutOfRangeProperty = IsOutOfRangePropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsInvalidStatePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsInvalidState), typeof(bool), typeof(NaviusDateInput), new PropertyMetadata(false));

    public static readonly DependencyProperty IsInvalidStateProperty = IsInvalidStatePropertyKey.DependencyProperty;

    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueChanged), RoutingStrategy.Bubble,
        typeof(RoutedPropertyChangedEventHandler<DateOnly?>), typeof(NaviusDateInput));

    private readonly System.Collections.Generic.List<DateTimeSegment> _segments = new();
    private readonly System.Collections.Generic.List<NaviusFieldSegment> _cells = new();
    private Panel? _host;
    private bool _isInternalSet;

    static NaviusDateInput()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusDateInput), new FrameworkPropertyMetadata(typeof(NaviusDateInput)));
        FocusableProperty.OverrideMetadata(typeof(NaviusDateInput), new FrameworkPropertyMetadata(false));
    }

    public DateOnly? Value
    {
        get => (DateOnly?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>"day" (y/m/d), "month" (y/m), or "year".</summary>
    public string Granularity
    {
        get => (string)GetValue(GranularityProperty);
        set => SetValue(GranularityProperty, value);
    }

    public DateOnly? MinValue
    {
        get => (DateOnly?)GetValue(MinValueProperty);
        set => SetValue(MinValueProperty, value);
    }

    public DateOnly? MaxValue
    {
        get => (DateOnly?)GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    public DateOnly? PlaceholderValue
    {
        get => (DateOnly?)GetValue(PlaceholderValueProperty);
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

    /// <summary>Invalid || IsOutOfRange (drives the theme's data-invalid-equivalent trigger).</summary>
    public bool IsInvalidState => (bool)GetValue(IsInvalidStateProperty);

    public event RoutedPropertyChangedEventHandler<DateOnly?> ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    private CultureInfo EffectiveCulture => Culture ?? CultureInfo.CurrentCulture;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _host = GetTemplateChild(PartSegments) as Panel;
        RebuildLayout();
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusDateInputAutomationPeer(this);

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

        var layout = SegmentLayoutBuilder.BuildDateLayout(EffectiveCulture, Granularity);
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

            var model = SegmentLayoutBuilder.CreateSegment(item.Unit, hourCycle: 24, minuteStep: 1, secondStep: 1);
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
        SegmentUnit.Year => "year",
        SegmentUnit.Month => "month",
        SegmentUnit.Day => "day",
        _ => unit.ToString(),
    };

    private void SeedFromValue()
    {
        var value = Value;
        foreach (var segment in _segments)
        {
            segment.Value = value is null ? null : segment.Unit switch
            {
                SegmentUnit.Year => value.Value.Year,
                SegmentUnit.Month => value.Value.Month,
                SegmentUnit.Day => value.Value.Day,
                _ => segment.Value,
            };
            segment.TypeBuffer = string.Empty;
        }

        RecomputeDayMaxIfPresent();
    }

    private void RecomputeDayMaxIfPresent()
    {
        var day = _segments.FirstOrDefault(s => s.Unit == SegmentUnit.Day);
        if (day is null)
        {
            return;
        }

        var year = _segments.FirstOrDefault(s => s.Unit == SegmentUnit.Year)?.Value;
        var month = _segments.FirstOrDefault(s => s.Unit == SegmentUnit.Month)?.Value;
        DateSegmentComposer.RecomputeDayMax(day, year, month);
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

    private DateOnly? ComposeValue()
    {
        var year = _segments.FirstOrDefault(s => s.Unit == SegmentUnit.Year);
        var month = _segments.FirstOrDefault(s => s.Unit == SegmentUnit.Month);
        var day = _segments.FirstOrDefault(s => s.Unit == SegmentUnit.Day);
        return DateSegmentComposer.Compose(year, month, day);
    }

    private int ResolvePlaceholderBasis(SegmentUnit unit)
    {
        var basis = PlaceholderValue ?? DateOnly.FromDateTime(DateTime.Today);
        return unit switch
        {
            SegmentUnit.Year => basis.Year,
            SegmentUnit.Month => basis.Month,
            SegmentUnit.Day => basis.Day,
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

    private void UpdateComputedState(DateOnly? value)
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
            RecomputeDayMaxIfPresent();
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

        RecomputeDayMaxIfPresent();
        RefreshAllCells();
        Commit();
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var input = (NaviusDateInput)d;
        var newValue = (DateOnly?)e.NewValue;

        if (!input._isInternalSet)
        {
            input.SeedFromValue();
            input.RefreshAllCells();
        }

        input.UpdateComputedState(newValue);
        input.RaiseEvent(new RoutedPropertyChangedEventArgs<DateOnly?>((DateOnly?)e.OldValue, newValue, ValueChangedEvent));
    }

    private static void OnLayoutAffectingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusDateInput)d).RebuildLayout();

    private static void OnBoundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusDateInput)d).UpdateComputedState(((NaviusDateInput)d).Value);

    private static void OnFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusDateInput)d).RefreshAllCells();

    private static void OnReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var input = (NaviusDateInput)d;
        foreach (var cell in input._cells)
        {
            cell.IsSegmentReadOnly = input.ReadOnly;
        }
    }
}

/// <summary>
/// Maps the root to role="group" plus a read-only ValuePattern surfacing the composed ISO date
/// (same rationale as NaviusSelectAutomationPeer / time-picker.md's ValuePattern note: template-only
/// text otherwise exposes nothing over UIA).
/// </summary>
public sealed class NaviusDateInputAutomationPeer : FrameworkElementAutomationPeer, IValueProvider
{
    public NaviusDateInputAutomationPeer(NaviusDateInput owner) : base(owner)
    {
    }

    private NaviusDateInput Input => (NaviusDateInput)Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override string GetClassNameCore() => nameof(NaviusDateInput);

    public override object? GetPattern(PatternInterface patternInterface) =>
        patternInterface == PatternInterface.Value ? this : base.GetPattern(patternInterface);

    public bool IsReadOnly => true;

    public string Value => Input.Value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;

    public void SetValue(string value) =>
        throw new InvalidOperationException("NaviusDateInput is read-only over ValuePattern; change the value via keyboard.");
}
