# DateInput

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| `NaviusDateInput` | `<div role="group">` | Root of the segmented date editor. Elective (not part of Base UI); the react-aria `DateField` analogue. Owns a `DateOnly?` value; builds a culture-aware segment layout (MM/DD/YYYY order + separators) and cascades a `SegmentFieldContext`. Renders the segment layout, `ChildContent`, and a hidden `NaviusBubbleInput` for form submission. |
| `NaviusFieldSegment` *(shared, `Navius.Primitives.Common`)* | `<span role="spinbutton">` | One editable segment (year/month/day). Not a hand-placed public part: the root iterates its culture-derived layout and emits one per unit. Owns its `ElementReference` for root-driven DOM focus. All value/keyboard logic lives in the root; this part only presents `aria-value*`/labels and `data-*` hooks. Shared with `NaviusTimeInput`. |
| `NaviusFieldSegmentLiteral` *(shared, `Navius.Primitives.Common`)* | `<span aria-hidden="true">` | Non-interactive separator between two segments ("/", ".", ":"). Never focusable. Shared with `NaviusTimeInput`. |
| `NaviusBubbleInput` *(shared, `Navius.Primitives.Common`)* | `<input type="hidden">` (visually-hidden) | Native form mirror. Submits the composed value as ISO `yyyy-MM-dd` under `Name` so the field participates in real form submission. |

## Parameters

### NaviusDateInput

| Name | Type | Default | Notes |
|---|---|---|---|
| `Value` | `DateOnly?` | `null` | Controlled value. Use `@bind-Value`. |
| `DefaultValue` | `DateOnly?` | `null` | Uncontrolled initial value. |
| `Granularity` | `string` | `"day"` | Which units the field edits: `"day"` (y/m/d), `"month"` (y/m), or `"year"`. |
| `MinValue` | `DateOnly?` | `null` | Drives `IsOutOfRange` / `data-invalid`; does not clamp. |
| `MaxValue` | `DateOnly?` | `null` | Drives `IsOutOfRange` / `data-invalid`; does not clamp. |
| `PlaceholderValue` | `DateOnly?` | `null` (→ today) | Seeds which value an empty segment jumps to on the first arrow key. |
| `Disabled` | `bool` | `false` | |
| `ReadOnly` | `bool` | `false` | |
| `Required` | `bool` | `false` | Drives `Field` validity (`ValueMissing`) when composing is incomplete. |
| `Invalid` | `bool` | `false` | Force-invalid regardless of native validity (mirrors Base UI `Field`). |
| `ForceLeadingZeros` | `bool` | `false` | Pad month/day (and year to 4 digits) with leading zeros (react-aria `shouldForceLeadingZeros`). |
| `Culture` | `CultureInfo?` | `null` (→ `CultureInfo.CurrentCulture`) | Drives segment order + separators via `ShortDatePattern`. |
| `Dir` | `string?` | `null` (→ cascaded `NaviusDirection` then `"ltr"`) | Reading direction; flips ArrowLeft/ArrowRight focus travel. |
| `Name` | `string?` | `null` | Form field name for the hidden bubble input. |
| `ChildContent` | `RenderFragment?` | `null` | Optional trailing content (e.g. a decorative calendar glyph). |
| `Attributes` | `IDictionary<string, object>?` (`CaptureUnmatchedValues`) | `null` | Passthrough attributes on the root `<div>`. |

### NaviusFieldSegment

| Name | Type | Default | Notes |
|---|---|---|---|
| `Segment` | `DateTimeSegment` | `default!` | The segment model this part renders (from the root's layout). |
| `Focus` | `bool` | `false` | One-shot: true for exactly one render after a keyboard-driven focus move; triggers `ElementReference.FocusAsync()` in `OnAfterRenderAsync`. |

### NaviusFieldSegmentLiteral

| Name | Type | Default | Notes |
|---|---|---|---|
| `Text` | `string` | `""` | The separator characters to render. |

### NaviusBubbleInput

| Name | Type | Default | Notes |
|---|---|---|---|
| `InputType` | `string` | `"checkbox"` (DateInput passes `"hidden"`) | Native input type. |
| `Name` | `string?` | `null` | Form field name submitted with the owning form. |
| `Value` | `string` | `"on"` (DateInput passes the composed ISO string) | Submitted value. |
| `Checked` | `bool?` | `null` | Not used by DateInput (checkbox/radio only); null = indeterminate (attribute omitted). |
| `Required` | `bool` | `false` | Mirrors HTML `required`. |
| `Disabled` | `bool` | `false` | Mirrors HTML `disabled`. |
| `Form` | `string?` | `null` | Optional id of an associated `<form>`. |
| `Attributes` | `IDictionary<string, object>?` (`CaptureUnmatchedValues`) | `null` | Passthrough attributes. |

## Events

| Part | Event | Signature | Fires when |
|---|---|---|---|
| `NaviusDateInput` | `ValueChanged` | `EventCallback<DateOnly?>` | From `CommitAsync()`: after a keystroke changes a segment and the newly-composed `DateOnly?` differs from the last-emitted value (`_lastComposed`). Fires on every committing keystroke (arrow/page/digit/Home/End/Backspace), not only when the value becomes fully composed. |

`NaviusFieldSegment`/`NaviusFieldSegmentLiteral`/`NaviusBubbleInput` declare no `EventCallback` parameters; their `@onkeydown`/native events route back through `SegmentFieldContext.HandleKeyAsync` (wired via `KeyImpl`), not through Blazor `EventCallback` parameters.

## State + data attributes

Root `<div>` (`NaviusDateInput`):

| Attribute | Condition |
|---|---|
| `data-navius-date-input` | always |
| `data-disabled` | `Disabled` |
| `data-readonly` | `ReadOnly` |
| `data-invalid` | `IsInvalid` (`Field?.IsInvalid ?? (Invalid \|\| IsOutOfRange)`) |
| `data-filled` | `AnyFilled` (any focusable segment has a value) |
| `data-dirty` | internal `_dirty` (true once any commit has happened) |
| `data-focused` | internal `_focused` |
| `data-touched` | internal `_touched` (true after focus-out) |
| `aria-describedby` | `Field?.DescribedBy` |
| `aria-invalid` | `"true"` when `IsInvalid` |
| `role` | `"group"` |

Per-segment (`NaviusFieldSegment`):

| Attribute | Condition |
|---|---|
| `data-segment` | segment kind: `year`/`month`/`day` |
| `data-navius-date-input-segment` | always (via `SegmentFieldContext.PartName` = `"date-input"`) |
| `data-placeholder` | segment is unfilled |
| `data-disabled` | `Ctx.Disabled` |
| `data-readonly` | `Ctx.ReadOnly` |

Literal (`NaviusFieldSegmentLiteral`): `data-segment="literal"`, `data-navius-date-input-literal`.

Internal (non-DOM) state: `_internal`/`_lastComposed` (uncontrolled value tracking, round-trip guard), `_valueSet` (controlled-detection), `_focused`/`_touched`/`_dirty`, `_builtGranularity`/`_builtCulture` (layout-rebuild guard), and per-segment `DateTimeSegment.Value`/`TypeBuffer`/`Filled`.

## Keyboard

Resolved per-segment by `SegmentMath.HandleKey` (in `Navius.Primitives.Common`), invoked from `NaviusDateInput.HandleKeyAsync` via `NaviusFieldSegment`'s `@onkeydown`. No-op (`Disabled`/`ReadOnly` short-circuit `OnKeyDownAsync` before it reaches the root).

For year/month/day segments:

| Key | Behavior |
|---|---|
| `ArrowUp` | Step +`ArrowStep` (1), wrapping at `Min`/`Max`. Empty segment lands on the placeholder basis (today, or `PlaceholderValue`) rather than basis ± step. |
| `ArrowDown` | Step -`ArrowStep`, same wrap/placeholder rule. |
| `PageUp` | Step +`PageStep` (year 5, month 3, day 7). |
| `PageDown` | Step -`PageStep`. |
| `Home` | Set to `Min`, clear type buffer. |
| `End` | Set to `Max`, clear type buffer. |
| `Backspace` / `Delete` | Clear the segment to unfilled (`Value = null`). |
| `ArrowLeft` | Move focus to the previous segment (flipped to next when `Dir="rtl"`). |
| `ArrowRight` | Move focus to the next segment (flipped to previous when `Dir="rtl"`). |
| digit `0`-`9` | Append to the type buffer; auto-advances focus to the next segment (ignoring RTL, `ignoreDir: true`) when the candidate value exceeds what one more digit could stay within bounds for (`candidate * 10 > Max`), or the buffer reaches `MaxDigits` (4 for year, 2 for month/day). A digit that would exceed `Max` restarts the buffer at that single digit. |
| other keys | No-op (`SegmentKeyResult.None`). |

The day segment's `Max` is recomputed from the current year/month (`DateTime.DaysInMonth`) after every value-changing key, and the day value is clamped down if it now exceeds the new max.

Focus moves via `MoveFocus`, which sets a one-shot `FocusIndex`/`FocusRequested` on `SegmentFieldContext`, consumed by the target `NaviusFieldSegment.OnAfterRenderAsync` calling `ElementReference.FocusAsync()`; the root clears `FocusRequested` in its own `OnAfterRender` (after the segment has already captured it as a parameter for that render batch).

## Accessibility

- Root: `role="group"`, `aria-describedby` (from a `NaviusField` ancestor's `DescribedBy`), `aria-invalid="true"` when invalid.
- Each segment: `role="spinbutton"`, `tabindex="0"` (omitted/`null` when `Disabled`), `aria-label` ("year"/"month"/"day"), `aria-valuenow` (raw number, or `null` when unfilled), `aria-valuemin`/`aria-valuemax`, `aria-valuetext` (`"Empty"` when unfilled, else the value: month renders as the localized month name), `aria-disabled="true"` when disabled, `aria-readonly="true"` when read-only.
- Literal separators: `aria-hidden="true"`, never focusable.
- Focus management: `NaviusDateInput` registers `Field.FocusControl = FocusFirstAsync` with an ancestor `NaviusField`, so an external "focus this field" request (e.g. clicking the field's `<label>`) focuses segment 0. Segment-to-segment focus travel on keyboard is a one-shot flag consumed via `ElementReference.FocusAsync()` (no JS interop).
- `NaviusDateInput` reports its composite state (`Value`=ISO string, `Filled`, `Dirty`, `Focused`, `Touched`, `Valid`, `ValueMissing`) to the ancestor `NaviusField` via `Field.ApplyControlStateAsync` on every commit and focus in/out, so the field's label/description/error can reflect it.

## WPF strategy

Tier B (custom lookless control).

There is no native WPF control with per-segment spinbutton semantics for a date (`DatePicker` edits as a single masked text run, not independently focusable/steppable segments), so this should be a lookless `Control` (or `Control`-derived custom control) with a `ControlTemplate` composed of per-segment `FrameworkElement` "spinbutton" parts and literal separators, driven by the same `DateTimeSegment`/`SegmentMath` state machine ported to C# (it is already framework-agnostic pure C#, so it should port near-verbatim). Each segment's `role="spinbutton"` + `aria-valuenow`/`min`/`max`/`valuetext` maps to a custom `AutomationPeer` implementing `IRangeValueProvider` (`AutomationControlType.Spinner`) exposing `Minimum`/`Maximum`/`Value` and `SetValue`; the root's `role="group"` maps to `AutomationControlType.Group`. What will not translate cleanly: the DOM-order-based one-shot `ElementReference.FocusAsync()` hand-off between segments (port to `Keyboard.Focus`/`FocusManager.SetFocusedElement` on the segment's `FrameworkElement`), the culture-driven `ShortDatePattern` parsing for segment order/separators (`.NET`'s `DateTimeFormatInfo.ShortDatePattern` is available identically in WPF, so this actually ports cleanly), and RTL handling (`Dir`/`CascadedDir` → WPF `FlowDirection`, though the explicit ArrowLeft/Right flip logic in `MoveFocus` will need to be reproduced manually since WPF does not auto-flip arrow-key semantics for custom controls).

## Open questions

- `IsOutOfRange`/`MinValue`/`MaxValue` only ever surface as `data-invalid`: the composed value is never clamped or rejected. Confirm the WPF port should keep "never clamp, always reflect what's visibly typed" rather than snapping to range.
- `Compose()` silently clamps an out-of-range day (e.g. Feb 30 typed digit-by-digit) down to the month's max via `Math.Min` inside `Compose`, while `RecomputeDayMax` also clamps the segment's own `Value` after every keystroke: worth confirming there's no double-clamp edge case (e.g. typing day "31" then changing month to February) that produces surprising WPF behavior if ported literally.
- The hidden hand-off between `NaviusDateInput` and `NaviusField` (`Field.FocusControl`, `Field.ApplyControlStateAsync`, `Field.DescribedBy`) depends on a `NaviusField` component not in this family's folder: its full contract needs to be pulled in before the WPF `Field`-equivalent parity can be finalized.
- No explicit behavior is shown for what happens when `Granularity` changes at runtime while a value is mid-edit beyond "rebuild layout, reseed from `CurrentValue`" (`RebuildIfNeeded`), confirm this is the desired WPF behavior (loses in-progress unconfirmed typing).

## WPF implementation notes

Implemented at `src/Navius.Wpf.Primitives/Controls/DateInput/NaviusDateInput.cs`, sharing
`src/Navius.Wpf.Primitives/Controls/DateInput/NaviusFieldSegment.cs` and the pure engine at
`src/Navius.Wpf.Primitives/Controls/Internal/SegmentEngine.cs` (`SegmentUnit`, `DateTimeSegment`,
`SegmentKey`, `SegmentMath`, `SegmentLayoutBuilder`, `DateSegmentComposer`, `SegmentFormat`) with
NaviusTimeInput/NaviusTimePicker. Theme: `Themes/DateInput.xaml` (not merged into Generic.xaml;
pages/consumers merge it directly, same precedent as Select/Menu/Popover). Tests:
`tests/Navius.Wpf.Tests/DateInputTests.cs`. Gallery: `Pages/DateInputPage.xaml(.cs)`.

Resolved open questions:

- **Never-clamp range semantics kept.** `MinValue`/`MaxValue` only drive `IsOutOfRange`/
  `IsInvalidState`; the composed value is never snapped, matching the web contract exactly.
- **Compose()'s day-clamp is the single source of truth.** `DateSegmentComposer.Compose` clamps an
  out-of-range day down via `Math.Min` against `DateTime.DaysInMonth`; the root additionally calls
  `DateSegmentComposer.RecomputeDayMax` after every value-changing key so the Day segment's own
  `Max`/displayed value stay in sync with the current Year/Month before the next keystroke, exactly
  mirroring the web's `RecomputeDayMax` + `Compose` pairing. No double-clamp surprises were found:
  typing day "31" then changing month to February clamps the day segment to 28/29 immediately (via
  `RecomputeDayMax`), and `Compose` is a no-op re-clamp of the same already-clamped value.
  Verified by `RecomputeDayMax_ClampsDayValueDown_WhenMonthShrinksMax` and
  `MonthChange_RecomputesDayMax_AndClampsDay`.
- **`Granularity` runtime changes rebuild and reseed, discarding in-progress typing** (matches the
  web's `RebuildIfNeeded`): `OnLayoutAffectingChanged` calls `RebuildLayout()`, which rebuilds the
  segment/cell lists from scratch and reseeds from the current `Value`.
- **No ambient `NaviusField`.** That family isn't ported in this repo yet. `Field.FocusControl` and
  `Field.ApplyControlStateAsync` have no analogue here; `FocusFirstSegment()` is exposed directly on
  the control instead, and `IsFilled`/`IsOutOfRange`/`IsInvalidState` are public read-only
  dependency properties a future Field port can read directly (no event needed, since WPF property
  system already notifies bound consumers).

Contract deltas (WPF-native substitutions, not gaps):

- **No `NaviusBubbleInput`.** This repo already established (in `NaviusSelectBase`) that Tier B
  controls drop the hidden native-form mirror; `Name` stays a marker-only property.
- **`Dir` is WPF's native `FlowDirection`**, not a custom string parameter; `ArrowLeft`/`ArrowRight`
  focus-flip honors it exactly as the contract's `MoveFocus` does for `Dir="rtl"`.
- **Placeholder rendering.** An unfilled segment shows a unit-shorthand token ("yyyy"/"mm"/"dd" via
  `SegmentFormat.PlaceholderToken`) instead of the literal aria-valuetext string `"Empty"` — WPF's
  own native masked-input placeholder idiom. The `NaviusFieldSegmentAutomationPeer`'s
  `IRangeValueProvider.Value` still reports `NaN` for an unfilled segment (screen readers get a
  distinct empty/NaN signal independent of the visible placeholder token).
- **Month/day-name `aria-valuetext` not ported.** UIA's `IRangeValueProvider` only exposes a numeric
  `Value` (no free-text valuetext property analogous to ARIA's `aria-valuetext`), so a screen reader
  hears the month as a number (e.g. "8"), not "August". Reproducing the localized name would require
  a second pattern (e.g. `LegacyIAccessible.Value`) layered on top; left as a follow-up rather than
  implemented speculatively, since no test/consumer currently depends on it.
- **AutomationPeer additions beyond the contract.** `NaviusDateInputAutomationPeer` adds a read-only
  `IValueProvider` surfacing the composed ISO date (`yyyy-MM-dd`) on the root, matching the
  `NaviusSelectAutomationPeer` precedent and the sibling `NaviusTimePicker`'s explicit requirement
  ("template-only text otherwise exposes nothing over UIA").

Segment-engine coverage: 34 `[Fact]` tests in `DateInputTests.cs` cover `SegmentMath` (wrap/clamp,
Arrow/PageUp/PageDown/Home/End/Backspace/Delete, RTL-flipped ArrowLeft/Right, digit accumulation
with early-advance and max-digit-advance, digit-exceeds-max buffer restart, day-period letter/arrow
handling), `SegmentLayoutBuilder` (culture-driven ordering/separators, granularity filtering,
hour-cycle sniffing, per-unit bounds), `DateSegmentComposer` (null-when-incomplete, day clamping,
granularity defaults, day-max recompute), and `SegmentFormat` (placeholder tokens, leading-zero
padding). 17 `[StaFact]` tests cover the control (template building, seeding, typed-digit
composition end to end, placeholder-basis Arrow reveal, focus travel, Backspace clearing,
ReadOnly lockout, month-change day-max clamping, out-of-range invalid state) and both automation
peers.
