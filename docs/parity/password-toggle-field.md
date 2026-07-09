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

## WPF port notes (implemented 2026-07-09)

Shipped as `Controls/PasswordToggleField/`: `NaviusPasswordToggleField` (root owning `Visible`), `NaviusPasswordToggleFieldInput`, `NaviusPasswordToggleFieldToggle`, `NaviusPasswordToggleFieldIcon`, `NaviusPasswordToggleFieldSlot`.

PasswordBox strategy (the flagged port risk, resolved): the password stays INSIDE the native controls at all times, there is no bindable plaintext dependency property. The Input's template overlays two real controls, `PART_PasswordBox` (authoritative while hidden) and `PART_TextBox` (authoritative while revealed); the value is copied across exactly at the moment `Visible` flips, and on hide the TextBox is cleared immediately after the copy so plaintext never lingers in a loaded-but-collapsed TextBox. This preserves WPF's PasswordBox security-by-design intent (the non-bindable `Password` is deliberate) while still delivering the reveal behavior. Two opt-in escape hatches exist: `GetPassword()` (an explicit plaintext read, same trust boundary as touching `PasswordBox.Password` directly) and a bubbling `PasswordChanged` routed event that carries no plaintext. The Tier C alternative from the open questions (a single custom masked TextBox, never using PasswordBox) was rejected: it would trade away the OS-level protections just to gain bindability the contract does not actually require.

Other deltas:

- Toggle is a plain `Button` (not `ToggleButton`): `ButtonAutomationPeer` exposes no pressed state, matching the source's deliberate omission of `aria-pressed` for the same no-double-announcement reason. `AutomationProperties.Name` flips between "Show password" and "Hide password".
- `aria-controls` is dropped: this WPF runtime's `AutomationPeer` has no overridable `GetControllerForCore` (only the non-virtual `GetControllerForProviderArray`), so there is no supported extensibility point for a ControllerFor relationship.
- The form submit/reset auto-hide does not port (no HTML form model, see `docs/adr/0001-web-form-participation-params.md`); a consumer or a containing `NaviusForm` can set `Visible=false` in its submit handler to reproduce the safeguard.
- The pointer-vs-keyboard focus-return distinction (`MouseEventArgs.Detail`) is not ported; WPF button activation keeps focus on the toggle in both cases, which matches the web's keyboard path and avoids a browser-specific heuristic.
- Icon and Slot became `ContentControl`s swapping `VisibleContent`/`HiddenContent` (the open question's DataTemplateSelector alternative was unnecessary; Content swap is the native analog of the fragment swap). Slot adds `ContentFactory` (`Func<bool, object>`) mirroring the `Render` render-prop, taking precedence over the two content properties.
- No cascading-parameter analog exists, so the root pushes state down (from `OnContentChanged` and every `Visible` change) instead of parts pulling a context; this also keeps everything working headlessly, where `FrameworkElement.Loaded` never fires.
- The contract's `DefaultVisible` (uncontrolled initial revealed state) is not ported as a separate parameter: WPF's `Visible` dependency property already serves both the controlled and uncontrolled roles (a consumer sets it once for an initial value, or two-way binds it for controlled use), so a distinct `DefaultVisible` would be redundant. `Visible` defaults to `false`, matching the contract's `DefaultVisible` default.

## M6 audit (2026-07-09)

Adversarial re-verification against the C#/XAML at file:line, with the security check as the priority.

Security result (HELD UP, no leak): the masked password never reaches a bindable/gettable dependency property and never reaches the UI Automation surface in the hidden state.
- Storage: the value lives only inside the native `PART_PasswordBox` (`SecureString`-backed) while hidden and, while revealed, inside `PART_TextBox` (the intended plaintext state). Neither the `Input` nor the `Root` exposes a bindable plaintext DP; `GetPassword()` is an explicit opt-in method (same trust boundary as touching `PasswordBox.Password` directly) and `PasswordChanged` is a routed event carrying no plaintext (NaviusPasswordToggleFieldInput.cs:108-129).
- On hide, the plaintext is copied back into the `PasswordBox` and the `TextBox` is immediately cleared and collapsed (`ApplyVisibility`, lines 88-100), so nothing lingers in a loaded-but-invisible control.
- Automation: `NaviusPasswordToggleFieldInput` has no custom peer; the authoritative `PasswordBox` reports `IsPassword() == true` (UIA masks it) and does not surface the plaintext through the Value pattern. Two new regression tests lock this in: `HiddenState_DoesNotLeakPlaintextThroughTheAutomationSurface` and `RevealThenHide_LeavesNoPlaintextInTheAutomationSurface` assert on the actual peer/Value-pattern surface, not merely on DP types.

CONFIRMED (fixed):
- Doc completeness: the WPF notes did not record that the contract's `DefaultVisible` parameter was dropped. Added a delta line explaining that WPF's `Visible` DP covers both the controlled and uncontrolled roles, so `DefaultVisible` is redundant (default `false` preserved).

Verified true (spot checks):
- The toggle is a real `Button` (NaviusPasswordToggleFieldToggle.cs:26) and is therefore keyboard-operable via Enter/Space through native `ButtonBase` semantics; its `Click` handler flips `Visible` (line 44-45), covered by `ToggleClick_FlipsVisible_AndRaisesVisibleChanged`. `AutomationProperties.Name` flips "Show password"/"Hide password" (line 41-42), covered by `ToggleAccessibleName_FlipsBetweenShowAndHidePassword`. No `aria-pressed`/toggle-state analog is exposed, matching the deliberate web omission.
- `aria-controls` is genuinely dropped (no `GetControllerForCore` override anywhere), the form submit/reset auto-hide is genuinely absent (no such code), and the pointer-vs-keyboard focus heuristic is genuinely absent, all as documented.
- `Themes/PasswordToggleField.xaml` uses only `DynamicResource` for tokens (`Navius.Background`, `Navius.Foreground`, `Navius.Input`, `Navius.MutedForeground`, `Navius.Radius.Control`), all present in both token dictionaries. `Background="Transparent"` on the inner boxes/toggle is a theme-neutral literal, not a token.

PLAUSIBLE (residual, unfixed): none.
