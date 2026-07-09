# Form

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusForm | `<form>` | Root form: owns a `FormContext` field registry, cascades it to descendants, and orchestrates submit/reset/errors flow |
| NaviusFormSubmit | `<button type="submit">` | Headless submit button that participates in the native form's submit flow |

`FormContext` (FormContext.cs) and `FormData` (FormData.cs) are internal/support classes, not rendered components; see State section.

## Parameters

### NaviusForm

| Name | Type | Default | Notes |
|---|---|---|---|
| OnSubmit | `EventCallback` | - | Invoked on native form submit only when every registered field is currently valid |
| Errors | `IReadOnlyDictionary<string, string[]>?` | null | Validation errors keyed by field name; pushed to each field's `Error` when the dict reference changes |
| OnClearErrors | `EventCallback` | - | Fired before re-validating on submit and again on reset, to clear stale errors |
| PreventDefault | `bool` | true | When true, native submit is prevented (`@onsubmit:preventDefault`) so the consumer owns submission |
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string, object>?` | null | `CaptureUnmatchedValues`, splatted onto `<form>` |

### NaviusFormSubmit

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string, object>?` | null | `CaptureUnmatchedValues`, splatted onto `<button>` |

## Events

- **NaviusForm.OnSubmit** (`EventCallback`): fires from `HandleSubmitAsync` (bound to `@onsubmit`) after `ClearServerErrorsAsync` + `OnClearErrors` + `RevealAllAsync` run, but only if `FormContext.IsValid` is true afterward. If any field is invalid, `OnSubmit` is NOT invoked and focus is moved to the first invalid field instead.
- **NaviusForm.OnClearErrors** (`EventCallback`): fires twice in the code paths: once at the start of `HandleSubmitAsync` (before revalidation) and once in `HandleResetAsync` (bound to `@onreset`).
- **NaviusFormSubmit**: no `EventCallback` parameters; it is a plain `<button type="submit">` that relies on native form-submission semantics.

## State + data attributes

- `data-navius-form` on `<form>`.
- `data-navius-form-submit` on `<button>`.
- `FormContext` (not rendered): registry of `FieldContext` by name (`Register`/`Unregister`/`FindField`), `IsValid` (true iff every registered field's `IsInvalid` is false), `FirstInvalidField()` (DOM-registration order), `Changed` event raised when a registered field's validity changes, `BuildFormData()` snapshot, `ApplyErrorsAsync`/`RevealAllAsync`/`ClearServerErrorsAsync` internal orchestration methods.
- `FormData` (not rendered): read-only field-name → value snapshot exposed to consumers (e.g. a custom `MatchFn`) via `Get(name)`, indexer (`this[name]`, empty string when absent), and `Names`.

## Keyboard

No `KeyDown`/`OnKeyDown` handlers exist in `NaviusForm.razor` or `NaviusFormSubmit.razor`. Any Enter-to-submit or button-activation-on-Space/Enter behavior comes from the native `<form>` / `<button type="submit">` browser semantics, not from custom code in this family.

| Key | Behavior |
|---|---|
| (none) | No custom keyboard handling in the Form family's code. Native `<form>`/`<button type="submit">` browser defaults apply. |

## Accessibility

No `role=` or `aria-*` attributes are explicitly wired in `NaviusForm.razor` or `NaviusFormSubmit.razor`'s markup.

Focus management: on a blocked (invalid) submit, `FocusFirstInvalidAsync()` in `NaviusForm.razor.cs`-equivalent code block:
1. Looks up `FormContext.FirstInvalidField()`.
2. If the field exposes a `FocusControl`, calls `field.FocusAsync()` directly.
3. Otherwise falls back to JS interop: lazily imports `navius-interop.js` (package path `./_content/Navius.Primitives/navius-interop.js`, falling back to vendored path `./navius-interop.js`) and calls `focusElementById(field.ControlId)`. A `JSException` here is swallowed (best-effort focus, not guaranteed).

## WPF strategy

Tier B (custom lookless control). No native WPF control models a DOM `<form>`'s submit/validation/error orchestration; `NaviusForm` needs a custom `Control` (or attached-property-based coordinator, since WPF has no cascading-parameter mechanism) that exposes a `FormContext`-equivalent registry to descendant field controls. `NaviusFormSubmit` can derive from `System.Windows.Controls.Button` (arguably Tier A on its own) wired so `CanExecute`/`IsEnabled` tracks `FormContext.IsValid`, replacing the native-submit-plus-`preventDefault` model with an explicit command. The JS-interop `focusElementById` fallback used when a field has no `FocusControl` has no WPF equivalent and will not translate: WPF field controls must always expose a focusable `UIElement`/reference so `FocusFirstInvalidAsync` can call `.Focus()` directly, with no async fallback path. The `PreventDefault` parameter (suppressing the browser's native default-submit navigation) has no meaning in WPF, since there is no browser-level default action to prevent.

## Open questions

- `FieldContext` (`FocusControl`, `ControlId`, `FocusAsync`, `RevealAsync`, `SetFormErrorsAsync`, `SetServerInvalidAsync`, `IsInvalid`, `Name`, `Value`) lives in the Field family, not read as part of this extraction; the WPF field-control contract these forms cascade into needs its own parity pass before `FormContext`'s registry can be fully ported.
- Native `<form>` browser behavior (Enter-to-submit inside a plain text input) is implicit and not represented in this family's code; WPF has no equivalent implicit behavior, so a decision is needed on whether to emulate it (e.g. via `Button.IsDefault`) or drop it.
- `FormData.Get` is documented (in FormData.cs's XML comments) as reading "the latest value strings surfaced by each field's `NaviusFormControl`... from its splatted `value` attribute": that wiring lives in the Field family and is out of scope here, so the exact value-sourcing contract for a WPF `FormData` equivalent is unresolved.
- `OnClearErrors` is a non-generic `EventCallback` (no argument carrying which errors were cleared); unclear whether a WPF port needs a richer signature.
- `FocusFirstInvalidAsync` silently no-ops on `JSException` (best-effort focus); it's unclear whether a WPF port should hard-fail or preserve the same silent-fallback behavior when a target field control is unfocusable.
