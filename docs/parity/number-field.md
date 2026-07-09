# NumberField

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusNumberField (Root) | `<div data-navius-number-field>` | Owns the authoritative value + min/max/step/clamp logic; cascades `NumberFieldContext` to its parts |
| NaviusNumberFieldGroup | `<div role="group" data-navius-number-field-group>` | Wraps the input and the increment/decrement buttons; reflects discrete field state |
| NaviusNumberFieldInput | `<input role="spinbutton" data-navius-number-field-input>` | The native text input; shows the formatted value, commits on change/blur, handles keyboard stepping |
| NaviusNumberFieldIncrement | `<button data-navius-number-field-increment>` | Stepper button that adds `Step` to the value; out of tab order; disabled at `Max` |
| NaviusNumberFieldDecrement | `<button data-navius-number-field-decrement>` | Stepper button that subtracts `Step` from the value; out of tab order; disabled at `Min` |

## Parameters

**NaviusNumberField (Root)**

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | `double?` | null | Controlled value (null = empty). Use `@bind-Value` |
| DefaultValue | `double?` | null | Uncontrolled initial value |
| Min | `double?` | null | |
| Max | `double?` | null | |
| Step | `double` | 1 | Coerced to 1 if <= 0 in context sync |
| LargeStep | `double` | 10 | Step for PageUp/PageDown and Shift+Arrow |
| SmallStep | `double` | 0.1 | Step for Alt+Arrow fine adjustments |
| Disabled | `bool` | false | |
| ReadOnly | `bool` | false | |
| Required | `bool` | false | |
| Format | `string?` | null | .NET numeric format string (e.g. `"0.##"`), formatted with InvariantCulture |
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string, object>?` | null | CaptureUnmatchedValues |

**NaviusNumberFieldGroup**

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string, object>?` | null | CaptureUnmatchedValues |

**NaviusNumberFieldInput**

| Name | Type | Default | Notes |
|---|---|---|---|
| Attributes | `IDictionary<string, object>?` | null | CaptureUnmatchedValues; only declared parameter (reads all other state from `NumberFieldContext`) |

**NaviusNumberFieldIncrement**

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string, object>?` | null | CaptureUnmatchedValues |

**NaviusNumberFieldDecrement**

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string, object>?` | null | CaptureUnmatchedValues |

## Events

| Part | Event | Type |
|---|---|---|
| NaviusNumberField (Root) | ValueChanged | `EventCallback<double?>` |

Increment/Decrement/Input have no public `EventCallback` parameters; they call back into `NumberFieldContext` methods (`StepAsync`, `SetToBoundAsync`, `SetTextAsync`) which the root wires internally.

## State + data attributes

Root div: `data-navius-number-field`, `data-disabled` (present/`""` when Disabled, else omitted), `data-readonly`, `data-required` (all discrete presence attributes).

Group div: `role="group"`, `data-navius-number-field-group`, `data-disabled`, `data-readonly`, `data-required`.

Input: `role="spinbutton"`, `inputmode="decimal"`, `autocomplete="off"`, `aria-valuenow`, `aria-valuemin`, `aria-valuemax` (all from `Context.Value/Min/Max`, InvariantCulture), `value` bound to `Context.Display`, `disabled`, `readonly`, `required`, `data-navius-number-field-input`, `data-disabled`, `data-readonly`, `data-required`.

Increment/Decrement buttons: `type="button"`, `tabindex="-1"`, `aria-label="Increase"`/`"Decrease"`, `disabled` (native), `data-navius-number-field-increment`/`-decrement`, `data-disabled`.

Public state on `NumberFieldContext`: `Value`, `Step`, `LargeStep`, `SmallStep`, `Min`, `Max`, `Disabled`, `ReadOnly`, `Required`, `Display` (formatted string), `ControlId`, `CanDecrement`, `CanIncrement` (both account for Disabled/ReadOnly plus bound comparison).

## Keyboard

Handled in `NaviusNumberFieldInput.OnKeyDownAsync`. Effective step is `Step`, or `LargeStep` if Shift is held, or `SmallStep` if Alt is held.

| Key | Behavior |
|---|---|
| ArrowUp | Step value up by the effective step |
| ArrowDown | Step value down by the effective step |
| PageUp | Step value up by `LargeStep` |
| PageDown | Step value down by `LargeStep` |
| Home | Jump to `Min` (no-op if `Min` unset) |
| End | Jump to `Max` (no-op if `Max` unset) |

All stepping goes through `Context.StepAsync`/`Context.SetToBoundAsync`, which are no-ops when `Disabled` or `ReadOnly`, and clamp the result to `[Min, Max]`.

## Accessibility

Input carries `role="spinbutton"` with `aria-valuenow`, `aria-valuemin`, `aria-valuemax` synced to the live value/bounds. Group carries `role="group"` (no explicit `aria-labelledby` wiring is present in the code). Increment/Decrement buttons are `tabindex="-1"` so they never receive tab focus; the input is the sole tab-stop and single source of keyboard interaction. Buttons expose static `aria-label` text ("Increase"/"Decrease") and become natively `disabled` at their respective bound.

## WPF strategy

Tier B (custom lookless control). There is no native WPF NumberBox (that's a WinUI/UWP control), so this should be a lookless `Control` (or `RangeBase`-derived) with a `ControlTemplate` containing a `TextBox`-like edit part plus two `RepeatButton`/`Button` parts, driven by dependency properties mirroring `Value`/`Min`/`Max`/`Step`/`LargeStep`/`SmallStep`. Implement a custom `AutomationPeer` returning `AutomationControlType.Spinner` and implementing `IRangeValueProvider` (`Minimum`/`Maximum`/`Value`/`SmallChange`=Step/`LargeChange`=LargeStep) to mirror `role="spinbutton"` + `aria-valuenow/min/max`. The increment/decrement buttons' `tabindex="-1"` behavior maps directly to `Focusable="False"` in WPF. `Format` maps cleanly to a .NET format string, though the source explicitly uses `InvariantCulture` while WPF apps more commonly want current-culture formatting: decide per app.

## Open questions

- The component comment marks scrub-area pointer-lock dragging, press-and-hold auto-repeat, and Intl-locale formatting as tracked follow-ups, not yet implemented. Should the WPF port defer these too, or should WPF's native `RepeatButton` auto-repeat be adopted immediately since it comes for free?
- `Format` is applied with `InvariantCulture` only; does the WPF port need current-culture / locale-aware number formatting and parsing (the Blazor side also parses with `NumberStyles.Float` + `InvariantCulture`)?
- No `aria-labelledby`/label association is wired on the group or input; how should the WPF control associate an external `Label`?
- `SetTextAsync` reverts to the current value on unparseable input by re-applying `CurrentValue` (forcing a re-render back to `Display`): confirm this "snap back on invalid text" behavior is the desired WPF behavior versus rejecting keystrokes outright.
