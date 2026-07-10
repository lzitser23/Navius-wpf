using System;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.DatePicker;

/// <summary>
/// Tier B single-date picker: trigger button + anchored popup hosting a <see cref="Controls.Calendar.NaviusCalendar"/>
/// in SingleDate mode. Arrow keys navigate the calendar without committing; a day click or
/// Enter/Space commits <see cref="Value"/> and closes; Escape or an outside press dismisses
/// without committing. Not derived from the native <see cref="System.Windows.Controls.DatePicker"/>:
/// its DatePickerTextBox editing surface belongs to the DateInput family (a separate family this
/// wave), and its popup does not route through this repo's OverlayStack dismissal substrate.
/// </summary>
public class NaviusDatePicker : NaviusDatePickerBase
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(DateTime?), typeof(NaviusDatePicker),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    /// <summary>Fires on every committed pick (contract's ValueChanged).</summary>
    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NaviusDatePicker));

    static NaviusDatePicker()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusDatePicker),
            new FrameworkPropertyMetadata(typeof(NaviusDatePicker)));
    }

    /// <summary>The committed date (date component only). Use two-way binding.</summary>
    public DateTime? Value
    {
        get => (DateTime?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public event RoutedEventHandler ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusDatePickerAutomationPeer(this);

    protected override void OnOpened()
    {
        if (CalendarPart is null)
        {
            return;
        }

        CalendarPart.SelectionMode = CalendarSelectionMode.SingleDate;
        CalendarPart.SelectedDate = Value;
        CalendarPart.DisplayDate = Value ?? DateTime.Today;
    }

    protected override void OnPickCommitted(DateTime day)
    {
        Value = day;
        RaiseEvent(new RoutedEventArgs(ValueChangedEvent, this));
        IsOpen = false;
    }

    protected override void UpdateDisplay() =>
        SetDisplay(Value is DateTime value ? FormatDate(value) : Placeholder, Value is not null);

    /// <summary>ValuePattern text: the formatted date, or empty while unset (never the placeholder).</summary>
    internal string FormatValueText() => Value is DateTime value ? FormatDate(value) : string.Empty;

    internal static string FormatDate(DateTime value) => value.ToString("d", CultureInfo.CurrentCulture);

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusDatePicker)d).UpdateDisplay();
}

/// <summary>
/// Peer mirroring the native WPF DatePickerAutomationPeer's shape (ControlType Custom with a
/// "date picker" localized control type) plus the two patterns the M3 gate showed template-only
/// controls need: a read-only ValuePattern surfacing the formatted date and ExpandCollapse over
/// IsOpen (the web trigger's aria-haspopup="dialog"/aria-expanded).
/// </summary>
public class NaviusDatePickerAutomationPeer : FrameworkElementAutomationPeer, IValueProvider, IExpandCollapseProvider
{
    public NaviusDatePickerAutomationPeer(NaviusDatePicker owner) : base(owner)
    {
    }

    private NaviusDatePicker Picker => (NaviusDatePicker)Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;

    protected override string GetLocalizedControlTypeCore() => "date picker";

    protected override string GetClassNameCore() => nameof(NaviusDatePicker);

    public override object? GetPattern(PatternInterface patternInterface) => patternInterface switch
    {
        PatternInterface.Value or PatternInterface.ExpandCollapse => this,
        _ => base.GetPattern(patternInterface),
    };

    public bool IsReadOnly => true;

    public string Value => Picker.FormatValueText();

    public void SetValue(string value) =>
        throw new InvalidOperationException("NaviusDatePicker is read-only over ValuePattern; pick a date via the calendar.");

    public ExpandCollapseState ExpandCollapseState =>
        Picker.IsOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    public void Expand()
    {
        if (Picker.IsEnabled)
        {
            Picker.IsOpen = true;
        }
    }

    public void Collapse() => Picker.IsOpen = false;
}
