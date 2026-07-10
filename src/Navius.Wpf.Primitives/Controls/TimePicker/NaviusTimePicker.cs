using System;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Navius.Wpf.Primitives.Controls.Internal;
using Navius.Wpf.Primitives.Overlays;

namespace Navius.Wpf.Primitives.Controls.TimePicker;

/// <summary>
/// Tier B: custom lookless control composed from Tier-A/B pieces (contract wraps NaviusPopover +
/// NaviusTimeInput). The contract's multi-part shell (Root/Input/Trigger/Content/Column/Option)
/// collapses onto one templated Control -- same "wrap the whole shell in one root Control" choice
/// NaviusSelectBase made for its Trigger+Popup+Item parts -- with PART_Input (a real
/// NaviusTimeInput, bound TwoWay to Value/Granularity/HourCycle/MinuteStep/SecondStep via the
/// template so typed edits and popup selections share one value), PART_Trigger (ToggleButton,
/// IsChecked TwoWay to IsOpen, mirrors NaviusSelectBase's PART_Trigger), PART_Popup
/// (NaviusAnchoredPopup, the substrate named in this batch's brief), and one ListBox per unit
/// (PART_HourColumn/PART_MinuteColumn/PART_SecondColumn/PART_DayPeriodColumn) built in the theme
/// XAML rather than a hand-rolled listbox, since a native ListBox already gives roving tabindex,
/// SelectionPattern, and TextSearch-based typeahead for free (see this family's WPF strategy note
/// and "WPF implementation notes" for the resulting contract deltas).
/// </summary>
[TemplatePart(Name = PartInput, Type = typeof(Navius.Wpf.Primitives.Controls.TimeInput.NaviusTimeInput))]
[TemplatePart(Name = PartTrigger, Type = typeof(ToggleButton))]
[TemplatePart(Name = PartPopup, Type = typeof(NaviusAnchoredPopup))]
[TemplatePart(Name = PartPopupContent, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartHourColumn, Type = typeof(ListBox))]
[TemplatePart(Name = PartMinuteColumn, Type = typeof(ListBox))]
[TemplatePart(Name = PartSecondColumn, Type = typeof(ListBox))]
[TemplatePart(Name = PartDayPeriodColumn, Type = typeof(ListBox))]
public class NaviusTimePicker : Control
{
    private const string PartInput = "PART_Input";
    private const string PartTrigger = "PART_Trigger";
    private const string PartPopup = "PART_Popup";
    private const string PartPopupContent = "PART_PopupContent";
    private const string PartHourColumn = "PART_HourColumn";
    private const string PartMinuteColumn = "PART_MinuteColumn";
    private const string PartSecondColumn = "PART_SecondColumn";
    private const string PartDayPeriodColumn = "PART_DayPeriodColumn";

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(TimeOnly?), typeof(NaviusTimePicker),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen), typeof(bool), typeof(NaviusTimePicker),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsOpenChanged));

    public static readonly DependencyProperty GranularityProperty = DependencyProperty.Register(
        nameof(Granularity), typeof(string), typeof(NaviusTimePicker), new PropertyMetadata("minute", OnColumnsAffectingChanged));

    public static readonly DependencyProperty HourCycleProperty = DependencyProperty.Register(
        nameof(HourCycle), typeof(int?), typeof(NaviusTimePicker), new PropertyMetadata(null, OnColumnsAffectingChanged));

    public static readonly DependencyProperty MinuteStepProperty = DependencyProperty.Register(
        nameof(MinuteStep), typeof(int), typeof(NaviusTimePicker), new PropertyMetadata(1, OnColumnsAffectingChanged));

    public static readonly DependencyProperty SecondStepProperty = DependencyProperty.Register(
        nameof(SecondStep), typeof(int), typeof(NaviusTimePicker), new PropertyMetadata(1, OnColumnsAffectingChanged));

    public static readonly DependencyProperty CultureProperty = DependencyProperty.Register(
        nameof(Culture), typeof(CultureInfo), typeof(NaviusTimePicker), new PropertyMetadata(null, OnColumnsAffectingChanged));

    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueChanged), RoutingStrategy.Bubble,
        typeof(RoutedPropertyChangedEventHandler<TimeOnly?>), typeof(NaviusTimePicker));

    public static readonly RoutedEvent OpenChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(OpenChanged), RoutingStrategy.Bubble,
        typeof(RoutedPropertyChangedEventHandler<bool>), typeof(NaviusTimePicker));

    private TimeInput.NaviusTimeInput? _input;
    private ToggleButton? _trigger;
    private FrameworkElement? _popupContent;
    private ListBox? _hourColumn;
    private ListBox? _minuteColumn;
    private ListBox? _secondColumn;
    private ListBox? _dayPeriodColumn;
    private OverlaySession? _session;
    private bool _syncingColumns;

    static NaviusTimePicker()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusTimePicker), new FrameworkPropertyMetadata(typeof(NaviusTimePicker)));
    }

    public TimeOnly? Value
    {
        get => (TimeOnly?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>Which columns render: "hour", "minute", or "second".</summary>
    public string Granularity
    {
        get => (string)GetValue(GranularityProperty);
        set => SetValue(GranularityProperty, value);
    }

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

    public CultureInfo? Culture
    {
        get => (CultureInfo?)GetValue(CultureProperty);
        set => SetValue(CultureProperty, value);
    }

    public event RoutedPropertyChangedEventHandler<TimeOnly?> ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    public event RoutedPropertyChangedEventHandler<bool> OpenChanged
    {
        add => AddHandler(OpenChangedEvent, value);
        remove => RemoveHandler(OpenChangedEvent, value);
    }

    private CultureInfo EffectiveCulture => Culture ?? CultureInfo.CurrentCulture;

    public int EffectiveHourCycle => SegmentLayoutBuilder.ResolveHourCycle(EffectiveCulture, HourCycle);

    public override void OnApplyTemplate()
    {
        if (_hourColumn is not null)
        {
            _hourColumn.SelectionChanged -= OnColumnSelectionChanged;
        }

        if (_minuteColumn is not null)
        {
            _minuteColumn.SelectionChanged -= OnColumnSelectionChanged;
        }

        if (_secondColumn is not null)
        {
            _secondColumn.SelectionChanged -= OnColumnSelectionChanged;
        }

        if (_dayPeriodColumn is not null)
        {
            _dayPeriodColumn.SelectionChanged -= OnColumnSelectionChanged;
        }

        base.OnApplyTemplate();

        _input = GetTemplateChild(PartInput) as TimeInput.NaviusTimeInput;
        _trigger = GetTemplateChild(PartTrigger) as ToggleButton;
        _popupContent = GetTemplateChild(PartPopupContent) as FrameworkElement;
        _hourColumn = GetTemplateChild(PartHourColumn) as ListBox;
        _minuteColumn = GetTemplateChild(PartMinuteColumn) as ListBox;
        _secondColumn = GetTemplateChild(PartSecondColumn) as ListBox;
        _dayPeriodColumn = GetTemplateChild(PartDayPeriodColumn) as ListBox;

        if (_hourColumn is not null)
        {
            _hourColumn.SelectionChanged += OnColumnSelectionChanged;
        }

        if (_minuteColumn is not null)
        {
            _minuteColumn.SelectionChanged += OnColumnSelectionChanged;
        }

        if (_secondColumn is not null)
        {
            _secondColumn.SelectionChanged += OnColumnSelectionChanged;
        }

        if (_dayPeriodColumn is not null)
        {
            _dayPeriodColumn.SelectionChanged += OnColumnSelectionChanged;
        }

        RebuildColumns();
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusTimePickerAutomationPeer(this);

    private static int ToTwelveHour(int hour24)
    {
        var h = hour24 % 12;
        return h == 0 ? 12 : h;
    }

    private void RebuildColumns()
    {
        if (_hourColumn is null)
        {
            return;
        }

        var hourCycle = EffectiveHourCycle;
        var showMinute = Granularity is "minute" or "second";
        var showSecond = Granularity == "second";
        var showDayPeriod = hourCycle == 12;

        ConfigureColumn(_hourColumn, TimePickerOptionBuilder.Hours(hourCycle), visible: true, "Hours");
        ConfigureColumn(_minuteColumn, TimePickerOptionBuilder.Minutes(MinuteStep), showMinute, "Minutes");
        ConfigureColumn(_secondColumn, TimePickerOptionBuilder.Seconds(SecondStep), showSecond, "Seconds");
        ConfigureColumn(_dayPeriodColumn, TimePickerOptionBuilder.DayPeriods(), showDayPeriod, "AM/PM");

        SyncColumnSelections();
    }

    private static void ConfigureColumn(ListBox? column, System.Collections.Generic.IReadOnlyList<TimePickerOption> options, bool visible, string label)
    {
        if (column is null)
        {
            return;
        }

        column.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (!visible)
        {
            return;
        }

        column.DisplayMemberPath = nameof(TimePickerOption.Text);
        column.SelectedValuePath = nameof(TimePickerOption.Value);
        column.ItemsSource = options;
        AutomationProperties.SetName(column, label);
    }

    private void SyncColumnSelections()
    {
        _syncingColumns = true;
        try
        {
            var value = Value;
            var hourCycle = EffectiveHourCycle;

            if (_hourColumn is not null)
            {
                _hourColumn.SelectedValue = value is null ? null : (hourCycle == 24 ? value.Value.Hour : ToTwelveHour(value.Value.Hour));
            }

            if (_minuteColumn is not null && _minuteColumn.Visibility == Visibility.Visible)
            {
                _minuteColumn.SelectedValue = value?.Minute;
            }

            if (_secondColumn is not null && _secondColumn.Visibility == Visibility.Visible)
            {
                _secondColumn.SelectedValue = value?.Second;
            }

            if (_dayPeriodColumn is not null && _dayPeriodColumn.Visibility == Visibility.Visible)
            {
                _dayPeriodColumn.SelectedValue = value is null ? null : (value.Value.Hour >= 12 ? 1 : 0);
            }
        }
        finally
        {
            _syncingColumns = false;
        }
    }

    private void OnColumnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingColumns || !IsEnabled)
        {
            return;
        }

        if (_hourColumn?.SelectedValue is not int hourSelection)
        {
            return;
        }

        var hourCycle = EffectiveHourCycle;
        var minute = (_minuteColumn is { Visibility: Visibility.Visible } && _minuteColumn.SelectedValue is int m) ? m : 0;
        var second = (_secondColumn is { Visibility: Visibility.Visible } && _secondColumn.SelectedValue is int s) ? s : 0;
        var pm = _dayPeriodColumn is { Visibility: Visibility.Visible } && _dayPeriodColumn.SelectedValue is int dp && dp == 1;

        var hour24 = hourCycle == 24 ? hourSelection : (hourSelection % 12) + (pm ? 12 : 0);
        SetCurrentValue(ValueProperty, new TimeOnly(hour24, minute, second));
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var picker = (NaviusTimePicker)d;
        picker.SyncColumnSelections();
        picker.RaiseEvent(new RoutedPropertyChangedEventArgs<TimeOnly?>((TimeOnly?)e.OldValue, (TimeOnly?)e.NewValue, ValueChangedEvent));
    }

    private static void OnColumnsAffectingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusTimePicker)d).RebuildColumns();

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var picker = (NaviusTimePicker)d;
        if ((bool)e.NewValue)
        {
            picker.OpenCore();
        }
        else
        {
            picker.CloseCore();
        }

        picker.RaiseEvent(new RoutedPropertyChangedEventArgs<bool>((bool)e.OldValue, (bool)e.NewValue, OpenChangedEvent));
    }

    private void OpenCore()
    {
        if (_session is not null || _popupContent is null)
        {
            return;
        }

        var window = Window.GetWindow(this);
        if (window is null)
        {
            return;
        }

        var stack = OverlayStack.GetFor(window);
        _session = stack.Push(_popupContent, new OverlayOptions
        {
            Modal = false,
            CloseOnEscape = true,
            CloseOnOutsideClick = true,
            TrapFocus = false,
            RestoreFocus = false,
        });

        _session.RegisterInputRoot(_popupContent);
        if (_trigger is not null)
        {
            _session.RegisterInputRoot(_trigger);
        }

        if (_input is not null)
        {
            _session.RegisterInputRoot(_input);
        }

        _session.Closed += OnSessionClosed;
        SyncColumnSelections();
    }

    private void CloseCore() => _session?.RequestClose(OverlayCloseReason.Programmatic);

    private void OnSessionClosed(object? sender, EventArgs e)
    {
        if (_session is not null)
        {
            _session.Closed -= OnSessionClosed;
            _session = null;
        }

        SetCurrentValue(IsOpenProperty, false);
    }
}

/// <summary>
/// Maps the root to role="combobox"-equivalent semantics (trigger + popup + value, same shape as
/// NaviusSelectAutomationPeer): ExpandCollapse over IsOpen, plus a read-only ValuePattern
/// surfacing the formatted time -- required per this family's brief, since the M3 gate proved
/// template-only text otherwise exposes nothing over UIA.
/// </summary>
public sealed class NaviusTimePickerAutomationPeer : FrameworkElementAutomationPeer, IValueProvider, IExpandCollapseProvider
{
    public NaviusTimePickerAutomationPeer(NaviusTimePicker owner) : base(owner)
    {
    }

    private NaviusTimePicker Picker => (NaviusTimePicker)Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ComboBox;

    protected override string GetClassNameCore() => nameof(NaviusTimePicker);

    public override object? GetPattern(PatternInterface patternInterface) => patternInterface switch
    {
        PatternInterface.Value or PatternInterface.ExpandCollapse => this,
        _ => base.GetPattern(patternInterface),
    };

    public bool IsReadOnly => true;

    public string Value => Picker.Value?.ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty;

    public void SetValue(string value) =>
        throw new InvalidOperationException("NaviusTimePicker is read-only over ValuePattern; change the value via the input or column selection.");

    public ExpandCollapseState ExpandCollapseState => Picker.IsOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    public void Expand()
    {
        if (Picker.IsEnabled)
        {
            Picker.IsOpen = true;
        }
    }

    public void Collapse() => Picker.IsOpen = false;
}
