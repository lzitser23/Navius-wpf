# TimeInput

Elective component (not part of Base UI). Single-file family: only `NaviusTimeInput`. It reuses the shared segment machinery in `Navius.Primitives.Common` (`DateTimeSegment`, `SegmentMath`, `SegmentFieldContext`, `NaviusFieldSegment`, `NaviusFieldSegmentLiteral`) also used by `NaviusDateInput`; that shared machinery is documented here as it applies to TimeInput, since it is inseparable from this component's behavior.

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusTimeInput | `<div role="group" data-navius-time-input>` (+ hidden `<NaviusBubbleInput type="hidden">`) | Root: owns a `TimeOnly?` value, builds the hour/minute/second/day-period segment layout, cascades `SegmentFieldContext`, integrates with an ambient `FieldContext` (from `Navius.Primitives.Components.Field`) |
| (shared) NaviusFieldSegment | `<span role="spinbutton">` | One editable segment (hour/minute/second/day-period), APG spinbutton pattern; iterated by the root from its computed layout, not hand-placed |
| (shared) NaviusFieldSegmentLiteral | separator text (e.g. `:`, ` `) | Non-editable layout glue between segments |

## Parameters

### NaviusTimeInput

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | `TimeOnly?` | null | Controlled (`@bind-Value`) |
| ValueChanged | `EventCallback<TimeOnly?>` | | |
| DefaultValue | `TimeOnly?` | null | Uncontrolled initial value |
| Granularity | `string` | "minute" | `"hour"`, `"minute"`, or `"second"` |
| HourCycle | `int?` | null | `12` or `24`; defaults to the culture's short-time pattern (`H` present => 24) |
| MinuteStep | `int` | 1 | Arrow step for the minute segment |
| SecondStep | `int` | 1 | Arrow step for the second segment |
| MinValue | `TimeOnly?` | null | No clamping; out-of-range surfaces via `data-invalid`, value is not silently snapped |
| MaxValue | `TimeOnly?` | null | Same as above |
| PlaceholderValue | `TimeOnly?` | null | Seeds which value an empty segment jumps to on first arrow press; defaults to `DateTime.Now` |
| Disabled | `bool` | false | |
| ReadOnly | `bool` | false | |
| Required | `bool` | false | |
| Invalid | `bool` | false | Combined with `Field?.IsInvalid` and out-of-range check for effective invalid state |
| ForceLeadingZeros | `bool` | false | |
| Culture | `CultureInfo?` | null | Defaults to `CultureInfo.CurrentCulture` |
| Dir | `string?` | null | Falls back to cascaded `NaviusDirection`, then `"ltr"` |
| Name | `string?` | null | Form field name for the hidden bubble input, submits ISO `HH:mm[:ss]` |
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

## Events

| Part | Event | Signature |
|---|---|---|
| NaviusTimeInput | ValueChanged | `EventCallback<TimeOnly?>`, fired when the composed value changes on commit (only when all segments are filled and differs from the last composed value) |

## State + data attributes

| Element | Attribute | Meaning |
|---|---|---|
| Root | `role="group"`, `id` (from ambient `FieldContext.ControlId` or a generated id), `aria-describedby` (from `Field?.DescribedBy`), `aria-invalid` | |
| Root | `data-navius-time-input`, `data-disabled`, `data-readonly`, `data-invalid`, `data-filled` (any segment filled), `data-dirty`, `data-focused`, `data-touched` | |
| Segment (shared) | `role="spinbutton"`, `tabindex="0"` (unless disabled), `aria-label`, `aria-valuenow`, `aria-valuemin`/`aria-valuemax` (omitted for day-period), `aria-valuetext`, `aria-disabled`, `aria-readonly` | |
| Segment (shared) | `data-segment` (segment kind name), `data-placeholder` (present when unfilled), `data-disabled`, `data-readonly`, `data-navius-time-input-segment` | |
| SegmentFieldContext (C# state) | `Layout` (ordered editable segments + literal separators), `Culture`, `ForceLeadingZeros`, `Disabled`, `ReadOnly`, `Required`, `Invalid`, `Dir`, `FocusIndex`/`FocusRequested` (one-shot focus) | Cascaded (fixed) from root to segment parts |
| DateTimeSegment (C# state, per segment) | `Value` (nullable int), `TypeBuffer` (digits typed since last commit), `Min`/`Max`, `ArrowStep`, `PageStep`, `MaxDigits`, `Order`, `Filled` | Mutable model per hour/minute/second/day-period segment |

## Keyboard

Handled per-segment via `NaviusFieldSegment`'s `@onkeydown`, routed to `SegmentMath.HandleKey` (shared with `NaviusDateInput`).

### Numeric segments (hour, minute, second)

| Key | Behavior |
|---|---|
| ArrowUp | `+ArrowStep`, wraps at `[Min, Max]`; empty segment reveals `PlaceholderBasis` instead of stepping from nothing |
| ArrowDown | `-ArrowStep`, same wrap/placeholder-reveal rule |
| PageUp | `+PageStep`, same wrap rule |
| PageDown | `-PageStep`, same wrap rule |
| Home | Jump to `Min` |
| End | Jump to `Max` |
| Backspace / Delete | Clear segment to unfilled placeholder state |
| ArrowLeft | Move focus to previous segment (`MovePrev`) |
| ArrowRight | Move focus to next segment (`MoveNext`) |
| Digit (0-9) | Appends to `TypeBuffer`; auto-advances to the next segment when the candidate exceeds what one more digit could represent, or `TypeBuffer.Length >= MaxDigits` |

### Day-period segment (AM/PM)

| Key | Behavior |
|---|---|
| ArrowUp / ArrowDown | Toggle AM/PM; empty segment reveals AM (0) first |
| `a` / `A` | Set AM (0) |
| `p` / `P` | Set PM (1) |
| Backspace / Delete | Clear to unfilled |
| ArrowLeft | Move focus to previous segment |
| ArrowRight | Move focus to next segment |

Focus movement (`MoveFocus`) honors RTL: for ArrowLeft/ArrowRight the logical direction flips when `Dir == "rtl"`, but auto-advance after a filled digit-type always moves physically forward (`ignoreDir: true`).

## Accessibility

- Root: `role="group"`, `aria-describedby` linking to ambient field description, `aria-invalid`.
- Each segment: `role="spinbutton"` (APG spinbutton pattern) with `aria-label`, `aria-valuenow`, `aria-valuemin`/`aria-valuemax` (day-period segment omits min/max), `aria-valuetext`, `aria-disabled`, `aria-readonly`.
- Focus management: one-shot `FocusRequested`/`FocusIndex` on `SegmentFieldContext`, captured as a render-time `Focus` parameter on the target `NaviusFieldSegment` so it survives the root clearing the flag in its own `OnAfterRender`; the segment then calls `ElementReference.FocusAsync()`.
- Integrates with an ambient `FieldContext` (label/description/validation) via `Field.ApplyControlStateAsync`, reporting `Value`, `Filled`, `Dirty`, `Focused`, `Touched`, `Valid`, `ValueMissing` on every commit and focus transition.
- `Field.FocusControl` is wired to `FocusFirstAsync`, letting an external "focus this field" trigger (e.g. a validation summary) land on the first segment.

## WPF strategy

Tier B: custom lookless control. No native WPF control directly matches an APG-spinbutton-segmented time editor; this is analogous to a masked/segmented `TextBox` and should be a custom `Control` composed of per-segment `TextBlock`/`Border` "cells" inside one focus-scope container, similar to how a masked date picker is typically built in WPF. Each segment's `AutomationPeer` should implement UIA `ValuePattern`/`RangeValuePattern` to mirror `role="spinbutton"` + `aria-valuenow/min/max/text`; the root's `role="group"` maps to a `GroupAutomationPeer` or `AutomationProperties.Name` grouping. `SegmentMath` (`Wrap`/`Step`/`Type`/`Clear`/`HandleKey`) is pure C# with no Blazor dependency and ports essentially unchanged onto `PreviewKeyDown` for a WPF segment control; the digit-typing auto-advance and RTL-aware focus-move logic (`MoveFocus`) likewise port directly, with WPF's `FlowDirection` potentially replacing the manual `Dir` string checks. The ambient `FieldContext` cascading (label/description/validation reporting) should map to whatever field-wrapper pattern the WPF port's Field family adopts (see that family's own parity doc); this component cannot be fully specified without it.

## Open questions

- This component shares its entire segment engine with `NaviusDateInput` (not in this batch's 15 families); the WPF port should likely design one shared segment-editor base control for both rather than duplicating `SegmentMath`/`DateTimeSegment` twice, confirm scope/sequencing with the DateInput parity doc.
- Depends on an ambient `FieldContext` from `Navius.Primitives.Components.Field` (also outside this batch) for label/description/validation wiring; the WPF strategy here is provisional until that family's port shape is decided.
- `MinValue`/`MaxValue` are validation-only (no clamping); confirm the WPF port keeps this "don't silently snap" behavior rather than adopting a clamping `RangeBase`-style pattern.
- Should HourCycle default resolution (culture short-time pattern sniffing for `H` vs `h`) reuse .NET's `DateTimeFormatInfo` the same way, or does WPF/`CultureInfo` on the target platform behave differently.

## WPF implementation notes

Implemented at `src/Navius.Wpf.Primitives/Controls/TimeInput/NaviusTimeInput.cs`. Resolves this
family's own open question directly: **one shared segment-editor base was NOT built**; instead the
engine (`Controls/Internal/SegmentEngine.cs`) and the segment/literal cell
(`Controls/DateInput/NaviusFieldSegment.cs`) are shared verbatim by both `NaviusDateInput` and
`NaviusTimeInput`, while the two roots stay independent `Control`-derived classes. Rationale
recorded in `NaviusTimeInput.cs`'s class remarks: the two roots' layout-building
(`SegmentLayoutBuilder.BuildDateLayout` vs `BuildTimeLayout`) and `Compose()` targets (`DateOnly?`
vs `TimeOnly?`) differ enough that a shared root base would mostly be pass-through plumbing, whereas
sharing the engine + cell type already eliminates all the actual duplication (SegmentMath, layout
tokenizing, focus/keyboard wiring pattern). Theme: `Themes/TimeInput.xaml` (styles the root only;
merges alongside `Themes/DateInput.xaml` for the shared `NaviusFieldSegment` style, per the theme
file's own header comment). Tests: `tests/Navius.Wpf.Tests/TimeInputTests.cs`. Gallery:
`Pages/TimeInputPage.xaml(.cs)`.

Resolved open questions:

- **`HourCycle` resolution reuses `DateTimeFormatInfo` identically**, via
  `SegmentLayoutBuilder.ResolveHourCycle`: an explicit 12/24 wins, otherwise the culture's
  `ShortTimePattern` is sniffed for an `'H'` token, exactly as the web contract specifies. WPF/.NET
  share the same `CultureInfo`/`DateTimeFormatInfo` implementation, so no platform divergence was
  found (verified by `ResolveHourCycle_SniffsCulturePattern_WhenUnset` in `DateInputTests.cs`, since
  the helper is engine-shared).
- **`MinValue`/`MaxValue` stay validation-only**, exactly like `NaviusDateInput`: `IsOutOfRange`/
  `IsInvalidState` are computed read-only DPs; the composed `TimeOnly?` is never snapped.
- **Ambient `FieldContext` not ported**, same as `NaviusDateInput`: `FocusFirstSegment()` stands in
  for `Field.FocusControl`; `IsFilled`/`IsOutOfRange`/`IsInvalidState` are public DPs a future Field
  port can consume directly.

Contract deltas: identical to `NaviusDateInput`'s (no `NaviusBubbleInput`, `Dir` -> `FlowDirection`,
unit-shorthand placeholder tokens instead of literal "Empty", month/day-name-style free-text
valuetext not representable over `IRangeValueProvider`). One TimeInput-specific delta: the
day-period (AM/PM) segment is modeled over the *same* `IRangeValueProvider` as numeric segments
(`Minimum=0`/`Maximum=1`) rather than a second peer/pattern type, since ArrowUp/Down already treat
it as a 2-state range in `SegmentMath.HandleKey`; see `NaviusFieldSegmentAutomationPeer`'s remarks.

Segment-engine coverage (time-specific, beyond what `date-input.md` already covers for the shared
engine): 8 `[Fact]` tests in `TimeInputTests.cs` for `BuildTimeLayout` (12h includes day-period, 24h
omits it, second/hour granularity filtering) and `TimeSegmentComposer` (null-when-incomplete, 12h
midnight/noon/PM round-trip, 24h ignores day-period, absent minute/second default to zero). 9
`[StaFact]` tests cover the control (template building for both hour cycles, seeding, end-to-end
digit typing, AM/PM letter-key toggling, ReadOnly lockout, MinuteStep-driven Arrow increment) and
the automation peer's ValuePattern.
