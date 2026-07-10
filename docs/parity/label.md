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

## WPF implementation notes

- Implemented as a single new file, `Controls/NaviusLabel.cs` : `Label`.
- Resolved open question 1: `For` stays a plain `string?` dependency property (not a bound object reference). It resolves to a `FrameworkElement` by name lookup (`FindName`), walking up from the label to the nearest ancestor `NameScope` that has it, and the resolved element is assigned to the inherited `Label.Target`. Resolution is retried on `Loaded` (covers XAML declaration order where `For` is set before the target is registered) and eagerly on `For`'s property-changed callback.
- Resolved open question 3: click-to-focus is authored from scratch via an `OnMouseDown` override, since WPF's native `Label.Target` only drives access-key/mnemonic activation, not a plain click.
- Resolved open question 2: the double/triple-click guard was ported (`e.ClickCount > 1` mirrors the source's `e.Detail > 1`), setting `e.Handled = true` instead of forwarding focus a second time on rapid multi-click.
- `AutomationProperties.LabeledBy` is set on the resolved target at the same time `Target` is assigned, preserving the DOM `for=`'s screen-reader association per the contract's suggestion.
- Resolved open question 4 (confirmed by the porting brief, not decided locally): `Attributes` splat is dropped globally across all four families in this batch; no extensibility story was built for it.
- Dropped: the `data-navius-label` marker attribute has no WPF equivalent (no arbitrary `data-*` attribute concept) and was not replicated onto any attached property.
- Theme: `Themes/Label.xaml` gives `NaviusLabel` a token-driven `Foreground`/`FontSize` and a plain `ContentPresenter` template (`RecognizesAccessKey="True"` preserved from the native `Label` default so mnemonics still work); no visual novelty beyond brand tokens, since the source has no distinct visual state machine.

## M6 audit (2026-07-09)

Adversarial parity audit of the WPF port against this doc. Default assumption: every claim FALSE until proven at file:line.

CONFIRMED (fixed): none. Every WPF implementation note was verified true.

Verified accurate (no change):

- `For` is a plain `string?` DP resolved by name lookup walking up NameScopes via `FindName` (NaviusLabel.cs `ResolveTarget` lines 62-86), retried on `Loaded` (line 39) and eagerly on the property-changed callback (line 49); covered by `LabelTests.For_ResolvesTargetByNameAndSetsLabeledBy`.
- Click-to-focus authored from scratch in the `OnMouseDown` override (line 88), with the double/triple-click text-selection guard `e.ClickCount > 1` setting `e.Handled = true` (lines 92-97), mirroring the source's `e.Detail > 1`; covered by `LabelTests.MouseDown_SingleClick_DoesNotSuppressDefault` and `MouseDown_MultiClick_SuppressesDefault_ToPreventTextSelection`.
- `AutomationProperties.SetLabeledBy(resolved, this)` is set on the resolved target alongside `Target` (line 58); asserted by the resolve test.
- Label.xaml preserves `RecognizesAccessKey="True"` (line 18), uses only DynamicResource, and its one token (Navius.Foreground) exists in both token dictionaries.

PLAUSIBLE (unfixed, not a proven defect):

- `OnMouseDown` fires for any mouse button (the guard and focus-forward are not restricted to the left button); the web's `onmousedown` is likewise button-agnostic, so this is arguably parity, but right-click focus-forwarding was not evaluated against AT expectations.
