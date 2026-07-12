using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls.DatePicker;

namespace Navius.Wpf.Primitives.Controls.DateRangePicker;

/// <summary>
/// Tier B range picker: the same trigger + anchored popup + <see cref="Controls.Calendar.NaviusCalendar"/>
/// anatomy as <see cref="NaviusDatePicker"/>, with the two-pick commit model from the contract on
/// top: the first pick (click or Enter/Space on a day) sets Start, the second sets End (ordered by
/// <see cref="DateRangeCommitEngine"/>) and closes; Escape reverts BOTH endpoints to their state
/// when the popup opened, then closes. The calendar runs in SingleRange mode purely so both
/// endpoints and the days between them render as selected (the web data-range-start/-end/-middle
/// styling); commits never go through the native mode's Shift-to-extend semantics.
/// </summary>
public class NaviusDateRangePicker : NaviusDatePickerBase
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(NaviusDateRange), typeof(NaviusDateRangePicker),
        new FrameworkPropertyMetadata(NaviusDateRange.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    /// <summary>Fires on every committed endpoint change: first pick (start), second pick (end),
    /// and an Escape revert that actually changed the value (contract's ValueChanged).</summary>
    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NaviusDateRangePicker));

    private NaviusDateRange _valueAtOpen;
    private bool _syncingCalendar;

    static NaviusDateRangePicker()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusDateRangePicker),
            new FrameworkPropertyMetadata(typeof(NaviusDateRangePicker)));
    }

    /// <summary>The committed range (either endpoint may be null mid-pick). Use two-way binding.</summary>
    public NaviusDateRange Value
    {
        get => (NaviusDateRange)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public event RoutedEventHandler ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusDateRangePickerAutomationPeer(this);

    protected override void OnOpened()
    {
        _valueAtOpen = Value;

        if (CalendarPart is null)
        {
            return;
        }

        CalendarPart.SelectionMode = CalendarSelectionMode.SingleRange;
        SyncCalendarSelection();
        CalendarPart.DisplayDate = Value.Start ?? DateTime.Today;
    }

    protected override void OnPickCommitted(DateTime day)
    {
        Value = DateRangeCommitEngine.Commit(Value, day);
        RaiseEvent(new RoutedEventArgs(ValueChangedEvent, this));

        if (Value.IsComplete)
        {
            IsOpen = false;
        }
    }

    protected override void CancelAndClose()
    {
        if (Value != _valueAtOpen)
        {
            Value = _valueAtOpen;
            RaiseEvent(new RoutedEventArgs(ValueChangedEvent, this));
        }

        base.CancelAndClose();
    }

    protected override void UpdateDisplay()
    {
        var text = FormatValueText();
        SetDisplay(text.Length > 0 ? text : Placeholder, !Value.IsEmpty);
    }

    /// <summary>ValuePattern text per the contract: "start - end" (formatted), "start - " while
    /// the second pick is pending, or empty while unset (never the placeholder).</summary>
    internal string FormatValueText() => Value switch
    {
        { Start: DateTime start, End: DateTime end } =>
            $"{NaviusDatePicker.FormatDate(start)} - {NaviusDatePicker.FormatDate(end)}",
        { Start: DateTime start } => $"{NaviusDatePicker.FormatDate(start)} - ",
        _ => string.Empty,
    };

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NaviusDateRangePicker)d;
        control.UpdateDisplay();

        if (control.IsOpen)
        {
            control.SyncCalendarSelection();
        }
    }

    // Repaints the calendar's SelectedDates from the committed range so start/end/middle days all
    // show as selected. Guarded because SelectedDates mutation re-enters SelectedDatesChanged
    // internally; commits never read calendar selection state, only pick events, so this is
    // display-only.
    private void SyncCalendarSelection()
    {
        if (CalendarPart is null || _syncingCalendar || CalendarPart.SelectionMode != CalendarSelectionMode.SingleRange)
        {
            return;
        }

        _syncingCalendar = true;
        try
        {
            CalendarPart.SelectedDates.Clear();
            if (Value is { Start: DateTime start, End: DateTime end })
            {
                CalendarPart.SelectedDates.AddRange(start, end);
            }
            else if (Value.Start is DateTime startOnly)
            {
                CalendarPart.SelectedDate = startOnly;
            }
        }
        finally
        {
            _syncingCalendar = false;
        }
    }
}

/// <summary>
/// Same peer shape as <see cref="NaviusDatePickerAutomationPeer"/> ("date range picker" localized
/// control type): read-only ValuePattern surfacing "start - end" and ExpandCollapse over IsOpen.
/// </summary>
public class NaviusDateRangePickerAutomationPeer : FrameworkElementAutomationPeer, IValueProvider, IExpandCollapseProvider
{
    public NaviusDateRangePickerAutomationPeer(NaviusDateRangePicker owner) : base(owner)
    {
    }

    private NaviusDateRangePicker Picker => (NaviusDateRangePicker)Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;

    protected override string GetLocalizedControlTypeCore() => "date range picker";

    protected override string GetClassNameCore() => nameof(NaviusDateRangePicker);

    public override object? GetPattern(PatternInterface patternInterface) => patternInterface switch
    {
        PatternInterface.Value or PatternInterface.ExpandCollapse => this,
        _ => base.GetPattern(patternInterface),
    };

    public bool IsReadOnly => true;

    public string Value => Picker.FormatValueText();

    public void SetValue(string value) =>
        throw new InvalidOperationException("NaviusDateRangePicker is read-only over ValuePattern; pick dates via the calendar.");

    public ExpandCollapseState ExpandCollapseState =>
        Picker.IsOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    public void Expand()
    {
        ThrowIfDisabled();
        Picker.IsOpen = true;
    }

    public void Collapse()
    {
        ThrowIfDisabled();
        Picker.IsOpen = false;
    }

    private void ThrowIfDisabled()
    {
        if (!Picker.IsEnabled)
        {
            throw new ElementNotEnabledException();
        }
    }
}
