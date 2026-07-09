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

## M6 audit (2026-07-09)

**CONFIRMED disparity, fixed.** Prior to this audit, `NaviusButton` (`src/Navius.Wpf.Primitives/Controls/NaviusButton.cs`)
was a bare `System.Windows.Controls.Button` subclass with zero custom properties: no `Disabled`,
no `FocusableWhenDisabled`, no test file (`tests/Navius.Wpf.Tests/ButtonTests.cs` did not exist),
and this doc had no "WPF implementation notes" section at all -- i.e. this family was shipped
without the soft-disabled contract parameter ever being ported or the gap being disclosed.

Fix: added `Disabled`/`FocusableWhenDisabled`/`IsSoftDisabled` (read-only) dependency properties.
`Disabled && !FocusableWhenDisabled` sets native `IsEnabled=false` (hard-disabled, unchanged
behavior for the common case). `Disabled && FocusableWhenDisabled` keeps `IsEnabled=true` (so the
button stays focusable/tabbable) but overrides `OnClick()` to no-op -- `ButtonBase.OnClick()` is
the single funnel for mouse, keyboard, and UIA-Invoke activation, so this one override suppresses
all three. Added `NaviusButtonAutomationPeer : ButtonAutomationPeer` overriding `IsEnabledCore()`
to report disabled to UIA when soft-disabled (the `aria-disabled`-while-focusable analog); this
also makes WPF's stock `IInvokeProvider.Invoke()` throw `ElementNotEnabledException` rather than
silently activating a soft-disabled button, which is the correct UIA behavior. Added an
`IsSoftDisabled` opacity trigger to `Themes/Button.xaml` alongside the existing `IsEnabled=False`
one, since the soft-disabled case doesn't get the free `IsEnabled` visual trigger. Added
`tests/Navius.Wpf.Tests/ButtonTests.cs` (11 facts) covering default state, hard- vs soft-disabled
`IsEnabled`/`IsSoftDisabled`, `OnClick` suppression (via reflection on the protected `ButtonBase.OnClick`,
since that's the actual mouse/keyboard funnel), and the AutomationPeer's `IsEnabled()`/`IInvokeProvider.Invoke()`
behavior in both states.

`Type` and `NativeButton` remain unported, unchanged from before this audit -- the doc's own "Open
questions" section already flags both as unresolved design decisions (not false claims), and native
`Button` already exposes `IsDefault`/`IsCancel` as a partial, non-string-typed analog to `Type`'s
`submit`/`reset` values, so this is left as a residual open question rather than a confirmed gap.
