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
