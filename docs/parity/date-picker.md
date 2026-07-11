# DatePicker

**No web extraction of its own.** The web Navius primitives library has no standalone DatePicker
family: the parity set's nearest web relatives are `date-input` (the segmented editor, see
`date-input.md`) and `date-range-picker` (the popup composite, see `date-range-picker.md`). The
web contract source for this document is the date-range-picker family composition: `NaviusDatePicker`
is the same trigger + anchored popup + calendar anatomy that composite's extraction specified,
applied to a single date. In the WPF registry, `date-picker` is an independent public item with
its own API (`Value` is a `DateTime?`, not a range), so it gets this standalone document
(previously its material was folded into `date-range-picker.md`; moved out 2026-07-11 per the
coverage rule in `README.md`). This document also owns the shared base class
`NaviusDatePickerBase`, which lives in this family's folder and which `NaviusDateRangePicker`
derives from.

## Parts

| Part | Type | Purpose |
|---|---|---|
| `NaviusDatePicker` | `class : NaviusDatePickerBase` | Tier B single-date picker (`Controls/DatePicker/NaviusDatePicker.cs`): owns `Value`, seeds the calendar in `SingleDate` mode on open, commits and closes on a pick. |
| `NaviusDatePickerBase` | `abstract class : Control` | Shared lookless base (`Controls/DatePicker/NaviusDatePickerBase.cs`), also derived by `NaviusDateRangePicker`: owns the open/close lifecycle, the `OverlayStack` session, popup placement properties, display-text plumbing, and pick detection. |
| `NaviusDatePickerAutomationPeer` | `class : FrameworkElementAutomationPeer, IValueProvider, IExpandCollapseProvider` | UIA surface, see "Accessibility". |

Template parts (`[TemplatePart]` on the base; wired by `Themes/DatePicker.xaml`):

| Part name | Type | Purpose |
|---|---|---|
| `PART_Trigger` | `ToggleButton` | The trigger; `IsChecked` two-way bound to `IsOpen` in the default template. Shows `DisplayText` plus a hairline calendar glyph. |
| `PART_Popup` | `NaviusAnchoredPopup` | Anchored to the trigger; `Side`/`Align`/`SideOffset`/`AlignOffset` template-bound from the picker root. |
| `PART_PopupContent` | `FrameworkElement` | The overlay input root. In the default theme a transparent, non-focusable `Border`: the calendar card is itself the popup surface (its own hairline border, no extra chrome). |
| `PART_Calendar` | `NaviusCalendar` | The calendar surface, referenced with the keyed `Navius.Calendar.Style`. See `calendar.md`. |

## Parameters

Declared on `NaviusDatePicker`:

| Name | Type | Default | Notes |
|---|---|---|---|
| `Value` | `DateTime?` | `null` | The committed date; two-way binds by default. A commit stores the date component only (`day.Date` in `OnPickCommitted`). |

Declared on `NaviusDatePickerBase` (shared with the range picker):

| Name | Type | Default | Notes |
|---|---|---|---|
| `IsOpen` | `bool` | `false` | Two-way binds by default; opening seeds the calendar, engages the overlay, and defers focus to the calendar. |
| `Placeholder` | `string?` | `null` | Muted trigger text while nothing is selected (the web `data-placeholder` styling, per the property's doc comment). |
| `Side` | `PlacementSide` | `Bottom` | Popup side. |
| `Align` | `PlacementAlign` | `Start` | Overridden default (the Select-family precedent; the web `DateRangePickerContent` default is `Align="start"`, per the source comment). |
| `SideOffset` | `double` | `6` | Popup offset. Differs from the web composite Content's default of `4` (see `date-range-picker.md`, "Parameters"); previously unrecorded delta, noted under "Residuals". |
| `AlignOffset` | `double` | `0` | Popup alignment offset. |
| `DisplayText` | `string?` (read-only DP) | `null` | Resolved trigger label: the formatted value, or the placeholder. A plain read-only display, not an editable segment field: segmented editing is owned by the DateInput family, and composing the two is a recorded follow-up (source doc comment). |
| `HasSelection` | `bool` (read-only DP) | `false` | True when a value is set; drives the placeholder-muting trigger styling. |

Formatting: `"d"` pattern with `CultureInfo.CurrentCulture` (`NaviusDatePicker.FormatDate`).

## Events

| Event | Signature | Fires when |
|---|---|---|
| `ValueChanged` | `RoutedEvent`, `RoutingStrategy.Bubble`, `RoutedEventHandler` | On every committed pick (`OnPickCommitted` sets `Value`, raises the event, then closes). Verified by `DatePickerTests.PickCommit_SetsValue_RaisesChanged_AndCloses`. |

Setting `Value` directly (binding or code) updates the display but does not raise `ValueChanged`;
only pick commits raise it.

## State + visual states

Template triggers standing in for the web data attributes (`Themes/DatePicker.xaml`):

| Trigger | Effect | Web analogue (per the XAML comments) |
|---|---|---|
| `HasSelection` = False | Label foreground drops to `Navius.MutedForeground` | `data-placeholder` |
| `IsEnabled` = False | `PART_Trigger` opacity 0.5 | `data-disabled` |
| Trigger `IsMouseOver` / `IsKeyboardFocused` / `IsChecked` | Border brush switches to `Navius.Ring` | hover/focus/open ring |

## Keyboard

All key handling lives on the base (`NaviusDatePickerBase`); traceability per row:

| Key | Behavior | Handler / verification |
|---|---|---|
| Enter / Space / ArrowDown, while closed | Opens the popup (no-op when `IsEnabled` is false) | `HandlePreviewKeyDown`; unit-tested (`DatePickerTests.ClosedTrigger_OpenKeys_OpenThePopup`, and the range-picker twin) |
| Escape, while open | `CancelAndClose()`: for this picker the base default, close without committing | `HandlePreviewKeyDown` plus `HandlePopupPreviewKeyDown`: the popup content lives in the `Popup`'s own `HwndSource`, so its key events never tunnel through the control's `PreviewKeyDown` and Escape needs its own hook there (source comment). Unit-tested (`DatePickerTests.Escape_ClosesWithoutCommitting`) |
| Arrows / PageUp / PageDown / Home / End, in the grid | Native `NaviusCalendar` navigation, without committing | Inherited native behavior, NOT independently test-driven; see `calendar.md`, "Keyboard" |
| Enter / Space, in the grid | Commits the focused day and closes | `OnCalendarKeyDown` reads `CalendarPart.SelectedDate`, which the native class handler has just moved to the focused day. Attached with `handledEventsToo` because Calendar's class handlers mark the event handled (source comment) |

Pointer equivalents (same commit path):

- A left mouse-up on a `CalendarDayButton` commits (`OnCalendarMouseUp` walks the visual tree via
  `FindDayButton`, stopping at the `Calendar` boundary, and reads the button's `DataContext`
  date). Also attached with `handledEventsToo`.
- Before commit detection, the handler releases mouse capture held by `CalendarItem` (the classic
  native WPF DatePicker popup quirk that otherwise swallows the next click anywhere in the
  window; source comment).
- An outside press dismisses without committing, via the `OverlayStack` session
  (`CloseOnOutsideClick = true`). The trigger and the popup content are both registered as input
  roots so a press on the trigger while open counts as "inside" and does not race the trigger's
  own toggle into a close-then-reopen (the Select precedent, per the source comment). Asserted by
  code reading only, not independently driven by a test (the posture the M6 audit recorded for
  the range picker applies equally here).

Design note (from the base's doc comment): pick detection deliberately does NOT listen to
`Calendar.SelectedDatesChanged`, because the native calendar moves selection on every arrow key
and the contract wants arrow keys to navigate without committing.

## Focus and overlay behavior

From `NaviusDatePickerBase`:

- **Open** (`OpenCore`): `OnOpened()` seeds the calendar first (`SingleDate`,
  `SelectedDate = Value`, `DisplayDate = Value ?? DateTime.Today` in this picker's override), then
  the overlay engages, then focus is deferred to the calendar at `DispatcherPriority.Input`
  because the `Popup`'s `HwndSource` does not exist until after the open pass
  (`FocusCalendarSoon`). Seeding before engagement means the open contract also holds headless
  (unit tests have no `Window`, so `EngageOverlay` no-ops there; source comment).
- **Overlay options**: `Modal = false`; `CloseOnEscape = false` (Escape is owned by the handlers
  here because revert semantics differ per picker); `CloseOnOutsideClick = true`;
  `TrapFocus = false` (focus is moved onto the calendar explicitly); `RestoreFocus = false`.
- **Close** (`CloseCore`): the session is closed programmatically and focus returns to the
  trigger (`_triggerPart?.Focus()`). The session's `Closed` event syncs `IsOpen` back to `false`
  (covers outside-press dismissal).

## Accessibility

`NaviusDatePickerAutomationPeer` (in `NaviusDatePicker.cs`), mirroring the native WPF
`DatePickerAutomationPeer`'s shape plus the two patterns the M3 gate showed template-only
controls need (source doc comment):

- `AutomationControlType.Custom` with localized control type `"date picker"`, class name
  `NaviusDatePicker`.
- Read-only `ValuePattern`: the formatted date, or empty while unset, never the placeholder
  (`FormatValueText`); `SetValue` throws `InvalidOperationException`. Verified by
  `DatePickerTests.ValuePattern_SurfacesFormattedDate_NeverThePlaceholder`.
- `ExpandCollapsePattern` over `IsOpen` (`Expand` gated on `IsEnabled`). This replaces the web
  trigger's `aria-haspopup="dialog"`/`aria-expanded` pair; the web popup's `role="dialog"` has no
  direct WPF equivalent, and ExpandCollapse plus moved-in focus is the native WPF idiom (the
  WAI-ARIA APG date-picker-dialog tiebreak recorded in `date-range-picker.md`). Verified by
  `DatePickerTests.ExpandCollapsePattern_TracksIsOpen`.

## WPF implementation notes

Tier B (custom lookless control). Shipped 2026-07-09 in the same batch as Calendar and
DateRangePicker; this section was extracted from `date-range-picker.md`'s "WPF implementation
notes" on 2026-07-11 with no behavior change.

- **Not derived from the native `System.Windows.Controls.DatePicker`** (source doc comment): its
  `DatePickerTextBox` editing surface belongs to the DateInput family (a separate family the same
  wave), and its popup does not route through this repo's `OverlayStack` dismissal substrate.
- **Read-only display**: the trigger shows plain formatted text; editable endpoint fields
  (composing `NaviusDateInput` into the picker) are a recorded follow-up once both families are
  merged (carried over from the composite's part-mapping record).
- **Theme**: `Themes/DatePicker.xaml`, which merges `Themes/Calendar.xaml` transitively (the
  ContextMenu-merges-Menu precedent) so one dictionary pulls in the calendar part styles. Merged
  into `Themes/Generic.xaml`; the file header previously claimed the opposite and the M6 audit
  corrected it (full entry in `date-range-picker.md`). Hard rule for the wave: hairline borders,
  no `DropShadowEffect`, one-ink brand; the popup surface is the calendar's own card.
- **Registry**: `date-picker` in `registry/registry.json`, `registry:primitive`,
  `registryDependencies`: `anchored-popup`, `calendar`, `core`; files `NaviusDatePicker.cs`,
  `NaviusDatePickerBase.cs`, `Themes/DatePicker.xaml`.
- **Gallery**: `apps/Navius.Wpf.Gallery/Pages/DatePickerPage.xaml`.
- **Tests**: `tests/Navius.Wpf.Tests/DatePickerTests.cs` (defaults, open keys, Escape, pick
  commit, placeholder/formatted display, ValuePattern, ExpandCollapse, `ApplyTemplate` with the
  theme loaded). Key simulation goes through a real hidden `HwndSource` because `KeyEventArgs`
  requires a non-null `PresentationSource`.

## M6 audit (2026-07-09)

This family shipped and was audited as part of the date-range-picker batch; the audit log lives
in `date-range-picker.md` ("M6 audit"). Entries touching this family, summarized:

- `Themes/DatePicker.xaml`'s header claimed it was not merged into `Generic.xaml`; both the claim
  and its cited precedent were false. Corrected (CONFIRMED, fixed, doc-only).
- The keyboard-coverage overclaim correction applies here identically: only the open-when-closed
  keys, Escape, and the pick-commit path are unit-driven; in-grid native navigation and
  outside-press dismissal are asserted as inherited/code-read only.

## Residuals (2026-07-11 docs pass)

- `apps/Navius.Wpf.Gallery/Pages/DatePickerPage.xaml` still carries the pre-audit resource
  comment "Not merged into Generic.xaml (out of scope here)", which `Themes/Generic.xaml`'s merge
  list contradicts. Comment-only, no functional impact; not fixed here (this change is
  docs-only).
- `SideOffset` defaults to `6` while the web composite's Content documents a default of `4`; no
  rationale is recorded in the source for the difference. Newly recorded here; carrying-forward
  or aligning it is a product decision.
- No dedicated e2e coverage exists for this control (`tests/Navius.Wpf.E2E` does not reference
  it); coverage is unit-level only.
