# Field

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusField | `div[data-navius-field]` | Root. Creates and cascades `FieldContext` (control id, validity, interaction state); merges discrete field-state attributes onto itself. |
| NaviusFieldLabel | `label[for][data-navius-field-label]` | Label pointing at the control id so a native click focuses the control. |
| NaviusFieldControl | `NaviusInput` (default) or `CascadingValue<ControlProps>` + `ChildContent` | Renders the field-aware input, or cascades `ControlProps` (id/aria-describedby/aria-invalid) for a consumer-supplied custom control. |
| NaviusFieldDescription | `p[data-navius-field-description]` | Supplementary field information text. |
| NaviusFieldItem | `div[data-navius-field-item]` | Groups one item (a label + description) inside a checkbox/radio group. |
| NaviusFieldError | `div[role=alert][data-navius-field-error]` (conditional) | Error message tied to a validity match; shown only while validity is surfaced and the match fails. |
| NaviusFieldValidity | (none, render-prop only) | Exposes the field's live `FieldValidity` to `ChildContent`; renders nothing itself. |
| NaviusInput | `input[data-navius-input]` | Field-aware native input. Controlled via `Value`/`ValueChanged` or uncontrolled via `DefaultValue`. Bridges native `ValidityState` + interaction state via JS interop (`createConstraintValidation`). |

Note: `FieldPart` (abstract `ComponentBase`) is the shared base for Label/Control/Description/Item/Error/Validity (subscribes to `FieldContext.Changed`, exposes `StateAttributes`) but is not itself a rendered component, so it has no row above.

## Parameters

### NaviusField

| Name | Type | Default | Notes |
|---|---|---|---|
| Name | string | `""` | `[Parameter, EditorRequired]`. Mirrors Base UI `Field.Root name`. |
| Disabled | bool | `false` | Takes precedence combined with an enclosing Fieldset's `Disabled` (OR'd together). |
| Validity | FieldValidity? | `null` | Full controlled-validity snapshot; when set, overrides the `Invalid` convenience flag. |
| Invalid | bool | `false` | Convenience "mark invalid" flag; maps to `FieldValidity { Valid = false, CustomError = true }` when `Validity` is null. |
| ServerInvalid | bool | `false` | Server-side invalidity; auto-clears internally on the next user edit. |
| ValidationMode | FieldValidationMode | `OnSubmit` | One of `OnSubmit`, `OnBlur`, `OnChange`; gates when native validity is surfaced. |
| ChildContent | RenderFragment? | `null` | Field body (label/control/description/error). |
| Attributes | IDictionary<string,object>? | `null` | `[Parameter(CaptureUnmatchedValues = true)]`; splatted (merged with state attrs) onto the root div. |

### NaviusFieldLabel

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | `null` | Label content. |
| Attributes | IDictionary<string,object>? | `null` | Splatted onto the `<label>`, merged with field state attrs. |

### NaviusFieldControl

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment<ControlProps>? | `null` | When set, cascades `ControlProps` (recomputed every render) instead of rendering the default `NaviusInput`. |
| Attributes | IDictionary<string,object>? | `null` | Splatted onto the default `NaviusInput` when `ChildContent` is null. |

### NaviusFieldDescription

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | `null` | Description content. |
| Attributes | IDictionary<string,object>? | `null` | Splatted onto the `<p>`, merged with field state attrs. |

### NaviusFieldItem

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | `null` | Item content (a label + description grouping). |
| Attributes | IDictionary<string,object>? | `null` | Splatted onto the `<div>`, merged with field state attrs. |

### NaviusFieldError

| Name | Type | Default | Notes |
|---|---|---|---|
| Match | string? | `null` | Validity key (e.g. `"valueMissing"`) this error fires on. Omitted = catch-all shown whenever the field is invalid. |
| ForceMatch | bool | `false` | Shows the error unconditionally regardless of validity (mirrors spec `match={true}`). |
| ChildContent | RenderFragment? | `null` | Custom error content; if null, renders `Field.Errors` joined by spaces. |
| Attributes | IDictionary<string,object>? | `null` | Splatted onto the `<div>`, merged with field state attrs. |

### NaviusFieldValidity

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment<FieldValidity>? | `null` | `[Parameter, EditorRequired]`. Receives the field's current `FieldValidity`; component renders nothing else. |

### NaviusInput

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | string? | `null` | Controlled value; use `@bind-Value`. Setting this parameter (even to null) marks the input controlled (`SetParametersAsync` checks presence, not value). |
| ValueChanged | EventCallback<string> | n/a | Paired with `Value` for two-way binding. |
| DefaultValue | string? | `null` | Uncontrolled initial value, used only when `Value` was never set. |
| Attributes | IDictionary<string,object>? | `null` | Splatted onto the `<input>`, merged with field/local state attrs. |

## Events

| Part | Event | Signature | Fires when |
|---|---|---|---|
| NaviusInput | ValueChanged | `EventCallback<string>` | On every native `oninput` event, with the new string value (before/independent of the JS-interop constraint-validation bridge). |

`NaviusInput.OnFieldStateChange` is a `[JSInvokable]` method (not a `[Parameter]` EventCallback) invoked by the JS engine's `createConstraintValidation` bridge on input/change/invalid/focus/blur, carrying a `FieldStatePayload` (validity flags + focused/touched/dirty/filled). When cascaded inside a `NaviusField`, this is applied to `FieldContext.ApplyControlStateAsync`; standalone, it updates local state only.

## State + data attributes

Static markers (always present on the relevant part):

| Attribute | Element |
|---|---|
| `data-navius-field` | NaviusField root div |
| `data-navius-field-label` | NaviusFieldLabel `<label>` |
| `data-navius-field-description` | NaviusFieldDescription `<p>` |
| `data-navius-field-item` | NaviusFieldItem `<div>` |
| `data-navius-field-error` | NaviusFieldError `<div>` (only while shown) |
| `data-navius-input` | NaviusInput `<input>` |

Discrete state attributes (`FieldContext.StateAttributes`, merged onto NaviusField's root and every `FieldPart`-derived part via `MergeState`):

| Attribute | Set when |
|---|---|
| `data-disabled` | `Disabled` true (field's own `Disabled` OR enclosing fieldset's `Disabled`) |
| `data-valid` | `Valid == true` |
| `data-invalid` | `Valid == false` |
| `data-dirty` | `IsDirty` true |
| `data-touched` | `IsTouched` true |
| `data-filled` | `IsFilled` true |
| `data-focused` | `IsFocused` true |

`NaviusInput` outside a `NaviusField` computes an equivalent local set (`data-dirty`/`data-touched`/`data-filled`/`data-focused`, no disabled/valid/invalid) from its own tracked state.

Internal (non-DOM) state: `FieldContext.Valid` is a tri-state `bool?` (`null` = not yet revealed per `ValidationMode`, matching the spec's `valid: null`). `EffectiveValidity` picks consumer validity over native `ValidityState` when the consumer marks the field invalid. `Errors` (form errors, else native `ValidationMessage`) feeds the default `NaviusFieldError` render. Active `aria-describedby` message ids are tracked as an ordered registry (`RegisterMessage`/`SetMessageActiveAsync`).

## Keyboard

No component-level keyboard handling; every Field-family file was checked (`NaviusField`, `NaviusFieldLabel`, `NaviusFieldControl`, `NaviusFieldDescription`, `NaviusFieldItem`, `NaviusFieldError`, `NaviusFieldValidity`, `NaviusInput`, `FieldContext`, `FieldPart`) and none registers a `KeyDown`/`OnKeyDown` handler or checks a key. Behavior relies entirely on native `<input>` focus/typing and the native `<label for>` click-to-focus delegation. No `tests/e2e` files exist for this family (confirmed by glob search) to cross-check.

## Accessibility

- `NaviusFieldLabel` renders `<label for="@Field.ControlId">`, giving native click-to-focus delegation to the control.
- `NaviusFieldError` renders `role="alert"` while shown.
- `aria-describedby`: `NaviusInput` sets `aria-describedby="@Field?.DescribedBy"` when cascaded; `ControlProps.Attributes` includes `aria-describedby` for custom controls via `NaviusFieldControl`'s `ChildContent` path. `FieldContext.DescribedBy` is the space-joined, registration-ordered list of currently-active message ids (from `NaviusFieldError.RegisterMessage`/`SetMessageActiveAsync`, called from `OnInitialized`/`OnAfterRenderAsync`).
- `aria-invalid`: `NaviusInput` sets `aria-invalid="true"` when `Field.IsInvalid`; `ControlProps.Attributes` sets `aria-invalid="true"` when `Invalid` for the custom-control path.
- Focus management: `FieldContext.FocusControl` is a delegate set by `NaviusInput` in `OnAfterRenderAsync` to call `_input.FocusAsync()`; `FieldContext.FocusAsync()` exposes this so "the form to focus the first invalid field" (per the code comment on `FocusControl`).
- The JS interop bridge (`NaviusJsInterop.CreateConstraintValidationAsync`) wires native focus/blur/input/invalid listeners on the `<input>` element; failures are swallowed (`catch (JSException)`) so binding and top-down validity still function without it.

## WPF strategy

Tier B (custom lookless control). `NaviusField` is a context-provider wrapper (not a single native control) that must be reimplemented as a lookless `Control`/`ContentControl`-derived type owning an equivalent `FieldContext` (a `DependencyObject` or view-model exposing `Valid`, `IsDirty`, `IsTouched`, `IsFilled`, `IsFocused` as dependency properties so triggers/styles can react, replacing the `data-*` attribute mechanism). `NaviusFieldLabel` maps cleanly to a native `Label` with `Target` bound to the control (AutomationProperties.LabeledBy for `AutomationPeer` wiring); `NaviusInput` maps to a `TextBox`-derived control. The two things that will NOT translate cleanly: (1) the JS-interop `createConstraintValidation` bridge that surfaces the browser's native `ValidityState` (badInput/patternMismatch/stepMismatch/etc.) has no WPF equivalent, since WPF's `TextBox` has no native HTML5-style constraint validation, this needs to be reimplemented against `INotifyDataErrorInfo`/`ValidationRule`; (2) the `ControlProps`/`@attributes` splat pattern used by `NaviusFieldControl.ChildContent` for arbitrary custom controls has no direct WPF analog and needs an attached-property or `TemplateBinding` convention instead.

## Open questions

- `FieldValidationMode.OnSubmit` is defined as "surfaced only after a submit attempt" but nothing in this family's code triggers that reveal; it is triggered externally (by a `Form`, per `FieldContext.RevealAsync()` being `internal` and the `FormChanged` bridge). The WPF port needs an equivalent submit-trigger convention since WPF has no `<form>` element.
- `FieldContext` auto-clears `ServerInvalid` and form errors "on the next user edit" (inside `ApplyControlStateAsync`, gated on `valueChanged`). For non-text WPF controls (ComboBox, DatePicker, CheckBox) that don't have a per-keystroke "input" concept, what counts as "the next edit" needs a product decision.
- `FieldValidity`'s flags (`BadInput`, `PatternMismatch`, `StepMismatch`, `TypeMismatch`, etc.) mirror HTML5 `ValidityState` keys. WPF controls have no native equivalent for most of these; the port needs to decide which flags are meaningful for which WPF control types, or collapse to a smaller invalid/valid + custom-message model.
- `NaviusFieldControl`'s two modes (default `NaviusInput` vs. `ChildContent` cascading `ControlProps`) imply a "any custom control can splat these attributes" pattern that depends on Blazor's `@attributes` splatting; the WPF equivalent (attached properties? a marker interface?) is unspecified.
- `ControlId` and the various generated ids (`navius-field-control-*`, `navius-field-error-*`, `navius-input-*`) are GUID-based DOM ids for `for=`/`aria-describedby` wiring; WPF's `AutomationProperties.LabeledBy`/`AutomationProperties.DescribedBy` use element references, not string ids, so the id-registry mechanism (`_messageOrder`, `_activeMessageIds`) may not be needed at all in the port.

## WPF port notes (implemented 2026-07-09)

Shipped as `Controls/Field/`: `NaviusField`, `NaviusFieldLabel`, `NaviusFieldControl`, `NaviusFieldDescription`, `NaviusFieldItem`, `NaviusFieldError`, `NaviusInput`. Parity of outcome through native mechanics, per the locked plan: instead of porting the JS-interop `createConstraintValidation` bridge, `NaviusField` listens to WPF's own bubbling `Validation.Error` routed event (so any descendant Binding using `ValidatesOnDataErrors`/`ValidatesOnNotifyDataErrors`/`ValidationRules` participates automatically) and computes the discrete web state attributes as read-only dependency properties from real bubbling control events: `IsDirty`/`IsFilled` (TextChanged), `IsTouched`/`IsFieldFocused` (GotFocus/LostFocus), `IsFieldValid` (tri-state `bool?`, null until revealed, matching the spec's `valid: null`) and `IsFieldInvalid`. Templates and styles trigger on these DPs exactly where the web styles `data-*` attributes (Themes/Field.xaml demonstrates with a `RelativeSource AncestorType=NaviusField` DataTrigger).

`ValidationMode` reveal gates are implemented as specified: `OnSubmit` (default) reveals only when `NaviusField.Reveal()` is called, which `NaviusForm` does on every submit attempt (resolving the first open question: the submit-trigger convention is `Reveal()`, public so consumers outside a Form can drive it too); `OnBlur` reveals on the first descendant LostFocus; `OnChange` on the first descendant TextChanged.

Deltas and resolved open questions:

- No cascading-parameter analog exists in WPF, so `NaviusField` pushes instead of parts pulling: on `OnContentChanged` it walks its logical descendants once to register the control (defaulting an empty `NaviusFieldControl` to a `NaviusInput`), wire `NaviusFieldLabel.Target`, and seed every `NaviusFieldError`; on each validity change it pushes fresh state to the errors. This deliberately avoids `FrameworkElement.Loaded`, which never fires outside a live Window.
- The GUID id registry (`ControlId`, `DescribedBy`, message ordering) is dropped entirely: WPF label/description associations use element references (`Label.Target`, `AutomationProperties`), confirming the extraction's suspicion that the registry is unnecessary here.
- `FieldValidity`'s HTML5 `ValidityState` flag set collapses to invalid/valid plus a message list (`GetErrors()` merges live `Validation.GetErrors` messages on the registered control with `ExternalErrors`); `NaviusFieldError.Match` therefore matches a message string, not a validity key.
- `Name` became `FieldName` (WPF's `FrameworkElement.Name` is load-bearing for `x:Name`/`FindName`).
- `ServerInvalid` auto-clears on the next descendant TextChanged, matching the contract; the "what counts as an edit for non-text controls" question stays open and currently answers "a bubbling TextChanged", i.e. text controls only.
- The `ControlProps`/@attributes splat pattern is replaced by WPF's ordinary Content model: put any control inside `NaviusFieldControl` and the field registers it; nothing needs to be splatted.
- `NaviusFieldValidity` (render-prop exposing the validity snapshot) has no separate control: consumers bind directly to the public read-only DPs, which is the idiomatic WPF equivalent of a render prop.
- `NaviusFieldError` carries `AutomationProperties.LiveSetting=Assertive`, the UIA analog of `role="alert"`.
