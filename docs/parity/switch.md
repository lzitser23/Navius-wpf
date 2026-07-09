# Switch

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusSwitch | `<button type="button" role="switch" data-navius-switch>` (+ hidden `<NaviusBubbleInput type="checkbox">` when `Name` set) | Root: toggle button with native focus/Space/Enter activation, owns controlled/uncontrolled checked state, cascades `NaviusSwitchContext`, mirrors state into a hidden checkbox for native form submission |
| NaviusSwitchThumb | `<span data-navius-switch-thumb>` | The moving thumb; mirrors the root's discrete state independently for styling (track vs. thumb) |

## Parameters

### NaviusSwitch

| Name | Type | Default | Notes |
|---|---|---|---|
| Checked | `bool` | false | Controlled state; presence in `SetParametersAsync` (not `CheckedChanged.HasDelegate`) determines controlled-ness |
| CheckedChanged | `EventCallback<bool>` | | |
| DefaultChecked | `bool` | false | Uncontrolled initial state |
| Disabled | `bool` | false | |
| Name | `string?` | null | Form field name applied to the hidden checkbox input |
| Value | `string` | "on" | Value submitted with the form when checked (spec default `"on"`) |
| Required | `bool` | false | Native form-validation required flag |
| ReadOnly | `bool` | false | Focusable but value cannot change (spec `readOnly`) |
| Form | `string?` | null | HTML `form` attribute id for the hidden input |
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

### NaviusSwitchThumb

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

## Events

| Part | Event | Signature |
|---|---|---|
| NaviusSwitch | CheckedChanged | `EventCallback<bool>`, fired on click toggle (blocked when `Disabled` or `ReadOnly`) |

## State + data attributes

| Element | Attribute | Meaning |
|---|---|---|
| Root | `role="switch"`, `aria-checked` | ARIA switch role, `"true"`/`"false"` |
| Root | `aria-readonly`, `aria-required` | Present per corresponding parameter |
| Root | `data-checked` / `data-unchecked` | Exactly one present, mirrors `CurrentChecked` (Base UI-style discrete data attributes) |
| Root | `data-disabled`, `data-readonly`, `data-required` | Present per corresponding parameter |
| Root | `data-navius-switch` | Marker |
| Root | native `disabled` attribute | Set from `Disabled` |
| Thumb | `data-checked` / `data-unchecked`, `data-disabled`, `data-readonly`, `data-required`, `data-navius-switch-thumb` | Mirrors root state via cascaded `NaviusSwitchContext`, styled independently of the track |
| NaviusSwitchContext (C# state) | `Checked`, `Disabled`, `ReadOnly`, `Required` | Cascaded (non-fixed) from root to thumb |

## Keyboard

No custom `@onkeydown` handling in the code; the root is a native `<button>`, so Space/Enter activation and Tab focus come for free from the browser's button semantics (no JS needed).

| Key | Behavior |
|---|---|
| Space / Enter | Native button activation triggers `@onclick` → `ToggleAsync` (blocked when `Disabled` or `ReadOnly`) |

## Accessibility

- `role="switch"` with `aria-checked="true"/"false"`.
- `aria-readonly="true"` when `ReadOnly`.
- `aria-required="true"` when `Required`.
- Native `disabled` attribute when `Disabled` (removes it from tab order and blocks activation via browser default behavior).
- Hidden `<NaviusBubbleInput type="checkbox">` mirrors checked/required/disabled state for native form participation (not for screen readers, form submission only).

## WPF strategy

Tier A: derive from `System.Windows.Controls.Primitives.ToggleButton` (or `CheckBox`, which itself derives from `ToggleButton`). WPF's `ToggleButton` already provides `ToggleButtonAutomationPeer` mapping to UIA `TogglePattern` (comparable to `role="switch"`/`aria-checked`), native Space/Enter activation, and `IsChecked`/`Checked`/`Unchecked` events matching `Checked`/`CheckedChanged`. The discrete `data-checked`/`data-unchecked`/`data-disabled`/`data-readonly`/`data-required` attributes map to a lookless `ControlTemplate` with `Trigger`s on `IsChecked`, `IsEnabled`, and new `ReadOnly`/`Required` dependency properties (WPF has no native `ReadOnly` concept on `ToggleButton`, so custom logic must intercept `OnClick`/`OnToggle` to no-op when read-only). `NaviusSwitchThumb` maps to a named template part (`PART_Thumb`) styled from the same triggers rather than a separate cascaded-context component. Hidden checkbox form-submission mirroring has no WPF equivalent and likely drops.

## Open questions

- WPF has no native `ReadOnly` semantics on `ToggleButton`/`CheckBox`; confirm the override approach (block `OnClick` while still focusable) matches intended behavior.
- Form-submission mirroring (`Name`/`Value`/`Form`) has no WPF analog; confirm this parameter set is dropped rather than ported.
- Should `NaviusSwitchThumb` become a mandatory template part (`PART_Thumb`) baked into the default `ControlTemplate`, or remain an optional child like the Blazor version.

## WPF implementation notes

Delivered: `src/Navius.Wpf.Primitives/Controls/Switch/NaviusSwitch.cs` (derives `ToggleButton`),
`Themes/Switch.xaml`, `tests/Navius.Wpf.Tests/SwitchTests.cs`,
`apps/Navius.Wpf.Gallery/Pages/SwitchPage.xaml(.cs)`.

**ReadOnly (first open question resolved)**: overridden via `OnToggle()`, exactly the pattern
already used by `NaviusCheckbox` and `NaviusRadioGroupItem` in this codebase -- while `ReadOnly`,
`OnToggle()` no-ops (does not call `base.OnToggle()`), leaving `IsEnabled`/`Focusable` untouched, so
the control stays focusable and keeps native Space/Enter routing but the value never flips.
Programmatic changes (`toggle.IsChecked = true`) still work while `ReadOnly`, matching the
contract's "the value cannot change" framing as a user-input guard, not a hard lock.

**Form-submission mirroring (second open question resolved)**: dropped entirely, per this wave's
brief that all web form-mirroring parameters (`Name`, `Value`, `Form`, `Required`'s native-validation
half) drop from the WPF port. `ReadOnly` and `Required` are kept as plain bool dependency properties
for API parity (matching `NaviusCheckbox`/`NaviusRadioGroupItem`'s precedent of always exposing
`ReadOnly`+`Required` together), but `Required` has no enforcement behavior in this port -- it is a
property a consumer can read/bind to, same as the sibling families.

**`PART_Thumb` (third open question resolved)**: mandatory template part, not an optional
component. It is declared via `[TemplatePart(Name = "PART_Thumb", ...)]` on `NaviusSwitch` and has
no dedicated CLR type; `Themes/Switch.xaml` styles it directly off the same `IsChecked`/`IsEnabled`
triggers as the track (a `Border` named `Track`), rather than a separate cascaded-context component.

**Peer**: no custom `AutomationPeer` -- the inherited `ToggleButtonAutomationPeer` (UIA
`TogglePattern`) is used as-is, matching `NaviusToggle`'s identical choice; this is the closest
native mapping to `role="switch"`/`aria-checked` available in WPF.

**Thumb animation**: a 120ms `DoubleAnimation` on a `TranslateTransform.X` applied to the
`PART_Thumb` Ellipse, driven by `Trigger.EnterActions`/`ExitActions` on `IsChecked` in
`Themes/Switch.xaml` (plain WPF `Storyboard`, no motion library dependency).
