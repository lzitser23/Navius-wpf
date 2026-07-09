using System.Windows;

namespace Navius.Wpf.Primitives.Controls.Calendar;

/// <summary>
/// Tier A: derives from the native <see cref="System.Windows.Controls.Calendar"/>, inheriting its
/// keyboard model (arrow-key day navigation, PageUp/PageDown month paging, Home/End, Enter/Space
/// commit) and its <c>CalendarAutomationPeer</c> for free, and supplies a token-driven default
/// template (Themes/Calendar.xaml) that restyles the framework's own CalendarItem/CalendarDayButton/
/// CalendarButton parts to the one-ink brand (hairline borders, no shadows).
///
/// <see cref="System.Windows.Controls.Calendar.SelectionMode"/> is the native property: single-date
/// selection (the default, <c>CalendarSelectionMode.SingleDate</c>) is used by both
/// <c>NaviusDatePicker</c> and <c>NaviusDateRangePicker</c>; the range picker layers its own
/// two-pick commit state machine on top of single-date commits rather than switching to
/// <c>CalendarSelectionMode.SingleRange</c>, because that native mode's Shift-to-extend semantics
/// do not match the contract's plain "first pick sets start, second sets end" model. See
/// docs/parity/date-range-picker.md "WPF implementation notes" for the full decision record.
/// </summary>
public class NaviusCalendar : System.Windows.Controls.Calendar
{
    static NaviusCalendar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusCalendar),
            new FrameworkPropertyMetadata(typeof(NaviusCalendar)));
    }
}
