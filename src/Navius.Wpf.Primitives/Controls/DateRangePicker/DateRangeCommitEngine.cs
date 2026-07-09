using System;

namespace Navius.Wpf.Primitives.Controls.DateRangePicker;

/// <summary>
/// Pure range-commit state machine backing <see cref="NaviusDateRangePicker"/>'s contract: the
/// first day picked (click or Enter/Space on a calendar day) sets Start; the second sets End,
/// ordered so Start &lt;= End; a pick after a complete range starts a fresh range rather than
/// extending it. No WPF dependency, so it is unit-testable without STA. The Esc side of the
/// contract ("Esc reverts both") is a snapshot restore owned by the control, not a state
/// transition here. See docs/parity/date-range-picker.md "WPF implementation notes" for why this
/// replaces native <c>CalendarSelectionMode.SingleRange</c> commit semantics (whose
/// Shift-to-extend model does not match "plain second pick sets end").
/// </summary>
public static class DateRangeCommitEngine
{
    public static NaviusDateRange Commit(NaviusDateRange current, DateTime day)
    {
        if (current.Start is null || current.IsComplete)
        {
            return new NaviusDateRange(day, null);
        }

        return day < current.Start
            ? new NaviusDateRange(day, current.Start)
            : new NaviusDateRange(current.Start, day);
    }
}
