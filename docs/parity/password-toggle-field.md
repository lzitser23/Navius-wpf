# PasswordToggleField

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusPasswordToggleField (Root) | No DOM (renders `ChildContent` inside a `CascadingValue`) | Owns the revealed/hidden visibility state; cascades `PasswordToggleFieldContext` |
| NaviusPasswordToggleFieldInput | `<input data-navius-password-toggle-field-input>` | The password input; switches `type="password"`/`type="text"` as visibility changes |
| NaviusPasswordToggleFieldToggle | `<button data-navius-password-toggle-field-toggle>` | Button that reveals/hides the password |
| NaviusPasswordToggleFieldIcon | No wrapper element (conditional `RenderFragment`) | Renders one of two icon fragments depending on revealed state |
| NaviusPasswordToggleFieldSlot | No wrapper element (conditional `RenderFragment`) | Swaps arbitrary content based on revealed state; supports a fully custom `render`-style callback |

## Parameters

**NaviusPasswordToggleField (Root)**

| Name | Type | Default | Notes |
|---|---|---|---|
| Visible | `bool` | false | Controlled revealed state; pair with `VisibleChanged` |
| DefaultVisible | `bool` | false | Uncontrolled initial revealed state |
| ChildContent | `RenderFragment?` | null | |

**NaviusPasswordToggleFieldInput**

| Name | Type | Default | Notes |
|---|---|---|---|
| Attributes | `IDictionary<string, object>?` | null | CaptureUnmatchedValues; only declared parameter. Splat renders after the literal `autocomplete="current-password"`, so a consumer-supplied `autocomplete` wins |

**NaviusPasswordToggleFieldToggle**

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string, object>?` | null | CaptureUnmatchedValues |

**NaviusPasswordToggleFieldIcon**

| Name | Type | Default | Notes |
|---|---|---|---|
| Visible | `RenderFragment?` | null | Content shown while the password is revealed (`type="text"`) |
| Hidden | `RenderFragment?` | null | Content shown while the password is hidden (`type="password"`) |

**NaviusPasswordToggleFieldSlot**

| Name | Type | Default | Notes |
|---|---|---|---|
| Visible | `RenderFragment?` | null | Content shown while the password is revealed |
| Hidden | `RenderFragment?` | null | Content shown while the password is hidden |
| Render | `RenderFragment<bool>?` | null | Fully custom per-state content, receives current visible state; takes precedence over `Visible`/`Hidden` |

## Events

| Part | Event | Type |
|---|---|---|
| NaviusPasswordToggleField (Root) | VisibleChanged | `EventCallback<bool>` |

Input, Toggle, Icon, and Slot have no public `EventCallback` parameters; the toggle button calls `Context.RequestToggleAsync()` internally, and the input's form submit/reset listener calls `Context.RequestSetAsync(false)` via `[JSInvokable]` callbacks (`OnFormSubmit`, `OnFormReset`).

## State + data attributes

Root: renders no DOM element itself (fragment-only).

Input: `type` = `"text"` when `Context.Visible` else `"password"`, `autocomplete="current-password"` (default, overridable), `data-navius-password-toggle-field-input`. No `data-visible`/`data-state` attribute is rendered on the input.

Toggle button: `type="button"`, `aria-label` = `"Hide password"` when `Context.Visible` else `"Show password"`, `aria-controls` = `Context.InputId`, `data-navius-password-toggle-field-toggle`. Deliberately no `aria-pressed` and no `data-state` (documented rationale: avoid double-announcing state to screen readers).

Public state on `PasswordToggleFieldContext`: `Visible` (bool), `InputId` (stable GUID-based id generated once per context instance, used to wire `aria-controls` and as the input's DOM `id`).

## Keyboard

No component-specific keydown handling is coded. The toggle is a native `<button type="button">`, so Enter/Space activate it per default browser button semantics; the input is a native text/password input with default browser text-editing keys. No custom `@onkeydown` handlers exist on any part in this family.

## Accessibility

Toggle: `aria-label` flips between `"Show password"` and `"Hide password"`; `aria-controls` references the input's `InputId`. `aria-pressed`/`data-state` are intentionally omitted (see comment in source) to avoid redundant screen-reader announcements.

Focus management on toggle click (`ToggleAsync`): a real pointer click (`MouseEventArgs.Detail > 0`) returns focus to the input via a JS interop call to `focusElementById(Context.InputId)`; a keyboard or virtual/assistive activation (`Detail == 0`) leaves focus on the toggle button. The JS module is imported lazily and best-effort (a failed import silently leaves focus wherever it was).

Security-driven behavior on Input: subscribes to the enclosing `<form>`'s `submit` and `reset` events via a JS `FormResetSubmitListener`, and forces `Visible` back to `false` on either event, so a revealed password is never persisted across a submit/reset. This listener creation is best-effort; a `JSException` during setup is swallowed and only the auto-hide-on-submit/reset feature is skipped (toggling itself still works).

## WPF strategy

Tier B (custom lookless control) with a Tier C-flavored caveat on the Input part. WPF's `PasswordBox` deliberately does not support data-binding its `Password` property (security-by-design, `SecureString`-oriented), and has no `type="text"` mode to reveal plaintext in place. Achieving the visible/hidden swap therefore requires overlaying two controls (a `PasswordBox` for the hidden state and a `TextBox` for the revealed state, kept in sync and swapped by visibility, e.g. via a `Visibility`-toggled pair or a custom control that owns both), rather than a single native control switching a `type` attribute the way the Blazor `<input>` does. The Toggle button maps to a plain WPF `Button` (not `ToggleButton`) to intentionally avoid `IsChecked`/`ToggleButtonAutomationPeer`'s pressed-state exposure, matching the source's deliberate omission of `aria-pressed`; use `AutomationProperties.Name` bound to the flipping "Show password"/"Hide password" text, and `AutomationProperties.LabeledBy` or a custom `AutomationPeer.Navigate` override to mirror `aria-controls`. The form-submit/reset auto-hide has no WPF equivalent (no HTML form model) and will not translate directly.

## Open questions

- Given WPF `PasswordBox.Password` cannot be data-bound, should the port keep two overlaid controls (`PasswordBox` + `TextBox`) as the Tier B answer, or is this enough of a platform mismatch to reconsider as Tier C (reinterpret with a single custom masked `TextBox` that never uses `PasswordBox`, trading the OS-level secure-string protections for bindability)?
- The pointer-vs-keyboard focus-return distinction (`MouseEventArgs.Detail > 0` vs `== 0`) is a browser-specific signal; does WPF need an equivalent (e.g. distinguishing `MouseButtonEventArgs` clicks from `AccessKeyPressed`/`Enter` activation), or can this be simplified given WPF's different focus/activation model?
- What should the form submit/reset auto-hide-on-submit security behavior map to in WPF, which has no forms (e.g., a bound `ICommand` execution, a dialog `Closing` event, or should this safeguard be dropped/reimplemented differently)?
- Icon and Slot render no wrapper element and simply swap `RenderFragment` content; should these become `DataTemplateSelector`/`DataTrigger`-based template swaps in XAML rather than separate control classes?
