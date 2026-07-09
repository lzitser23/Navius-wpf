# CurrencyInput

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusCurrencyInput | `<input>` | A caret-stable currency text field whose internal truth is a `decimal?` (never a string). On each `oninput`, a JS `MaskedSelection` bridge reads the raw value + caret atomically; C# (`CurrencyEngine`) parses digits, reformats via `NumberFormatInfo` (grouping, decimal separator, symbol), and re-lands the caret by counting digits; the new value + selection are written back in `OnAfterRenderAsync`. On blur, the value is clamped to `Min`/`Max` and the fraction is padded to `MinFractionDigits`. |

`CurrencyEngine` (`CurrencyEngine.cs`) is not itself a rendered part; it is the pure parse/format core used internally by `NaviusCurrencyInput` (`Parse`, `ToDecimal`, `FormatEditing`, `FormatCommitted`, `CountDigitsBefore`, `CaretForDigits`, `SymbolFor`).

## Parameters

### NaviusCurrencyInput
| Name | Type | Default | Notes |
|---|---|---|---|
| Value | decimal? | none | Controlled value. Use `@bind-Value`. |
| ValueChanged | EventCallback\<decimal?\> | none | |
| DefaultValue | decimal? | none | Uncontrolled initial value. |
| Culture | CultureInfo? | none | Drives symbol, grouping and decimals. Defaults to `CultureInfo.CurrentCulture` when not set. |
| Currency | string? | none | ISO 4217 code; overrides the culture's default currency symbol via `CurrencyEngine.SymbolFor`. |
| MinFractionDigits | int? | none | Defaults to the culture's currency decimal digits; used for blur padding. |
| MaxFractionDigits | int? | none | Defaults to the culture's currency decimal digits; caps digits typeable in the fraction. |
| AllowNegative | bool | `false` (implicit) | Enables recognizing/typing the culture's negative sign. |
| Min | decimal? | none | Clamped on blur. |
| Max | decimal? | none | Clamped on blur. |
| ShowSymbol | bool | `true` | Render the currency symbol in the displayed value. |
| Disabled | bool | `false` (implicit) | Reflected onto the native `disabled` attribute. |
| Invalid | bool | `false` (implicit) | Reflected as `data-invalid` for the skin; purely presentational, no validation logic in this component. |
| Attributes | IDictionary\<string, object\>? | none | Captured unmatched attributes. |

## Events

| Part | Event | Payload |
|---|---|---|
| NaviusCurrencyInput | ValueChanged | `decimal?`, invoked both on each input (with the live parsed value) and on blur if clamping changed the value. |

## State + data attributes

No public context/state classes in this family (no cascading context; `CurrencyEngine` is a static, stateless helper class). Internal component state (not exposed publicly): `_value` (decimal?, current truth), `_display` (string, formatted display text), `_minFrac`/`_maxFrac` (int, resolved fraction digit bounds).

Data attributes rendered on the `<input>`:
- `data-navius-currency-input` - marker attribute, always present.
- `data-empty` - present (empty string) when `_value is null`.
- `data-negative` - present when `_value < 0`.
- `data-invalid` - present when the `Invalid` parameter is true.

## Keyboard

No keyboard interaction implemented in this family. The component has no `@onkeydown` handler; it relies entirely on the native `<input>` element's own text-editing/typing behavior (native caret movement, native character entry). Digit filtering and reformatting happen reactively after the fact via the `@oninput` handler (`OnInputAsync`), not by intercepting or preventing specific keys. `inputmode="decimal"` is set to hint mobile/virtual keyboards toward a numeric layout, but this is a rendering hint, not keyboard-event handling.

## Accessibility

No explicit ARIA role or aria-* attributes are set in the markup (a bare native `<input>` already carries an implicit text-field role). No focus-management logic is implemented (no autofocus, no focus trapping); the only DOM interaction beyond the input's native behavior is JS-side caret/selection restoration after each reformat (`MaskedSelection.SetStateAsync`), which preserves (does not move) the user's editing position relative to the digits they've typed.

## WPF strategy

Tier A (derive from native TextBox)

Derive from `System.Windows.Controls.TextBox`, since the component is fundamentally a formatted single-line text input with no popup/overlay/composite-widget behavior. Port `CurrencyEngine`'s parse/format logic directly (it is pure C# using `System.Globalization.NumberFormatInfo` already, so it should be almost a straight copy); replace the JS `MaskedSelection` bridge's read-value+caret / write-value+caret round trip with WPF's synchronous `TextBox.Text`, `TextBox.CaretIndex`, and `TextBox.Select` on the `TextChanged` event, which removes the need for the two-phase (`OnInputAsync` then `OnAfterRenderAsync`) approach entirely since WPF text mutation and caret restoration can happen in the same handler. No `AutomationPeer` remapping is needed beyond what `TextBoxAutomationPeer` already provides (`ControlType.Edit`, `ValuePattern`), since the Blazor version carries no custom ARIA in the first place. Nothing in this family depends on portal rendering, positioning, or dismissable-layer machinery, so it should be one of the more direct ports in the set; the one behavioral nuance to preserve deliberately is the caret-stability algorithm (`CountDigitsBefore`/`CaretForDigits`), which re-lands the caret by digit-count rather than raw character-offset so typing mid-string doesn't jump the cursor after reformatting/regrouping.

## Open questions

- The code's own doc comment flags two documented deviations from a full ICU/`Intl.NumberFormat` formatter: grouping is fixed at 3 digits (`NumberFormatInfo.CurrencyGroupSizes` is not walked) and negative values render as `NegativeSign + positive` rather than following `CurrencyNegativePattern`'s parenthesized forms. Should the WPF port intentionally preserve these simplifications for behavioral parity with the Blazor version, or use this as an opportunity to implement full `CurrencyGroupSizes`/`CurrencyNegativePattern` support since WPF has no equivalent "keep it JS-interop-simple" constraint?
- `MaskedSelection` interop assumes single-cursor (collapsed) selection semantics implicitly via `dom.SelectionEnd`; should the WPF port explicitly define behavior for a non-collapsed selection (text highlighted, then a digit typed) or treat that as out of scope/identical to native TextBox replace-selection behavior?
- Should `Invalid` (currently purely a presentational passthrough to `data-invalid`) map to WPF's `Validation.HasError`/`ErrorTemplate` infrastructure, or stay as a simple bound bool with no built-in validation semantics, matching the Blazor component exactly?

## WPF implementation notes

Delivered: `src/Navius.Wpf.Primitives/Controls/CurrencyInput/CurrencyEngine.cs` (pure parse/format
core), `NaviusCurrencyInput.cs` (Tier A, TextBox-derived), `Themes/CurrencyInput.xaml`,
`tests/Navius.Wpf.Tests/CurrencyInputTests.cs`,
`apps/Navius.Wpf.Gallery/Pages/CurrencyInputPage.xaml(.cs)`.

**Engine port**: `CurrencyEngine` ported essentially unchanged (it was already pure
`System.Globalization` C#), made `public` for direct test access. The caret-stability algorithm
(`CountDigitsBefore`/`CaretForDigits`, digit-count anchoring) is preserved exactly and covered by
engine tests plus a control-level regrouping test (insert a digit mid-string, the display regroups
from `$1,234` to `$12,934`, the caret lands after the typed digit, not on the moved comma).

**ICU simplifications (first open question resolved)**: preserved deliberately for behavioral
parity with the Blazor version: grouping stays fixed at three digits (`CurrencyGroupSizes` not
walked) and negatives render as `NegativeSign + positive` (never `CurrencyNegativePattern`'s
parenthesised forms). Engine tests pin both deviations.

**Selection semantics (second open question resolved)**: defined explicitly: the caret anchor is
the selection END, collapsed, matching the web bridge's implicit `dom.SelectionEnd` single-cursor
assumption. Typing over a highlighted range therefore behaves like native TextBox
replace-selection followed by one reformat.

**Invalid (third open question resolved)**: stays a simple presentational bool consumed by a
template trigger (Destructive border), matching the Blazor component exactly; no
`Validation.HasError`/`ErrorTemplate` wiring.

**JS bridge collapse**: as the strategy predicted, the `MaskedSelection` two-phase
(`OnInputAsync` then `OnAfterRenderAsync`) approach collapsed into one synchronous
`OnTextChanged` handler (read text + selection, parse, reformat, re-land caret) plus an
`OnLostFocus` override for the blur commit (`CommitValue`: clamp to `Minimum`/`Maximum`, pad the
fraction to `MinFractionDigits`).

**Mapping notes**: `Min`/`Max` became `Minimum`/`Maximum` (WPF range-control convention;
NumberField precedent). `Value` is a two-way `decimal?` DP with a routed `ValueChanged` event that
fires on live parses and on blur clamps, mirroring the contract's dual firing. `data-negative`
maps to a read-only `IsNegative` DP (skin hook, deliberately untinted in the one-ink theme);
`data-empty` is derivable (`Value is null`); `inputmode="decimal"` has no WPF analog (no virtual
keyboard hint API) and was dropped. `Culture` defaults to `CultureInfo.CurrentCulture`; `Currency`
overrides the symbol via `CurrencyEngine.SymbolFor`. No custom AutomationPeer:
`TextBoxAutomationPeer` covers the bare-native-input contract. Tests pin formats with hand-built
`NumberFormatInfo` instances and `CultureInfo.InvariantCulture` so they are immune to ICU/NLS
cultural-data drift.
