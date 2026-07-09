# Label

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusLabel | `<label>` | Associates a caption with a control via `for=`, and suppresses accidental text-selection on rapid multi-click (mirrors Base UI Label Root) |

## Parameters

### NaviusLabel

| Name | Type | Default | Notes |
|---|---|---|---|
| For | `string?` | null | Rendered as the `<label>`'s `for` attribute (id of the associated control) |
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string, object>?` | null | `CaptureUnmatchedValues`, splatted onto `<label>` |

## Events

None. `NaviusLabel` has no `EventCallback` parameters. The `OnMouseDown` handler in the code block is a private internal handler bound to `@onmousedown`, not a public `[Parameter]` event.

## State + data attributes

- `data-navius-label` attribute on `<label>`.
- Internal (non-public) `_preventSelect` bool field: set inside `OnMouseDown(MouseEventArgs e)` to `e.Detail > 1` (i.e. true on a double/triple click, false on a single click), then read synchronously by `@onmousedown:preventDefault="_preventSelect"` to arm/disarm default-prevention for that same event. Not exposed to consumers.

## Keyboard

No `KeyDown`/`OnKeyDown` handlers exist in the code. The component wires only `@onmousedown`. Keyboard-focus-forwarding to the associated control (if any) relies entirely on the browser's native `for=` label semantics, not on custom code in this component.

| Key | Behavior |
|---|---|
| (none) | No custom keyboard handling in the Label component's code. |

## Accessibility

No `role=` or `aria-*` attributes are explicitly wired in the markup. The `for="@For"` attribute is the sole accessibility wiring: it relies on standard HTML label-association semantics (click-to-focus the target, and screen readers computing the accessible name/label relationship from it). No `FocusAsync` calls or `tabindex` management appear in the code; the click-to-focus behavior confirmed by `tests/e2e/specs/wave1.spec.ts` ("Label: clicking it focuses the associated input") comes from the browser's native handling of `for=`, not from any C# code in `NaviusLabel.razor`.

## WPF strategy

Tier A (derive from `System.Windows.Controls.Label`). WPF's `Label` already has a `Target` property (an object reference, unlike the DOM `for=`'s string id) and a `LabelAutomationPeer` mapping to the appropriate accessibility role. However, WPF's native `Label.Target` activates the target via an access-key/mnemonic (`Alt`+underlined character), not via a plain mouse click the way HTML `for=` does; the click-to-focus-target behavior (and the double/triple-click text-selection-prevention guard built on `MouseButtonEventArgs.ClickCount`, mirroring `e.Detail > 1`) has no automatic WPF equivalent and must be added explicitly via a `PreviewMouseDown`/`MouseDown` handler on the derived control. Set `AutomationProperties.LabeledBy` on the target element to preserve the DOM `for=`'s screen-reader label association.

## Open questions

- WPF's `Label.Target` binds an object reference (typically via `ElementName=`), while the Blazor `For` parameter is a plain string DOM id; the port needs to decide how a string identifier resolves to a target `UIElement` (e.g. `x:Name` lookup) or whether `For` becomes a bound reference instead.
- Whether the multi-click text-selection-suppression nuance (skip on single click, prevent on double/triple) is meaningful to port at all, given WPF's `TextBox` has its own native double-click-to-select-word behavior that is a different interaction model than DOM text selection.
- The Blazor code gets click-to-focus-target "for free" from the browser's native `for=` handling; nothing in `NaviusLabel.razor` implements it explicitly, so the WPF port must author the focus-forwarding logic from scratch rather than translate existing code.
- `Attributes` (`CaptureUnmatchedValues`) splatting has no direct WPF analog; the port needs an extensibility story (styles, attached properties, etc.) for arbitrary passthrough attributes.
