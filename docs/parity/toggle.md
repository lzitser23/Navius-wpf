# Toggle

Single-file component: only `NaviusToggle`.

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusToggle | `<button type="button" data-navius-toggle>` | A two-state pressed/unpressed button; native focus and Space/Enter activation come for free |

## Parameters

| Name | Type | Default | Notes |
|---|---|---|---|
| Pressed | `bool` | false | Controlled state |
| PressedChanged | `EventCallback<bool>` | | Controlled-ness determined by `PressedChanged.HasDelegate` (not by whether `Pressed` was explicitly set, unlike most other controlled components in this codebase) |
| DefaultPressed | `bool` | false | Uncontrolled initial state |
| Disabled | `bool` | false | |
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

## Events

| Part | Event | Signature |
|---|---|---|
| NaviusToggle | PressedChanged | `EventCallback<bool>`, fired on click toggle (blocked when `Disabled`); only fired when controlled (`IsControlled` true); in uncontrolled mode the internal field flips with no event |

## State + data attributes

| Element | Attribute | Meaning |
|---|---|---|
| Root | `aria-pressed` | `"true"`/`"false"` |
| Root | `data-pressed` | Present when pressed |
| Root | `data-disabled` | Present when `Disabled` |
| Root | `data-navius-toggle` | Marker |
| Root | native `disabled` attribute | Set from `Disabled` |

## Keyboard

No custom `@onkeydown` handler; native `<button>` semantics provide Space/Enter activation and Tab focus for free.

| Key | Behavior |
|---|---|
| Space / Enter | Native button activation triggers `@onclick` → `ToggleAsync` (no-op when `Disabled`) |

## Accessibility

- `aria-pressed="true"/"false"` (the toggle-button ARIA pattern, distinct from Switch's `role="switch"`).
- Native `disabled` attribute removes it from tab order and blocks activation via browser default behavior.
- No `role` override; relies on the implicit `button` role plus `aria-pressed`.

## WPF strategy

Tier A: derive from `System.Windows.Controls.Primitives.ToggleButton`. This maps almost exactly: `ToggleButton.IsChecked` <-> `Pressed`, `ToggleButtonAutomationPeer` already exposes UIA `TogglePattern` which AT maps to the same semantic as `aria-pressed` (a two-state toggle button, as opposed to Switch's `role="switch"`/`TogglePattern` distinction being more about visual affordance than ARIA). `Checked`/`Unchecked`/`Click` events cover `PressedChanged`. `data-pressed`/`data-disabled` become `Trigger`s on `IsChecked`/`IsEnabled` in a lookless `ControlTemplate`. This is one of the most direct Tier A mappings in the whole set; minimal custom logic needed beyond the template.

## Open questions

- Note the source's `IsControlled` here is keyed on `PressedChanged.HasDelegate`, not on whether `Pressed` was explicitly passed (unlike `NaviusSwitch`/`NaviusSlider`/etc., which check parameter presence in `SetParametersAsync`). Confirm whether this inconsistency is intentional upstream or a latent bug worth flagging before porting the same controlled/uncontrolled detection strategy.
- Should the WPF port unify `Toggle` and `Switch` on one shared `ToggleButton`-derived base (both are boolean on/off controls differing mainly in default visual/AutomationPeer semantics), or keep them fully separate controls as in the Blazor source.

## WPF implementation notes

- Implemented as `Controls/NaviusToggle.cs` : `ToggleButton`, with `IsThreeState` forced to `false` in the constructor (the contract's Toggle is strictly two-state, unlike Checkbox). No other members were added; `ToggleButton.IsChecked` is used directly as `Pressed`.
- Dropped parameters: `ChildContent` maps onto WPF's inherited `Content`; `Attributes` (splat) has no WPF analog and is dropped globally per the porting brief, along with all web-only form-mirroring parameters (not applicable to Toggle, which has none).
- No custom `AutomationPeer` was needed: the native `ToggleButtonAutomationPeer` already reports `AutomationControlType.Button` with `TogglePattern`, matching the contract's `aria-pressed` semantics.
- The controlled/uncontrolled `PressedChanged`/`DefaultPressed` duality collapses onto WPF's single bindable `IsChecked` dependency property (a `Binding` covers controlled use, a plain set covers uncontrolled use); this is the same simplification applied consistently across the other three families in this batch.
- Theme: `Themes/Toggle.xaml` renders a bordered `Border` + `ContentPresenter` with triggers on `IsChecked` (pressed), `IsMouseOver`, `IsFocused`, and `IsEnabled` (disabled), using only the documented `Navius.*` DynamicResource tokens.
