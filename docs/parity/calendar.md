# Calendar

**No web extraction of its own.** The web Navius primitives library has no Calendar family: on
the web side the calendar surface is `ZitsCalendar`, a styled-layer component outside the
primitives repo entirely, referenced only by delegation from the date-range-picker family (whose
extraction records that the range-selection grid has "no source in `Navius.Primitives` to port at
all"; see `date-range-picker.md`). The web contract source for this document is therefore the
date-range-picker family composition (including the `data-range-start`/`data-range-end`/
`data-range-middle` styling contract implied by the web `tests/e2e/specs/dates.spec.ts`) plus the
native WPF `Calendar` this control derives from. In the WPF registry, `calendar` is an
independent public item consumed by both pickers, so it gets this standalone document (previously
its material was folded into `date-range-picker.md`; moved out 2026-07-11 per the coverage rule
in `README.md`).

## Parts

| Part | Type | Purpose |
|---|---|---|
| `NaviusCalendar` | `class : System.Windows.Controls.Calendar` | The whole family: one class in `Controls/Calendar/NaviusCalendar.cs`. Its static constructor overrides `DefaultStyleKey` and it adds no other members; everything else is the native control. |

Theme parts (`Themes/Calendar.xaml`), all keyed styles rather than implicit:

| Resource | Targets | Purpose |
|---|---|---|
| `Navius.Calendar.Style` | `NaviusCalendar` | Root style: popover-card chrome (`Navius.Popover` background, 1px `Navius.Border`, `Navius.Radius.Card`), wires the three part styles below through the native `CalendarDayButtonStyle`/`CalendarButtonStyle`/`CalendarItemStyle` properties. Exposed both keyed (for explicit references inside the picker templates) and as an implicit `BasedOn` alias (for standalone page use). |
| `Navius.Calendar.DayButtonStyle` | `CalendarDayButton` | One day cell in month view: today/selected/outside-month/blackout states. |
| `Navius.Calendar.MonthButtonStyle` | `CalendarButton` | One month/year cell in the year and decade views. |
| `Navius.Calendar.ItemStyle` | `CalendarItem` | Header (`PART_PreviousButton` / `PART_HeaderButton` / `PART_NextButton`) plus empty `PART_MonthView`/`PART_YearView` grids that Calendar's own population code fills, and `PART_DisabledVisual`. Contains the `DayTitleTemplate` DataTemplate resource, looked up by name from `CalendarItem.OnApplyTemplate` for the day-of-week header row. |
| `Navius.Calendar.ChromeButton` | `Button` | Shared chrome for the three header buttons. |

**Why keyed, not implicit** (moved from `date-range-picker.md`'s part-mapping table): the part
styles ride the native Calendar's own `CalendarDayButtonStyle`/`CalendarButtonStyle`/
`CalendarItemStyle` properties so that Calendar's internal population code stamps them onto every
`CalendarDayButton`/`CalendarButton`/`CalendarItem` it creates. That keeps working after
`NaviusAnchoredPopup` reparents the picker popup content into a `Popup`, where ambient
implicit-style lookup is unreliable (the rationale comment at the top of `Themes/Calendar.xaml`).

## Parameters

None added. `NaviusCalendar` declares no dependency properties of its own; the public surface is
the native `Calendar`'s. Native properties this codebase actively uses:

| Native property | Used how |
|---|---|
| `SelectionMode` | Native default `SingleDate` (asserted by `CalendarTests.DerivesFromNativeCalendar_WithSingleDateDefault`). `NaviusDatePicker.OnOpened` sets `SingleDate`; `NaviusDateRangePicker.OnOpened` switches its instance to `SingleRange` while open, purely so both endpoints and the days between render as selected. Display-only: commits never go through `SingleRange`'s native Shift-to-extend semantics (see `date-range-picker.md`, "Commit model"). |
| `SelectedDate` / `SelectedDates` | Written by the pickers to seed and repaint selection. Read as a commit input only in one place: on an Enter/Space pick, the shared base reads `SelectedDate` after the native class handler has already moved it to the focused day (see `date-picker.md`, "Keyboard"). |
| `DisplayDate` | Seeded on open to the current value or `DateTime.Today`. |
| `CalendarDayButtonStyle` / `CalendarButtonStyle` / `CalendarItemStyle` | Set by `Navius.Calendar.Style` to the keyed part styles above. |

## Events

None added. The native `SelectedDatesChanged` event exists but is deliberately NOT used as a
commit source by the pickers: the native calendar moves its selection on every arrow key, and the
picker contract wants arrow keys to navigate without committing (doc comment on
`NaviusDatePickerBase`, `Controls/DatePicker/NaviusDatePickerBase.cs`).

## State + visual states

WPF triggers standing in for the web calendar's data attributes (`Themes/Calendar.xaml`):

| Trigger | Element | Effect | Web analogue (per the XAML comments) |
|---|---|---|---|
| `IsInactive` | day/month cell | Opacity 0.35 | `data-outside-month` |
| `IsMouseOver` | day/month cell, chrome buttons | `Navius.Accent` fill (+ `Navius.AccentForeground` on cells) | hover |
| `IsKeyboardFocused` | day/month cell, chrome buttons | Hairline `Navius.Ring` border, no glow | focus ring |
| `IsToday` | day cell | Hairline `Navius.Ring` border, no fill (selected wins) | `data-today` |
| `IsSelected` | day cell | `Navius.Primary` fill + `Navius.PrimaryForeground` | `data-selected`, and the range fill for `SingleRange` selections |
| `IsBlackedOut` | day cell | Opacity 0.3 | blackout dates |
| `HasSelectedDays` | month/year cell | `Navius.Primary` fill (`CalendarButton` exposes `HasSelectedDays`, not `IsSelected`) | selection in zoomed-out views |
| `IsEnabled` = False | cells 0.4 opacity; `CalendarItem` shows `PART_DisabledVisual` | | `data-disabled` |

## Keyboard

Inherited entirely from the native control; `NaviusCalendar` adds no key handling of its own. As
described by this repo's own sources: arrow keys move by day, PageUp/PageDown page by month,
Home/End, Enter/Space select, and the header button zooms out to year and decade views
(`NaviusCalendar.cs` doc comment; `apps/Navius.Wpf.Gallery/Pages/CalendarPage.xaml` intro text).

Verification status, stated plainly (this is the M6 audit posture recorded in
`date-range-picker.md`): the in-grid navigation model is asserted only as "inherited from native
Calendar behavior". No test in this repo independently drives arrows, PageUp/PageDown, Home, or
End. In particular the exact Home/End target is unverified, and the in-repo descriptions are not
consistent about it: `CalendarPage.xaml` (and `date-range-picker.md`'s pre-split wording) say
"the week's edges" while `NaviusCalendar.cs` leaves "Home/End" unqualified. Treat the Home/End
wording as unconfirmed
until someone drives it with a test. Enter/Space picks ARE exercised, but at the picker layer:
the shared base observes them with `handledEventsToo` handlers (see `date-picker.md`).

## Accessibility

- No custom `AutomationPeer`: the native `CalendarAutomationPeer` comes with the base class.
  `CalendarTests.AutomationPeer_IsTheNativeCalendarPeer` asserts the peer exists and reports
  `AutomationControlType.Calendar`.
- Everything else (cell peers, selection patterns) is the native, Microsoft-owned peer tree; this
  repo neither modifies nor independently verifies it.

## WPF implementation notes

Tier A (derives from a native WPF control). Shipped 2026-07-09 in the same batch as the two
pickers; this section was extracted from `date-range-picker.md`'s "WPF implementation notes" on
2026-07-11 with no behavior change.

- **Why Tier A**: deriving `System.Windows.Controls.Calendar` inherits the whole keyboard model
  and `CalendarAutomationPeer` for free; the Navius layer is the token re-style only (hairline
  borders, no shadows, one-ink brand, per the hard rule comment in `Themes/Calendar.xaml`).
- **Theme merge**: `Themes/Calendar.xaml` is merged into `Themes/Generic.xaml` directly, and is
  also reachable transitively via `Themes/DatePicker.xaml` and `Themes/DateRangePicker.xaml`
  (which merge it the way `Themes/ContextMenu.xaml` merges `Themes/Menu.xaml`). Consumers do not
  need to merge it themselves. The file header previously claimed the opposite; the M6 audit
  (2026-07-09) corrected it (full entry in `date-range-picker.md`, "M6 audit").
- **Registry**: `calendar` in `registry/registry.json`, `registry:primitive`, no
  `registryDependencies`, shipping `NaviusCalendar.cs` + `Themes/Calendar.xaml`. Both
  `date-picker` and `date-range-picker` list `calendar` as a registry dependency.
- **Consumers**: both pickers template a `NaviusCalendar` as `PART_Calendar` inside their
  `NaviusAnchoredPopup` (`Themes/DatePicker.xaml`, `Themes/DateRangePicker.xaml`), referencing
  `Navius.Calendar.Style` explicitly.
- **Gallery**: `apps/Navius.Wpf.Gallery/Pages/CalendarPage.xaml` shows a single-date instance and
  a second instance in `SelectionMode="SingleRange"`.
- **Tests**: `tests/Navius.Wpf.Tests/CalendarTests.cs` (derivation + `SingleDate` default,
  `ApplyTemplate` wires the three part styles, the implicit alias is `BasedOn` the shared keyed
  style, native peer control type). `DateRangePickerTests.OnOpened_WithRealCalendarPart_SwitchesToSingleRangeAndSyncsSelectionWithoutThrowing`
  additionally exercises a real templated instance accepting the `SingleRange` switch and both
  selection-write shapes.

## M6 audit (2026-07-09)

This family shipped and was audited as part of the date-range-picker batch; the audit log lives
in `date-range-picker.md` ("M6 audit"). The two entries touching this family, summarized:

- `NaviusCalendar.cs`'s own doc comment falsely claimed the range picker layers its commit state
  machine "rather than switching to `CalendarSelectionMode.SingleRange`"; the shipped
  `NaviusDateRangePicker.OnOpened` does switch modes. The stale comment was fixed (CONFIRMED,
  fixed).
- `Themes/Calendar.xaml`'s header claimed it was not merged into `Generic.xaml`; both the claim
  and its cited precedent were false. Corrected (CONFIRMED, fixed, doc-only).

## Residuals (2026-07-11 docs pass)

- `apps/Navius.Wpf.Gallery/Pages/CalendarPage.xaml` still carries the pre-audit resource comment
  "Not merged into Generic.xaml (out of scope here)", which `Themes/Generic.xaml`'s merge list
  contradicts. Comment-only, no functional impact; not fixed here (this change is docs-only).
- The Home/End keyboard wording inconsistency described under "Keyboard" above remains untested.
