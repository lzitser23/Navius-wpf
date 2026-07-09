# MaskedInput

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusMaskedInput | `<input>` | Caret-stable masked text input; controlled via `@bind-Value` or uncontrolled via `DefaultValue` |

`MaskEngine` (MaskEngine.cs, internal static class) plus its `ElementState` record, `MaskToken` struct, and `MaskTokenKind` enum are the pure masking core consumed by `NaviusMaskedInput`; they render nothing and are not `Navius*` components (see WPF strategy).

## Parameters

### NaviusMaskedInput

| Name | Type | Default | Notes |
|---|---|---|---|
| Mask | `string` | `""` | Mask pattern: `0`=digit, `A`=letter, `*`=alnum, any other char is a fixed literal |
| Value | `string?` | null | Controlled masked value; use `@bind-Value` |
| ValueChanged | `EventCallback<string>` | - | Paired with `Value` for two-way binding |
| DefaultValue | `string?` | null | Uncontrolled initial value (masked or raw; normalized through the mask on first render) |
| Placeholder | `char?` | null | Char shown for empty slots (e.g. `_`); null renders nothing (lazy skeleton) |
| Lazy | `bool` | true | true: trailing fixed tokens appear only once reached; false: eager (always emitted) |
| Overwrite | `bool` | false | Reserved for type-over (replace) mode; accepted for parity, "not yet wired to insertion" per source comment |
| Preprocessors | `IReadOnlyList<Func<ElementState, ElementState>>?` | null | Ordered pure transforms run on the proposed state before masking |
| Postprocessors | `IReadOnlyList<Func<ElementState, ElementState>>?` | null | Ordered pure transforms run on the masked state after masking (postfix, clamp, ...) |
| UnmaskedValueChanged | `EventCallback<string>` | - | Emits the raw editable characters (placeholder-filled, no literals) |
| Disabled | `bool` | false | Reflected as the native `disabled` attribute and `data-disabled` |
| Invalid | `bool` | false | Reflected as `data-invalid` for the skin; "validation is the consumer's concern" per source comment |
| Attributes | `IDictionary<string, object>?` | null | `CaptureUnmatchedValues`, splatted onto `<input>` |

## Events

- **ValueChanged** (`EventCallback<string>`): invoked from `OnInputAsync` after every native `input` event, once the mask pipeline (`RunPipeline` → `MaskEngine.Format`) runs, with the new masked display value (`_display`).
- **UnmaskedValueChanged** (`EventCallback<string>`): invoked from `OnInputAsync` alongside `ValueChanged`, with the placeholder-filled unmasked editable-characters string (`_unmasked`).

## State + data attributes

- `data-navius-masked-input` on `<input>`.
- `data-empty="" ` when the unmasked value (`_unmasked`) is empty, otherwise the attribute is omitted (rendered `null`).
- `data-disabled=""` when `Disabled` is true, otherwise omitted.
- `data-invalid=""` when `Invalid` is true, otherwise omitted.
- Native `disabled="@Disabled"` attribute.
- Internal (non-public) state: `_display` (current masked string shown in the DOM), `_unmasked` (raw editable chars), `_valueSet` (whether the caller bound `Value`, detected in `SetParametersAsync`), `_initialized` (first-render guard), `_hasPending`/`_pendingStart`/`_pendingEnd` (caret to reapply post-render, consumed in `OnAfterRenderAsync`), `_tokens` (parsed `MaskToken` list from `Mask`).

## Keyboard

No `KeyDown`/`OnKeyDown` handlers exist in the component; the only DOM event wired is `@oninput` (`OnInputAsync`).

| Key | Behavior |
|---|---|
| (none) | No key-specific handling in code. All input (typing, Backspace/Delete, paste) funnels through the same native `input` event and is processed uniformly by `MaskEngine.Format`, which filters non-matching characters and recomputes the caret. |

Confirmed by `tests/e2e/specs/tokens.spec.ts`: typing `1234567890abc` against a `(000) 000-0000`-style mask yields `(123) 456-7890` (letters rejected by the digit tokens), and a mid-string digit insertion (after `setSelectionRange(2, 2)`) lands the caret at the expected index (3) rather than jumping to the end, verifying `MaskEngine`'s caret-stability logic end-to-end through real keystrokes.

## Accessibility

No `role=` or `aria-*` attributes are explicitly wired in the markup. No `FocusAsync` calls appear in the component. `data-invalid`/`data-disabled` are plain data attributes intended for skin/CSS styling only; the source comment explicitly states "validation is the consumer's concern," so no `aria-invalid` or `aria-disabled` wiring exists in this component's code.

## WPF strategy

Tier B (custom lookless control). Native WPF `TextBox` has no masking concept, so this needs a custom `Control` (likely composing or deriving from `TextBox`) that ports `MaskEngine.cs`'s `Parse`/`Format`/`Walk` logic essentially unchanged, since it is already pure, side-effect-free C# with no DOM or JS dependency. What will NOT translate cleanly is the JS-interop bridge: `NaviusJsInterop.CreateMaskedSelectionAsync`/`GetStateAsync`/`SetStateAsync` exist to atomically read the proposed value+selection from the DOM and write value+selection back in one call, specifically to work around Blazor's async re-render resetting the caret to the end (handled via the `_hasPending`/`OnAfterRenderAsync` reapply dance). WPF's `TextBox.Text`/`SelectionStart`/`SelectionEnd` are synchronous properties settable directly inside a `TextChanged`/`PreviewTextInput` handler, so this entire async round-trip and post-render caret-reapply mechanism should collapse away rather than be ported literally. Map to `TextBoxAutomationPeer` (or the peer of whatever base is chosen) since no custom ARIA role is present in the Blazor markup, and surface `data-invalid`/`data-disabled` parity via `AutomationProperties`/standard `IsEnabled`/validation adorners instead of data attributes.

## Open questions

- `Overwrite` is explicitly a no-op stub in the Blazor source ("accepted for parity, not yet wired to insertion"); the WPF port must decide whether to implement true type-over/replace-mode masking or carry the same stub forward.
- Since WPF `TextBox` mutations are synchronous (unlike Blazor's async re-render), it's unclear whether any analog of the `OnAfterRenderAsync` pending-caret-reapply plumbing is needed at all in WPF, or whether it disappears entirely.
- `Preprocessors`/`Postprocessors` are typed as `Func<ElementState, ElementState>` using the Blazor `ElementState` record; unresolved whether the WPF port reuses this exact delegate contract or reworks it around WPF-idiomatic patterns (dependency properties, `INotifyPropertyChanged`, etc.).
- No explicit paste-handling or IME/composition-event logic appears in the code (only a single `@oninput` handler); it's unclear from the Blazor source whether IME composition characters are expected to funnel through the same single input path, and WPF's `TextCompositionManager`/`PreviewTextInput` model differs enough that this needs an explicit decision.
- `DefaultValue` vs `Value` precedence is implicit in `OnParametersSet`'s control flow (DefaultValue only used on first init; Value only re-synced when `_valueSet && Value != _display`) rather than documented as an explicit contract; the exact precedence when both change after initial render should be confirmed before porting.
