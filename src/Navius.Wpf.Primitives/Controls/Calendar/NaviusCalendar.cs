using System.Windows;

namespace Navius.Wpf.Primitives.Controls.Calendar;

/// <summary>
/// Tier A: derives from the native <see cref="System.Windows.Controls.Calendar"/>, inheriting its
/// keyboard model (arrow-key day navigation, PageUp/PageDown month paging, Home/End, Enter/Space
/// commit) and its <c>CalendarAutomationPeer</c> for free, and supplies a token-driven default
/// template (Themes/Calendar.xaml) that restyles the framework's own CalendarItem/CalendarDayButton/
/// CalendarButton parts to the one-ink brand (hairline borders, no shadows).
///
/// <see cref="System.Windows.Controls.Calendar.SelectionMode"/> is the native property:
/// <c>NaviusDatePicker</c> uses the default single-date selection (<c>CalendarSelectionMode.SingleDate</c>).
/// <c>NaviusDateRangePicker</c> switches its <see cref="NaviusCalendar"/> instance to
/// <c>CalendarSelectionMode.SingleRange</c> while open purely so both endpoints and the days
/// between them render as selected (the web data-range-start/-end/-middle styling); it layers its
/// own two-pick commit state machine (<c>DateRangeCommitEngine</c>) on top for the actual "first
/// pick sets start, second sets end" logic, so commits never go through SingleRange's native
/// Shift-to-extend semantics. See docs/parity/date-range-picker.md "WPF implementation notes" for
/// the full decision record.
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
