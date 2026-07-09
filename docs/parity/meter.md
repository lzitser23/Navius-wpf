# Meter

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusMeter | `div[role=meter]` | Root: static, determinate readout (not a progress bar); resolves value/min/max, cascades MeterContext |
| NaviusMeterTrack | `div[data-navius-meter-track]` | Visual rail containing the Indicator |
| NaviusMeterIndicator | `div[data-navius-meter-indicator]` | Filled portion; exposes `Percentage` for consumer-side sizing (headless, no CSS var) |
| NaviusMeterLabel | `span[id]` | Accessible label; registers its id with the context so the Root wires `aria-labelledby` |
| NaviusMeterValue | `span[data-navius-meter-value]` | Renders formatted value text; defaults to rounded percentage, overridable via ChildContent |

## Parameters

**NaviusMeter**

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | double | 0 | Required; clamped into [Min, Max] |
| Min | double | 0 | Minimum value |
| Max | double | 100 | Maximum value; if `Max <= Min`, context uses `Min + 100` |
| GetValueLabel | `Func<double, string?>?` | null | Optional accessible value-text builder; defaults to rounded percentage + "%" |
| ChildContent | RenderFragment? | null | |
| Attributes | `IDictionary<string, object>?` | null | Captures unmatched attributes |

**NaviusMeterTrack / NaviusMeterIndicator / NaviusMeterLabel / NaviusMeterValue**

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | Value part uses it to override the default formatted text |
| Attributes | `IDictionary<string, object>?` | null | Captures unmatched attributes |

## Events

None. No EventCallback parameters on any part; Meter is a read-only display.

## State + data attributes

- `data-navius-meter` on Root, `data-navius-meter-track`, `data-navius-meter-indicator`, `data-navius-meter-label` on their respective parts. `NaviusMeterValue` has no id attribute shown but carries `data-navius-meter-value`.
- No other data-* state attributes; Base UI Meter is documented in code comments as exposing no data-* attributes beyond the static markers and no CSS variables (headless sizing is left to the consumer).
- Public state: `MeterContext.Value`, `Min`, `Max`, `Fraction` (0..1, min-aware), `Percentage` (Fraction * 100), `LabelId`, `HasLabel`.

## Keyboard

None. Meter is a non-interactive, read-only display; no keyboard bindings exist in the code.

## Accessibility

- Root renders `role="meter"` with `aria-valuemin`, `aria-valuemax`, `aria-valuenow` (all culture-invariant formatted doubles), and `aria-valuetext` (from `GetValueLabel` or rounded percentage + "%").
- `aria-labelledby` is set to the Label's generated id only when a Label part is present (`Context.HasLabel`); otherwise omitted.
- No focus management: Meter parts are not focusable/interactive elements.

## WPF strategy

Tier A (derive from native `ProgressBar`, restyled to non-interactive read-only mode). WPF's `ProgressBar` already exposes `Minimum`/`Maximum`/`Value` and its `AutomationPeer` reports `AutomationControlType.ProgressBar`, which is the closest native mapping to ARIA `role=meter` (WPF has no distinct Meter automation type). The Indicator/Track split maps naturally to a `ControlTemplate` with a track `Border` and a width-bound fill `Border`, since Base UI Meter is headless and leaves sizing to the consumer. `aria-valuetext` maps to `AutomationProperties.ItemStatus` or a custom `RangeValuePattern` override; `aria-labelledby` maps to `AutomationProperties.LabeledBy` bound to the Label element.

## Open questions

- WPF's `RangeValuePattern`/`ProgressBar` semantically implies "progress" (task completion over time); confirm downstream consumers/screen readers won't announce a static Meter (e.g., disk usage) as an active progress operation.
- Whether `GetValueLabel`-style custom value-text formatting should be a `IValueConverter` or a bindable `Func` delegate in the WPF port.

## WPF implementation notes

Delivered: `src/Navius.Wpf.Primitives/Controls/Meter/NaviusMeter.cs` (derives `ProgressBar`),
`NaviusMeterAutomationPeer.cs`, `NaviusMeterValue.cs`, `NaviusMeterLabel.cs`, `Themes/Meter.xaml`,
`tests/Navius.Wpf.Tests/MeterTests.cs`, `apps/Navius.Wpf.Gallery/Pages/MeterPage.xaml(.cs)`.
Structured as a near-mirror of `NaviusProgress`'s own implementation notes, with the deltas below.

**Value model -- genuinely clamped, unlike Progress**: `NaviusMeter.Value` relies entirely on
`RangeBase`'s own default `CoerceValueCallback` (no override), so it clamps into
`[Minimum, Maximum]` exactly per the contract. This is the opposite choice from
`NaviusProgress`'s deliberate "validate, don't clamp" `CoerceValueCallback` override -- Meter's
contract wants real clamping, so no custom coercion was needed at all, only a
`PropertyChangedCallback` (`OnStateChanged`) to refresh derived display state.

**`IsIndeterminate` locked to false**: `IsIndeterminateProperty.OverrideMetadata` adds a
`CoerceValueCallback` that always returns `false`, so `meter.IsIndeterminate = true` is silently
rejected -- Meter is contractually always a static, determinate readout, unlike Progress. This also
resolves the "Open questions" concern about screen readers announcing Meter as an active progress
operation: the indeterminate pulsing visual/behavior from `NaviusProgress` is not reachable here.

**`Maximum <= Minimum` fallback**: `Minimum + 100`, per the contract's own rule (`CoerceMaximum`),
distinct from `NaviusProgress`'s unrelated `Maximum <= 0 -> 100` fallback rule.

**`GetValueLabel` signature**: `Func<double, string?>?` (single `value` parameter), matching the
contract's `Func<double, string?>` exactly -- unlike `NaviusProgress.GetValueLabel`, which deviates
to a two-parameter `Func<double, double, string?>` for its own reasons. No deviation here: this
resolves the contract's second open question in favor of a plain delegate property, same choice
Progress made.

**NaN/Infinity constraint (inherited, not re-solved)**: same as documented in
`docs/parity/progress.md` "WPF implementation notes" -- `RangeBase.ValueProperty`'s own
`ValidateValueCallback` rejects `NaN`/`Infinity` with an `ArgumentException` before any
`NaviusMeter` logic runs; only in-range and out-of-range-but-finite values are reachable, and
out-of-range values are clamped (not flipped to any indeterminate-like state, since Meter has none).

**Automation / `aria-valuetext`**: `NaviusMeterAutomationPeer : ProgressBarAutomationPeer` overrides
`GetItemStatusCore()` exactly like `NaviusProgressAutomationPeer`. `AutomationControlType` is left
as the inherited `ProgressBar` value -- WPF has no distinct Meter automation type, the same
acknowledged gap the contract's own "Open questions" section flags; no attempt was made to spoof a
different control type.

**Parts**: `PART_Track`/`PART_Indicator` are `ProgressBar`'s own required part names (free sizing
from `Value`/`Minimum`/`Maximum`, no manual binding). `NaviusMeterValue`/`NaviusMeterLabel` are
companion `TextBlock` subclasses wired via `Source`/`AutomationProperties.LabeledBy` respectively,
identical idiom to `NaviusProgressValue`/`NaviusProgressLabel`.
