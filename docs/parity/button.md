# Button

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| `NaviusButton` | `<button>` | Headless Base UI Button; the whole family is this single part |

## Parameters

### NaviusButton

| Name | Type | Default | Notes |
|---|---|---|---|
| `Type` | `string` | `"button"` | Rendered as the `type` attribute |
| `Disabled` | `bool` | `false` | Suppresses activation; combined with `FocusableWhenDisabled` to choose native `disabled` vs. `aria-disabled` |
| `FocusableWhenDisabled` | `bool` | `false` | When `true` and `Disabled`, keeps the button focusable/tabbable (`aria-disabled="true"`) instead of natively `disabled` |
| `NativeButton` | `bool` | `true` | Base UI parity flag; per the source comment, rendering as a non-button element is the asChild/Slot path, which Blazor cannot do (ADR-0003), so this parameter has no rendering effect in the current implementation |
| `OnClick` | `EventCallback<MouseEventArgs>` | none | Invoked on click when not disabled |
| `ChildContent` | `RenderFragment?` | none | |
| `Attributes` | `IDictionary<string, object>?` | none | Captured unmatched attributes |

## Events

| Part | Event | Type |
|---|---|---|
| `NaviusButton` | `OnClick` | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` |

## State + data attributes

No context/state class exists for this family (single stateless part). Rendered attributes:

| Attribute | Condition |
|---|---|
| `disabled` (native HTML attribute) | `Disabled && !FocusableWhenDisabled` |
| `aria-disabled="true"` | `Disabled && FocusableWhenDisabled`; otherwise omitted (`null`) |
| `data-disabled=""` | `Disabled` (either disabled mode); otherwise omitted |
| `data-navius-button` | always |

`@onclick:preventDefault="@Disabled"` additionally suppresses the native click when disabled, and `OnClickAsync` also short-circuits before invoking `OnClick` when `Disabled` is true (belt-and-suspenders against the focusable-while-disabled case where the native `disabled` attribute isn't present to block the event).

## Keyboard

No keyboard interaction implemented in this family beyond native `<button>` behavior (browser-default Enter/Space activation via the native element; there is no `@onkeydown` handler in the source).

## Accessibility

- No explicit `role` is set (native `<button>` has an implicit `role="button"`).
- `aria-disabled="true"` is wired only in the focusable-while-disabled mode (`Disabled && FocusableWhenDisabled`); in the plain-disabled mode the native `disabled` attribute is relied on instead.
- No other `aria-*` attributes and no focus-management code.

## WPF strategy

Tier A (derive from `System.Windows.Controls.Button`)

Derive directly from `System.Windows.Controls.Button`, which already exposes a `Click` routed event (maps to `OnClick`) and a `ButtonAutomationPeer` implementing `IInvokeProvider` (the correct UIA mapping; no ARIA role is used in the source to need overriding). `Type` (HTML `type="submit"|"button"|"reset"`) has no WPF equivalent and should be dropped or repurposed as a semantic no-op/marker property, since WPF has no form-submission concept. `FocusableWhenDisabled` needs custom work: native WPF sets `Focusable` effectively moot once `IsEnabled="False"` (disabled controls are skipped in tab order and don't receive input), so reproducing "focusable + `aria-disabled` but inert" requires keeping `IsEnabled="True"`, setting `Focusable="True"` explicitly, suppressing the `Click`/keyboard-activation handlers manually when the custom "soft-disabled" flag is set, and setting `AutomationProperties.IsOffscreenBehavior`/exposing the disabled state to `ButtonAutomationPeer` via an override (WPF's automation peer does not have a first-class `aria-disabled`-while-enabled concept). `NativeButton`/asChild has no WPF translation need: WPF's `ContentPresenter`/`ControlTemplate` model already allows arbitrary templated content without an element-type-swap mechanism, so this flag can simply be dropped in the port.

## Open questions

- Whether `NativeButton` (currently a no-op parity flag with no rendering effect) should be ported at all, given WPF has no asChild/Slot equivalent to gate.
- Whether `Type`'s three HTML values (`submit`/`button`/`reset`) should map to anything in the WPF port (e.g. `IsDefault`/`IsCancel` on `Button` approximate `submit`/`reset` inside a dialog) or be dropped entirely as meaningless outside a `<form>`.
- Confirm whether "soft-disabled" (`FocusableWhenDisabled`) is exercised anywhere else in the primitives set (e.g. other families with the same pattern) so the WPF mechanism for it can be shared rather than reinvented per-control.
