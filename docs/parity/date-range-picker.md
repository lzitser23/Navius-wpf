# DateRangePicker

**Scope (2026-07-11)**: this document covers the web family's contract and, on the WPF side, only
the composite `NaviusDateRangePicker`. The two sibling controls it composes are independent
public registry items with their own APIs and standalone parity docs: `calendar.md`
(`NaviusCalendar`) and `date-picker.md` (`NaviusDatePicker` plus the shared
`NaviusDatePickerBase`). Their per-control material previously lived in this file's "WPF
implementation notes" and was moved out per the coverage rule in `README.md`; composite-level
facts stay here.

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| `NaviusDateRangePicker` | None of its own (`<CascadingValue>` wrapping `<NaviusPopover>`) | Root. Elective (not part of Base UI). Owns the authoritative `NaviusDateRange` value and controlled/uncontrolled open state (delegated to the wrapped `NaviusPopover`). Cascades a `DateRangePickerContext`. Does not implement its own calendar engine: delegates range-grid selection to the styled layer's calendar (`ZitsCalendar`, outside this repo). |
| `NaviusDateRangePickerControl` | `<div role="group">` | The labelled group wrapping the two endpoint inputs, the separator, and the trigger. Hosts the two hidden `NaviusBubbleInput`s that submit the ISO start/end under `StartName`/`EndName`. |
| `NaviusDateRangePickerInput` | `<NaviusDateInput>` (from the DateInput family) | One endpoint of the range (`Part="start"` or `Part="end"`); reuses the whole segmented-editor brain from `NaviusDateInput`, bound to one side of `Ctx.Value`. |
| `NaviusDateRangePickerSeparator` | `<span aria-hidden="true">` | The visual "to" separator between the two endpoint inputs; hidden from assistive tech. |
| `NaviusDateRangePickerContent` | `<NaviusPopoverPortal><NaviusPopoverPositioner><NaviusPopoverPopup>` | Portals + anchors the popup, reusing the Popover engine's Portal > Positioner > Popup. Hosts the range calendar content. The Popup is `role="dialog"`. |
| `NaviusDateRangePickerTrigger` | `<NaviusPopoverTrigger>` | The calendar button that opens the popup. Delegates `aria-haspopup="dialog"`, `aria-expanded`, `aria-controls`, `data-popup-open` to the Popover trigger. |

Supporting (non-component) type: `DateRangePickerContext` (plain class, cascaded): holds `Value` (`NaviusDateRange`), `MinValue`/`MaxValue`, `Granularity`, `Disabled`/`ReadOnly`/`Required`/`Invalid`, `StartName`/`EndName`, and `SetRangeAsync`/`SetStartAsync`/`SetEndAsync` (the latter two set one endpoint, keeping the other), plus a `Changed` event.

**Scope note**: this family delegates the Popover open/close/positioning/focus mechanics entirely to a separate Popover primitive family (`NaviusPopover`, `NaviusPopoverPortal`, `NaviusPopoverPositioner`, `NaviusPopoverPopup`, `NaviusPopoverTrigger`), which is outside `Components\DateRangePicker` and not documented here. It also delegates the actual range-selection calendar grid to a styled-layer `ZitsCalendar` outside this repo entirely.

## Parameters

### NaviusDateRangePicker

| Name | Type | Default | Notes |
|---|---|---|---|
| `Value` | `NaviusDateRange` | `NaviusDateRange.Empty` | Controlled value. Use `@bind-Value`. |
| `DefaultValue` | `NaviusDateRange` | `NaviusDateRange.Empty` | Uncontrolled initial value. |
| `Open` | `bool` | `false` | Controlled open state. Use `@bind-Open`. |
| `DefaultOpen` | `bool` | `false` | Uncontrolled initial open state. |
| `MinValue` | `DateOnly?` | `null` | Passed through to both endpoint `NaviusDateInput`s. |
| `MaxValue` | `DateOnly?` | `null` | Passed through to both endpoint `NaviusDateInput`s. |
| `Granularity` | `string` | `"day"` | Passed through to both endpoint `NaviusDateInput`s. |
| `Disabled` | `bool` | `false` | |
| `ReadOnly` | `bool` | `false` | |
| `Required` | `bool` | `false` | |
| `Invalid` | `bool` | `false` | |
| `StartName` | `string?` | `null` | Form field name for the start endpoint's hidden bubble input. |
| `EndName` | `string?` | `null` | Form field name for the end endpoint's hidden bubble input. |
| `ChildContent` | `RenderFragment?` | `null` | |

### NaviusDateRangePickerControl

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | |
| `Attributes` | `IDictionary<string, object>?` (`CaptureUnmatchedValues`) | `null` | Passthrough on the root `<div>`. |

### NaviusDateRangePickerInput

| Name | Type | Default | Notes |
|---|---|---|---|
| `Part` | `string` | `"start"` | Which endpoint this input edits: `"start"` or `"end"` (anything other than `"end"` is treated as start). |
| `Attributes` | `IDictionary<string, object>?` (`CaptureUnmatchedValues`) | `null` | Passthrough onto the embedded `NaviusDateInput`. |

### NaviusDateRangePickerSeparator

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | |
| `Attributes` | `IDictionary<string, object>?` (`CaptureUnmatchedValues`) | `null` | |

### NaviusDateRangePickerContent

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | |
| `Side` | `string` | `"bottom"` | Popover positioner side. |
| `Align` | `string` | `"start"` | Popover positioner alignment. |
| `SideOffset` | `double` | `4` | Popover positioner offset. |
| `Attributes` | `IDictionary<string, object>?` (`CaptureUnmatchedValues`) | `null` | Passthrough on the `NaviusPopoverPopup`. |

### NaviusDateRangePickerTrigger

| Name | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | |
| `Attributes` | `IDictionary<string, object>?` (`CaptureUnmatchedValues`) | `null` | Passthrough on the `NaviusPopoverTrigger`. |

## Events

| Part | Event | Signature | Fires when |
|---|---|---|---|
| `NaviusDateRangePicker` | `ValueChanged` | `EventCallback<NaviusDateRange>` | From `ApplyRangeAsync`, invoked whenever `DateRangePickerContext.SetRangeAsync` is called, either by an endpoint input committing one side (`SetStartAsync`/`SetEndAsync`) or by the calendar content replacing the whole range. Fires in both controlled and uncontrolled modes. |
| `NaviusDateRangePicker` | `OpenChanged` | `EventCallback<bool>` | From `RequestOpenAsync`, invoked whenever the wrapped `NaviusPopover`'s `OpenChanged` fires (e.g. trigger click, or however the Popover engine decides to open/close: not shown in this family's code). |

No other part in this family declares an `EventCallback` parameter; the endpoint inputs route changes through `DateRangePickerContext.SetStartAsync`/`SetEndAsync` (plain `Task`-returning methods), not `EventCallback`.

## State + data attributes

`NaviusDateRangePickerControl` (`role="group"`):

| Attribute | Condition |
|---|---|
| `data-navius-date-range-picker` | always |
| `data-disabled` | `Ctx.Disabled` |
| `data-readonly` | `Ctx.ReadOnly` |
| `data-invalid` | `Ctx.Invalid` |
| `data-required` | `Ctx.Required` |
| `aria-invalid` | `"true"` when `Ctx.Invalid` |

`NaviusDateRangePickerInput` (on the embedded `NaviusDateInput`'s root `<div>`, in addition to that component's own `data-navius-date-input`/`data-disabled`/etc., see `date-input.md`):

| Attribute | Condition |
|---|---|
| `data-navius-date-range-picker-input` | always |
| `data-part` | `"start"` or `"end"` |

`NaviusDateRangePickerSeparator`: `data-navius-date-range-picker-separator`, `aria-hidden="true"`.

`NaviusDateRangePickerContent`: `data-navius-date-range-picker-popup` on the `NaviusPopoverPopup`.

`NaviusDateRangePickerTrigger`: `data-navius-date-range-picker-trigger`; `disabled=""` attribute when `Ctx.Disabled` (in addition to whatever the `NaviusPopoverTrigger` itself renders for `aria-haspopup`/`aria-expanded`/etc.).

Internal (non-DOM) state: `DateRangePickerContext.Value` (`NaviusDateRange`, the authoritative source synced every `OnParametersSet` via `SyncContext`), `_internalValue`/`_internalOpen` (uncontrolled tracking), `_valueSet`/`_openSet` (controlled-detection). `NaviusDateRange` itself (`Common/NaviusDateRange.cs`) exposes `IsComplete`, `IsEmpty`, `Ordered()` (swap endpoints if `End < Start`), and `StartIso`/`EndIso`.

## Keyboard

Not implemented directly in this family's own `.razor`/`.cs` files beyond what it inherits by composition:

- The two endpoint inputs are full `NaviusDateInput` instances, so they get that component's complete segment keyboard model (arrow step/wrap, Home/End, digit typing, Backspace/Delete, ArrowLeft/Right segment navigation), see `date-input.md` for the exact key table. No additional or overridden key handling is added by `NaviusDateRangePickerInput`.
- Opening/closing the popup and any keyboard interaction with the trigger (e.g. Enter/Space to open, Escape to close) is delegated entirely to `NaviusPopoverTrigger`/`NaviusPopover`, which live outside `Components\DateRangePicker` and are not read as part of this extraction.
- The calendar grid inside the popup (day-cell navigation, range-selection keys) is delegated to a styled-layer `ZitsCalendar`, outside this repo entirely: no keyboard logic for it exists in `Navius.Primitives`.
- `tests/e2e/specs/dates.spec.ts` exercises the picker only via mouse clicks (trigger click, day-cell clicks); it does not exercise or confirm any keyboard path for opening the popup or selecting a range, so no e2e-confirmed keyboard behavior can be documented for those parts.

## Accessibility

- `NaviusDateRangePickerControl`: `role="group"`, `aria-invalid="true"` when invalid (no explicit `aria-required`: `data-required` is a data attribute only, not mirrored to an ARIA attribute in this code).
- `NaviusDateRangePickerInput`: inherits the embedded `NaviusDateInput`'s ARIA (`role="group"` wrapper, per-segment `role="spinbutton"`, see `date-input.md`).
- `NaviusDateRangePickerSeparator`: `aria-hidden="true"`.
- `NaviusDateRangePickerContent`: the `NaviusPopoverPopup` is `role="dialog"` (per this file's own doc comment); actual attribute wiring lives in the Popover family, not read here.
- `NaviusDateRangePickerTrigger`: delegates `aria-haspopup="dialog"`, `aria-expanded`, `aria-controls` to `NaviusPopoverTrigger` (confirmed by `tests/e2e/specs/dates.spec.ts`, which asserts `aria-haspopup="dialog"` on `[data-navius-date-range-picker-trigger]`).
- No explicit `FocusAsync`/focus-trap code exists in this family's own files; any focus management on popup open/close (e.g. moving focus into the calendar, returning focus to the trigger on close) is owned by the Popover family and not visible here.

## WPF strategy

Tier B (custom lookless control).

Compose two Tier-B `DateInput` controls (see `date-input.md`) plus a `System.Windows.Controls.Primitives.Popup` hosting a calendar, similar in shape to how `System.Windows.Controls.DatePicker` composes a `DatePickerTextBox` + `Calendar` inside a `Popup`: that's the closest native analogue, though it is not a range picker so cannot be derived from directly. `role="group"` → `AutomationControlType.Group`; the trigger's `aria-haspopup="dialog"`/`aria-expanded` → a custom `AutomationPeer` implementing `IExpandCollapseProvider` (mirroring how WPF's own `ToggleButton`/`Expander` peers work) rather than a 1:1 ARIA attribute copy. What will not translate cleanly: the DOM-portal-based popup composition (`NaviusPopoverPortal` > `Positioner` > `Popup`, with `Side`/`Align`/`SideOffset`) has no DOM-portal equivalent needed in WPF: `Popup.Placement`/`PlacementTarget`/`HorizontalOffset`/`VerticalOffset` replace it directly, so this actually simplifies; conversely, the range-selection calendar grid itself (`ZitsCalendar`, styled-layer, out of this repo) has **no source in `Navius.Primitives` to port at all**, the WPF calendar surface will need to be designed from scratch or reverse-engineered from the styled-layer repo, referencing only the `data-range-start`/`data-range-end`/`data-range-middle` contract implied by `tests/e2e/specs/dates.spec.ts`.

## Open questions

- `DateRangePickerContext.SetStartAsync`/`SetEndAsync` do not call `NaviusDateRange.Ordered()`, so if a user types a start date after an end date via the two segmented inputs (not the calendar), the resulting range is not automatically normalized to `Start <= End`. Confirm whether the WPF port should preserve this (typed-order range can be "backwards") or add normalization.
- The full contract of the Popover family (`NaviusPopover`, `NaviusPopoverTrigger`, `NaviusPopoverPortal`, `NaviusPopoverPositioner`, `NaviusPopoverPopup`) needs to be extracted separately before this family's popup/keyboard/focus behavior can be fully specified for WPF parity: it was out of scope for this document.
- The calendar content rendered inside `NaviusDateRangePickerContent`'s `ChildContent` (`ZitsCalendar` per the source comment) is not part of `Navius.Primitives` at all; the range-selection grid's keyboard model, ARIA, and multi-month layout ("two-month range calendar" per the root's doc comment) have no code in this repo to extract and must be sourced from the styled-layer repo.
- No validation logic is shown for cross-endpoint constraints (e.g. can the end date be typed before the start date is set, does `MinValue`/`MaxValue` apply differently per endpoint) beyond what `NaviusDateInput` already does per-endpoint independently.

## WPF implementation notes

Shipped as three families in one batch: Calendar and DatePicker are documented standalone in `calendar.md` and `date-picker.md` (moved there 2026-07-11); this section keeps only the composite: `Controls/DateRangePicker/NaviusDateRangePicker` (+ `NaviusDateRange` + `DateRangeCommitEngine`), which derives the shared `NaviusDatePickerBase` (owned by `date-picker.md`) and templates a `NaviusCalendar` (owned by `calendar.md`). Theme: `Themes/DateRangePicker.xaml` (merges Calendar.xaml transitively, the ContextMenu-merges-Menu precedent), merged into `Themes/Generic.xaml` (M6 audit 2026-07-09: corrected -- this previously claimed the opposite, which was false; see `Themes/Generic.xaml`'s own merge list).

### Part mapping

| Web part | WPF |
|---|---|
| `NaviusDateRangePicker` (root CascadingValue over Popover) | `NaviusDateRangePicker : NaviusDatePickerBase : Control`, one templated control |
| `NaviusDateRangePickerControl` (`role="group"` + hidden bubble inputs) | Dropped: no native-form mirror in WPF (repo precedent, same as Select dropping NaviusBubbleInput); `StartName`/`EndName`/`Required`/`Invalid` not ported |
| `NaviusDateRangePickerInput` (two segmented `NaviusDateInput` endpoints) | NOT ported here: the segment-field brain is the DateInput family, owned by a concurrent agent this wave. The trigger shows a plain read-only display ("start - end" formatted per `CultureInfo.CurrentCulture`); composing editable endpoint fields into the picker is a follow-up once both families are merged |
| `NaviusDateRangePickerSeparator` | The literal " - " inside the display string (aria-hidden has no equivalent need; the string is one UIA value) |
| `NaviusDateRangePickerContent` (Popover Portal > Positioner > Popup, `role="dialog"`) | `NaviusAnchoredPopup` (PART_Popup) hosting PART_PopupContent; `Side`/`Align`/`SideOffset`/`AlignOffset` are flat properties on the picker root, declared on the shared base (Positioner-collapse precedent from Popover; property table in `date-picker.md`). Align defaults to Start per this contract's Content default |
| `NaviusDateRangePickerTrigger` (`aria-haspopup`/`aria-expanded`) | PART_Trigger ToggleButton (shared base, see `date-picker.md`); `IExpandCollapseProvider` on the picker's peer replaces the ARIA pair, as this doc's "WPF strategy" section predicted |
| `ZitsCalendar` (styled-layer, no source in this repo) | `NaviusCalendar`, Tier A: derives the native `System.Windows.Controls.Calendar`, inheriting its keyboard model and `CalendarAutomationPeer` for free, re-templated to tokens. Standalone family: the part styles, keyed-style rationale, and keyboard verification status are in `calendar.md` |

### Commit model

`DateRangeCommitEngine` (pure, STA-free tests): first pick sets Start; second pick sets End, swapped so `Start <= End`; a pick after a complete range starts fresh. Picks arrive through the shared base's detection (left mouse-up on a `CalendarDayButton`, or Enter/Space bubbling out of the calendar, both with `handledEventsToo`; mechanics and the mouse-capture-release quirk in `date-picker.md`). `Calendar.SelectedDatesChanged` is deliberately NOT a commit source: the native calendar moves selection on every arrow key, and the contract wants arrows to navigate without committing. The calendar runs in `CalendarSelectionMode.SingleRange` purely so start/end/middle days all render selected (the `data-range-start`/`data-range-end`/`data-range-middle` styling implied by the web e2e); native Shift-to-extend semantics never drive the committed value.

Escape reverts BOTH endpoints to their open-time snapshot, then closes (the contract's "Esc reverts both"); an outside press keeps whatever was committed (each pick updates `Value` and fires `ValueChanged` immediately, matching the web firing per endpoint set) and just dismisses. The typed-backwards-range open question above resolves trivially here: with no endpoint inputs, every range flows through the engine and is always ordered.

### Recorded deltas

- The two endpoint segment inputs, hidden bubble inputs, `role="group"` control wrapper, and `Granularity`/`MinValue`/`MaxValue` passthroughs are not ported (DateInput family split + no-native-form precedent). `MinValue`/`MaxValue` can later map to the calendar part's `DisplayDateStart`/`DisplayDateEnd`.
- UIA: `NaviusDateRangePickerAutomationPeer` reports `AutomationControlType.Custom` with localized control type "date range picker" (the native `DatePickerAutomationPeer` shape, same peer shape as `NaviusDatePicker`'s; the M3-gate rationale and the `role="dialog"` to ExpandCollapse APG tiebreak are recorded in `date-picker.md`, "Accessibility") plus read-only `IValueProvider` ("start - end" formatted, "start - " while the second pick is pending, empty while unset, never the placeholder) and `IExpandCollapseProvider` over `IsOpen`.
- Keyboard (the web contract had NO e2e-confirmed keyboard path for popup or grid, so WAI-ARIA APG plus native WPF Calendar won every tiebreak): the open/close/pick key handling is the shared base's, documented key-by-key with handler traceability in `date-picker.md`; the composite differences are Escape (reverts BOTH endpoints before closing, `CancelAndClose` override) and pick commit (routes through `DateRangeCommitEngine`, closing only when the range completes). Only the open-when-closed keys (Enter/Space/ArrowDown), Escape-reverts-and-closes, and the pick-commit engine are actually unit-tested here; the in-grid native Calendar navigation (arrows/PageUp/PageDown/Home/End, see `calendar.md` for the wording caveat) and outside-press dismissal are asserted only as "inherited from native Calendar behavior," not independently driven by a test (see M6 audit below -- the prior wording here, "all confirmed by unit tests," overstated this).
- Mid-interaction range display: after a pick the committed range is repainted onto `SelectedDates`, but arrowing then collapses the native highlight to the focused day until the next pick (state is never lost, display only). The web's two-month layout is single-month here (native Calendar shows one month per view).
- `NaviusDateRange` is a `readonly record struct` over `DateTime?` (not `DateOnly`) to match the native Calendar's value type; `Ordered()` from the web contract is unnecessary because the engine orders on commit.

## M6 audit (2026-07-09)

**CONFIRMED, fixed.** `Controls/Calendar/NaviusCalendar.cs`'s own doc comment directly
contradicted this doc and the actual shipped code: it claimed the range picker "layers its own
two-pick commit state machine on top of single-date commits rather than switching to
`CalendarSelectionMode.SingleRange`," while `NaviusDateRangePicker.OnOpened()` (`NaviusDateRangePicker.cs:65`)
explicitly does switch `CalendarPart.SelectionMode = CalendarSelectionMode.SingleRange`, exactly as
this doc's own "Commit model" section (above) correctly describes. Fixed the stale comment in
`NaviusCalendar.cs` to match the doc and the real behavior.

**CONFIRMED, fixed (doc-only).** `Themes/Calendar.xaml`, `Themes/DatePicker.xaml`, and
`Themes/DateRangePicker.xaml` all claimed (both in this doc and in each file's own header comment)
that they were "Not merged into Generic.xaml... same precedent as Themes/Select.xaml." Both halves
of that claim were false: `Themes/Generic.xaml` merges all three directly, and also merges
`Themes/Select.xaml` (so the cited "precedent" was itself wrong). Corrected this doc's "WPF
implementation notes" intro and the three theme files' header comments (not `Generic.xaml` itself,
which is out of scope for this audit's edits either way). No functional impact -- consumers were
already getting these styles for free via `Generic.xaml`; only the comments were wrong.
(2026-07-11 docs-pass addendum: the same stale "Not merged into Generic.xaml" wording also
survives in the three Gallery pages' resource comments, including
`apps/Navius.Wpf.Gallery/Pages/DateRangePickerPage.xaml`; comment-only, not fixed in the
docs-only split that created `calendar.md`/`date-picker.md`.)

**CONFIRMED, fixed.** The doc's "Keyboard" delta bullet claimed "all confirmed by unit tests," but
no test in `DateRangePickerTests.cs`/`DatePickerTests.cs`/`CalendarTests.cs` drives PageUp/PageDown/
Home/End/arrow-day navigation or an outside-press dismissal; only the open-when-closed keys,
Escape, and the commit engine are actually exercised. Softened the claim above rather than adding
exhaustive coverage of native `Calendar`'s own (pre-existing, Microsoft-owned) keyboard model,
which is out of proportion for this audit; the mis-claim itself is what's fixed.

**CONFIRMED, fixed (test coverage gap).** Every existing `StaFact` that opens the popup and commits
a pick (`FirstPick_SetsStart_AndKeepsThePopupOpen`, `SecondPick_CompletesTheRange_AndCloses`,
`Escape_RevertsBothEndpoints_AndCloses`) constructs a bare `NaviusDateRangePicker` without ever
calling `ApplyTemplate()`, so `CalendarPart` stays null and `OnOpened()`/`SyncCalendarSelection()`
return early at their null-guards -- the `SelectionMode = SingleRange` assignment and the
`SelectedDates`/`SelectedDate` repaint were never actually exercised against a real native
`Calendar`. Added `OnOpened_WithRealCalendarPart_SwitchesToSingleRangeAndSyncsSelectionWithoutThrowing`
(`DateRangePickerTests.cs`), which applies the real template first, then asserts (via reflection,
since `CalendarPart` is protected) that the native Calendar really does switch to `SingleRange` and
accepts both the partial-pick `SelectedDate` write and the complete-range `SelectedDates.AddRange`
write without throwing. This also resolves the one PLAUSIBLE item flagged during investigation
(whether `Calendar.SelectedDate`'s setter is safe under `SelectionMode=SingleRange`): confirmed
safe, no exception.
