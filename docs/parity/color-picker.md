# ColorPicker

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| `NaviusColorPicker` | `<div>` (plus a hidden `<input type="hidden">` via `NaviusBubbleInput` when `Name` is set) | Root; owns the color as HSVA (`ColorPickerContext`), projects it to the `Value` string via `Format`, cascades context to all children |
| `NaviusColorPickerArea` | `<div role="group">` containing two `<span role="slider">` thumbs (saturation, brightness) | 2D saturation/value canvas; a visible saturation (x) thumb + a visually-hidden brightness (y) thumb, each its own slider |
| `NaviusColorPickerHueSlider` | `<div>` containing one `<span role="slider">` thumb | Horizontal rainbow hue track |
| `NaviusColorPickerAlphaSlider` | `<div>` containing one `<span role="slider">` thumb | Horizontal alpha (opacity) track over a checkerboard, gradient of current color |
| `NaviusColorPickerField` | `<input type="text">` | Editable hex/color text field; parses on change (hex/rgb/hsl accepted) |
| `NaviusColorPickerSwatches` | `<div role="listbox">` | Preset swatch container (APG listbox); auto-generates `NaviusColorPickerSwatchItem` children from `Colors`, or accepts explicit children |
| `NaviusColorPickerSwatchItem` | `<button type="button" role="option">` | One preset swatch option inside a `Swatches` listbox |
| `NaviusColorPickerSwatch` | `<div role="img">` | Non-interactive preview swatch showing the current color |

Non-rendering shared-state/math files backing these parts (not their own visible parts): `ColorMath.cs` (static HSV/RGB/HSL conversion + parse/format, internal, no dependencies), `ColorPickerContext.cs` (the cascaded HSVA state + projections), `ColorPickerPart.cs` / `ColorPickerTrackPart.cs` (abstract bases handling context-change re-render and, for the tracks, pointer-drag wiring via a JS 2D pointer tracker), `SwatchesContext.cs` (roving-focus coordinator for the swatch listbox).

## Parameters

### NaviusColorPicker

| Name | Type | Default | Notes |
|---|---|---|---|
| `Value` | `string?` | none | Controlled color string (`@bind-Value`) |
| `ValueChanged` | `EventCallback<string>` | none | Pairs with `Value` |
| `DefaultValue` | `string?` | none | Uncontrolled initial color string |
| `Format` | `string` | `"hex"` | Output format: `hex`\|`rgb`\|`rgba`\|`hsl`\|`hsla` |
| `AlphaEnabled` | `bool` | `false` | Exposes an alpha channel (alpha included in the projected string) |
| `Disabled` | `bool` | `false` | |
| `ReadOnly` | `bool` | `false` | |
| `Name` | `string?` | none | Optional form field name; renders a hidden bubble input for native submission |
| `ChildContent` | `RenderFragment?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | Captures unmatched attributes |

### NaviusColorPickerArea

| Name | Type | Default | Notes |
|---|---|---|---|
| `AriaLabel` | `string?` | `"Color"` | Accessible name for the area group |
| `Attributes` | `IDictionary<string, object>?` | none | Captures unmatched attributes; `style` is extracted and merged with the intrinsic gradient background rather than forwarded raw |

### NaviusColorPickerHueSlider

| Name | Type | Default | Notes |
|---|---|---|---|
| `Attributes` | `IDictionary<string, object>?` | none | Captures unmatched attributes; `style` extracted/merged with the intrinsic rainbow gradient |

### NaviusColorPickerAlphaSlider

| Name | Type | Default | Notes |
|---|---|---|---|
| `Attributes` | `IDictionary<string, object>?` | none | Captures unmatched attributes; `style` extracted/merged with the intrinsic checkerboard + alpha gradient |

### NaviusColorPickerField

| Name | Type | Default | Notes |
|---|---|---|---|
| `AriaLabel` | `string?` | `"Hex color"` | |
| `Attributes` | `IDictionary<string, object>?` | none | Captures unmatched attributes |

### NaviusColorPickerSwatches

| Name | Type | Default | Notes |
|---|---|---|---|
| `AriaLabel` | `string?` | `"Swatches"` | |
| `Colors` | `IReadOnlyList<string>?` | none | Preset color strings to auto-render as options |
| `ChildContent` | `RenderFragment?` | none | If supplied, takes precedence over auto-generating items from `Colors` |
| `Attributes` | `IDictionary<string, object>?` | none | Captures unmatched attributes |

### NaviusColorPickerSwatchItem

| Name | Type | Default | Notes |
|---|---|---|---|
| `Value` | `string` | `""` | The swatch's color string; also its accessible name |
| `ChildContent` | `RenderFragment?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | Captures unmatched attributes; `style` extracted/merged with the intrinsic swatch fill |

### NaviusColorPickerSwatch

| Name | Type | Default | Notes |
|---|---|---|---|
| `Attributes` | `IDictionary<string, object>?` | none | Captures unmatched attributes; `style` extracted/merged with the intrinsic checkerboard + color fill |

## Events

| Part | Event | Payload |
|---|---|---|
| `NaviusColorPicker` | `ValueChanged` | `string` (the color projected via `Format`) |

No other part exposes a `[Parameter] EventCallback`. `NaviusColorPickerArea`, `HueSlider`, `AlphaSlider`, `Field`, `Swatches`, and `SwatchItem` all mutate state by calling methods on the cascaded `ColorPickerContext`/`SwatchesContext` (e.g. `SetSaturationValueAsync`, `SetHueAsync`, `SetAlphaAsync`, `SetFromStringAsync`, `SelectAsync`), which funnel back through the root's single `ValueChanged`.

## State + data attributes

`NaviusColorPicker` (root div): `data-navius-color-picker`, `data-disabled`.

`NaviusColorPickerArea` (div): `role="group"`, `aria-label`, `aria-disabled`, `data-navius-color-picker-area`, `data-disabled`. Saturation thumb (span): `role="slider"`, `data-navius-color-picker-area-thumb`, `data-channel="saturation"`, `data-dragging`, `data-disabled`. Brightness thumb (span, visually hidden): `role="slider"`, `data-navius-color-picker-area-thumb-y`, `data-channel="brightness"`, `data-disabled` (no `data-dragging` on this one).

`NaviusColorPickerHueSlider` (div): `data-navius-color-picker-hue`, `data-orientation="horizontal"`, `data-disabled`. Thumb (span): `role="slider"`, `data-navius-color-picker-hue-thumb`, `data-dragging`, `data-disabled`.

`NaviusColorPickerAlphaSlider` (div): `data-navius-color-picker-alpha`, `data-orientation="horizontal"`, `data-disabled`. Thumb (span): `role="slider"`, `data-navius-color-picker-alpha-thumb`, `data-dragging`, `data-disabled`.

`NaviusColorPickerField` (input): `data-navius-color-picker-field`, `data-invalid` (set on a failed parse), `aria-invalid`.

`NaviusColorPickerSwatches` (div): `role="listbox"`, `data-navius-color-picker-swatches`.

`NaviusColorPickerSwatchItem` (button): `role="option"`, `data-navius-color-picker-swatch-item`, `data-selected`, `data-disabled`.

`NaviusColorPickerSwatch` (div): `role="img"`, `data-navius-color-picker-swatch`.

`ColorPickerContext` (internal shared state, public properties): `H`, `S`, `V`, `A` (double, HSVA authoritative model), `Format`, `AlphaEnabled`, `Disabled`, `ReadOnly`; projections `Rgb`, `Projected` (color as `Format` string), `HexValue` (always hex), `CssColor` (rgba() for previews), `HueCss` (pure-hue rgb() for the area's base fill), `AreaThumbLeft`/`AreaThumbTop`, `HueThumbLeft`, `AlphaThumbLeft` (percentage offsets), `Description` (human-readable string used for `aria-valuetext`).

`SwatchesContext` (internal roving-focus coordinator): `CurrentHex`, registered item list, `AnySelected()`.

## Keyboard

| Key | Behavior | Part |
|---|---|---|
| ArrowRight | Saturation += step (0.01, or 0.1 with Shift) | `NaviusColorPickerArea` (both thumbs share the same handler regardless of which is focused) |
| ArrowLeft | Saturation -= step | `NaviusColorPickerArea` |
| ArrowUp | Brightness (V) += step | `NaviusColorPickerArea` |
| ArrowDown | Brightness (V) -= step | `NaviusColorPickerArea` |
| PageUp | Brightness (V) += 0.1 (large step) | `NaviusColorPickerArea` |
| PageDown | Brightness (V) -= 0.1 (large step) | `NaviusColorPickerArea` |
| Home | Saturation = 0 | `NaviusColorPickerArea` |
| End | Saturation = 1 | `NaviusColorPickerArea` |
| ArrowRight / ArrowUp | Hue += 1 degree (10 with Shift) | `NaviusColorPickerHueSlider` |
| ArrowLeft / ArrowDown | Hue -= 1 degree (10 with Shift) | `NaviusColorPickerHueSlider` |
| PageUp | Hue += 10 | `NaviusColorPickerHueSlider` |
| PageDown | Hue -= 10 | `NaviusColorPickerHueSlider` |
| Home | Hue = 0 | `NaviusColorPickerHueSlider` |
| End | Hue = 360 (displayed as 360 even though the model wraps to 0; tracked via a local `_atMax` flag) | `NaviusColorPickerHueSlider` |
| ArrowRight / ArrowUp | Alpha += 0.01 (0.1 with Shift) | `NaviusColorPickerAlphaSlider` |
| ArrowLeft / ArrowDown | Alpha -= 0.01 (0.1 with Shift) | `NaviusColorPickerAlphaSlider` |
| PageUp | Alpha += 0.1 | `NaviusColorPickerAlphaSlider` |
| PageDown | Alpha -= 0.1 | `NaviusColorPickerAlphaSlider` |
| Home | Alpha = 0 | `NaviusColorPickerAlphaSlider` |
| End | Alpha = 1 | `NaviusColorPickerAlphaSlider` |
| Home | Focus moves to the first swatch item | `NaviusColorPickerSwatches` |
| End | Focus moves to the last swatch item | `NaviusColorPickerSwatches` |
| ArrowRight / ArrowDown | Focus moves to the next swatch item | `NaviusColorPickerSwatchItem` |
| ArrowLeft / ArrowUp | Focus moves to the previous swatch item | `NaviusColorPickerSwatchItem` |

`NaviusColorPickerField` and `NaviusColorPickerSwatch` have no `@onkeydown` handlers (the field commits via the native `@onchange`/Enter-or-blur browser behavior; the swatch is non-interactive).

## Accessibility

- `NaviusColorPickerArea`: `role="group"` with `aria-label` (default "Color") and `aria-disabled`; saturation thumb `role="slider"` with `aria-orientation="horizontal"`, `aria-valuemin="0"`, `aria-valuemax="100"`, `aria-valuenow` (rounded percent), `aria-valuetext` (`Context.Description`), `aria-disabled`; brightness thumb `role="slider"` with `aria-orientation="vertical"` and the same value attributes, visually hidden via inline clip-rect CSS but still focusable for screen readers
- `NaviusColorPickerHueSlider`: thumb `role="slider"`, `aria-orientation="horizontal"`, `aria-valuemin="0"`, `aria-valuemax="360"`, `aria-valuenow`, `aria-valuetext` (`Context.Description`, i.e. announces the resulting color, not raw degrees), `aria-disabled`
- `NaviusColorPickerAlphaSlider`: thumb `role="slider"`, `aria-orientation="horizontal"`, `aria-valuemin="0"`, `aria-valuemax="100"`, `aria-valuenow` (alpha percent), `aria-valuetext` (`"{percent}% opacity"`), `aria-disabled`
- `NaviusColorPickerField`: `aria-label` (default "Hex color"), `aria-invalid` set on failed parse
- `NaviusColorPickerSwatches`: `role="listbox"`, `aria-label` (default "Swatches"), `aria-orientation="horizontal"`
- `NaviusColorPickerSwatchItem`: `role="option"`, `aria-selected`, `aria-label` set to the swatch's raw color `Value` string; roving `tabindex` (selected item, or the first item when none match, gets `tabindex="0"`; all others `-1`)
- `NaviusColorPickerSwatch`: `role="img"`, `aria-label` set to `Context.Projected` (the color string itself is the accessible name)
- Focus management: `SwatchesContext.MoveAsync`/`MoveEdgeAsync` explicitly call `.FocusAsync()` on the target swatch element (roving tabindex pattern); no other explicit focus-moving code exists in the family (e.g. opening the picker does not move focus anywhere)
- All draggable tracks (`Area`, `HueSlider`, `AlphaSlider`) get `tabindex="0"` unless `Context.Disabled` (`"-1"`)

## WPF strategy

Tier B (custom lookless control). The picker is a compound of a 2D pointer-driven canvas (`Area`), two 1D pointer-driven tracks (`HueSlider`, `AlphaSlider`), a text field, and a swatch listbox, coordinated through one shared HSVA model (`ColorPickerContext`) with no single native WPF control covering that composite; the closest fit is a lookless `Control` (or `UserControl`) with a dependency-property `Color`/`HSVA` model and named template parts, mirroring the Blazor cascading-context pattern. Individual parts do map onto native building blocks and `AutomationPeer`s: the `Area`'s two `role="slider"` thumbs (saturation x, brightness y) each map to a `RangeValuePattern`-exposing custom automation peer (or two `Slider`-derived controls composited into one canvas), `HueSlider`/`AlphaSlider` map well onto `System.Windows.Controls.Slider` subclasses with `SliderAutomationPeer`/`RangeValuePattern` and custom `Track`/`Thumb` templates for the rainbow/checkerboard backgrounds, `NaviusColorPickerField` maps to a `TextBox`, `NaviusColorPickerSwatches`/`SwatchItem` map onto a `ListBox`/`ListBoxItem` pair (`role="listbox"`/`"option"` map directly to `ListBoxAutomationPeer`/`SelectionItemPattern`), and `NaviusColorPickerSwatch` (a non-interactive `role="img"` preview) maps to a plain `Border`/`Rectangle` with `AutomationProperties.Name` bound to the projected color string, no interactive peer needed. Several things will not translate cleanly: the CSS `linear-gradient`/`conic-gradient` backgrounds driving the hue rainbow, SV square, and alpha checkerboard are computed as inline CSS strings and must be rebuilt as WPF `LinearGradientBrush`/`DrawingBrush` (tile) resources; the pointer-drag model is a JS-side 2D pointer tracker (`PointerTracker2D`, wired via JS interop with pointer-capture semantics) that has no WPF equivalent and must be reimplemented using native WPF mouse capture (`CaptureMouse`/`MouseMove`) on the track element; and the hex field's forced re-key trick (`@key="_rev"` bump to force Blazor to discard/recreate the DOM node so a normalized/rejected value visibly reverts) is a Blazor-specific workaround that a WPF `TextBox` with a `Binding` + `UpdateSourceTrigger` and explicit `Text` reset does not need.

## Open questions

- Should the WPF port keep the exact HSVA-as-source-of-truth model (`ColorPickerContext.H/S/V/A`) or use WPF's `System.Windows.Media.Color` (ARGB bytes) as the canonical model and derive HSV only for the UI, given the two round-trip differently at the edges (e.g. hue at S=0 or V=0 is undefined in HSV but the Blazor code stores it anyway)?
- The `Area`'s hidden brightness (y) slider is a real, separately focusable `role="slider"` element purely for screen-reader access to the second axis; does the WPF port need an equivalent hidden-but-focusable `AutomationPeer`-only element, or can `RangeValuePattern` on a single 2D `Thumb` expose both axes some other way (e.g. two `AutomationPeer`s over one visual)?
- `ColorMath.TryParse`/`Format` accept and emit hex/rgb(a)/hsl(a) text; is text-format parity (exact string formats, e.g. `"rgba({r}, {g}, {b}, {a})"` with a space after each comma) required for the WPF port, or is only the underlying color value expected to match?
- `HiddenUntilFound`-style browser find integration does not apply here, but the swatch listbox's roving-tabindex/APG-listbox pattern (`SwatchesContext`) needs an explicit WPF decision: reuse `ListBox`'s native keyboard navigation (which already does roving selection) versus reimplementing the exact Home/End-on-item split seen in the Blazor code.

## WPF implementation notes

Implemented as `Navius.Wpf.Primitives.Controls.NaviusColorPicker` (`src/Navius.Wpf.Primitives/Controls/ColorPicker/`):
one lookless `Control` owning `Hue`/`Saturation`/`Brightness`/`Alpha` as dependency properties
directly (the HSVA-as-source-of-truth model was kept, resolving that open question), with named
template parts for the Area/Hue/Alpha `Thumb`s, the hex `TextBox`, and a `ListBox` for swatches
(reusing native `ListBox` keyboard navigation rather than reimplementing the roving-tabindex
coordinator). Pointer drag uses `Thumb.DragDelta`, not a 2D pointer tracker. Pure conversion math
(`ColorMath`) and pure keyboard step math (`ColorPickerSteps`) are unit-tested headless with plain
`[Fact]`. The Area's screen-reader-only brightness thumb is not ported as a second hidden peer;
this port exposes one 2D thumb with one `AutomationPeer`. The peer implements `IValueProvider` as
read-only, surfacing `HexValue` (see `NaviusColorPickerAutomationPeer`) since the hex text lives
only in a template `TextBox` that exposes nothing over UIA on its own. `Name`/hidden-bubble-input
form participation is dropped (no WPF form-submission model to mirror).

## M6 audit (2026-07-09)

**CONFIRMED, fixed.** `NaviusColorPickerAutomationPeer` implemented `IValueProvider` but never
overrode `GetPattern(PatternInterface)`. WPF's `AutomationPeer.GetPattern` base implementation
returns `null` for every pattern regardless of which provider interfaces a subclass implements --
it does not auto-detect them (every other `IValueProvider` peer in this port, e.g. `NaviusDateInput`'s,
`NaviusFileUpload`'s, `NaviusTagInput`'s, follows the `GetPattern` override convention). This meant
`ValuePattern` was claimed as implemented but was actually unreachable from any real UIA client.
Fixed by adding the `GetPattern` override. Added `AutomationPeer_GetPattern_SurfacesValuePattern`
regression test.

**CONFIRMED, fixed.** The Hue slider's `End` key is documented (and its own `ColorPickerSteps.cs`
doc comment claims) to "set the model to exactly 0/360," but `HueProperty`'s `CoerceHue` callback
unconditionally wrapped `360 -> 0` via modulo, so `End` produced the identical model value as
`Home` (`Hue = 0`) and visually snapped the hue thumb to the track's LEFT edge instead of its
right edge. Fixed `CoerceHue` to preserve the literal `360` boundary value (safe: `ColorMath.HsvToRgb`
already re-normalizes `h` internally via its own `((h % 360) + 360) % 360`, so 0 and 360 produce
identical RGB) while every other out-of-range value still wraps via modulo as before. Added 3
regression tests (`Hue_SettingExactly360_IsPreservedNotWrappedToZero`,
`Hue_OtherOutOfRangeValues_StillWrapViaModulo`, `Hue_At360_HexValueMatchesHueAtZero`).

**CONFIRMED, fixed.** The current-color preview swatch (next to the hex field) had no
`AutomationProperties.Name` at all, contradicting the contract's `NaviusColorPickerSwatch: role="img",
aria-label=Context.Projected`. Fixed by binding the swatch `Border`'s `AutomationProperties.Name`
to the picker's `Value` in `Themes/ColorPicker.xaml`.

**CONFIRMED, fixed.** The Area/Field/Swatches `AriaLabel` parameters are documented contract
parameters (`docs/parity/color-picker.md` "Parameters": defaults `"Color"`/`"Hex color"`/`"Swatches"`,
caller-overridable), but the WPF port hardcoded these three strings directly into
`Themes/ColorPicker.xaml`'s `AutomationProperties.Name` values with no way for a consumer to
override them, and this was not disclosed as a dropped/simplified parameter anywhere. Fixed by
adding `AreaAriaLabel`/`FieldAriaLabel`/`SwatchesAriaLabel` dependency properties (same defaults)
and rebinding the theme's three `AutomationProperties.Name` values to them via `TemplateBinding`-style
`RelativeSource TemplatedParent` bindings. Added `AriaLabels_DefaultToContractStrings` and
`AriaLabels_AreConsumerOverridable` regression tests.

**CONFIRMED, deferred (documented gap, not fixed).** The Area/Hue/Alpha `Thumb`s expose zero UIA
value/range semantics: they are plain `System.Windows.Controls.Primitives.Thumb` instances with
only a hardcoded `AutomationProperties.Name`, no `IRangeValueProvider`/custom `AutomationPeer` at
all, despite this doc's own "Accessibility" section requiring `role="slider"` +
`aria-valuemin`/`aria-valuemax`/`aria-valuenow`/`aria-valuetext` on each, and the "WPF strategy"
section explicitly proposing "a `RangeValuePattern`-exposing custom automation peer." Keyboard
nudging works correctly (the DP values change), but a screen reader gets no
value/range announcement on any of the three sliders. Not fixed in this pass: doing so properly
requires introducing dedicated `Thumb`-derived classes (or peer factories) for all three tracks,
which is a larger, higher-risk structural change than this audit's remaining budget supports
cleanly; recorded here as a confirmed, disclosed gap (the repo's own established pattern for
disproportionate a11y gaps, e.g. `checkbox.md`'s undocumented `aria-readonly`/`aria-required`
peer) rather than silently left as a false claim.

**CONFIRMED, deferred (documented gap, not fixed).** `NaviusColorPickerSwatches.ChildContent`
(explicit custom swatch items overriding auto-generated `Colors`) has no WPF equivalent: `PART_Swatches`
is unconditionally bound to `Colors`. Not fixed in this pass (would need a new content-authoring
API); recorded as a confirmed, disclosed gap for the same proportionality reason as above.

**PLAUSIBLE, reported only:** (1) `FieldName` is a vestigial no-op DP whose in-code doc comment
("the surviving 'Name' parameter... survives as FieldName") reads as implying functional parity,
while the doc's own implementation notes correctly say `Name` is dropped -- more an API-hygiene
inconsistency than a contract violation. (2) `SyncValueFromModel` includes `Alpha` in the projected
`Value` string unconditionally, even when `AlphaEnabled=false`, if a caller sets `Format="rgba"`/`"hsla"`
directly; could not confirm against original Blazor semantics (not present in this repo) whether
this is a real deviation. (3) `RingThumbStyle`'s `Stroke="Black"`/`Stroke="White"` and the
checkerboard brush's `#FFFFFFFF`/`#FFCCCCCC` are hardcoded rather than `DynamicResource` tokens;
arguably a defensible exception (same class as "the swatch itself must show arbitrary colors") but
undocumented as such, and means the thumb ring/checkerboard don't respond to theme switching.
