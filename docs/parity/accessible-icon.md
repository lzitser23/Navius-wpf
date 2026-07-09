# AccessibleIcon

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusAccessibleIcon | none (renders `ChildContent` verbatim) + `<span>` | Wraps arbitrary icon content and appends a visually-hidden `<span>` carrying an accessible name for screen readers |

Per the spec's anatomy this primitive does not introduce a wrapper element: it renders the supplied `ChildContent` (typically an inline `<svg>`) unchanged, then appends the hidden label span. The `data-navius-accessible-icon` marker lives on that visually-hidden span, not on a wrapper.

## Parameters

### NaviusAccessibleIcon

| Name | Type | Default | Notes |
|---|---|---|---|
| Label | string | none (EditorRequired) | The accessible name announced by screen readers. Required, must be non-empty. |
| ChildContent | RenderFragment? | none | The icon to render (usually an inline SVG), rendered verbatim before the hidden label. |
| Attributes | IDictionary<string, object>? | none | Captures unmatched values (`CaptureUnmatchedValues = true`); forwarded to the visually-hidden label `<span>`. |

## Events

None. No `EventCallback<T>` parameters in this family.

## State + data attributes

| Attribute | Where | Notes |
|---|---|---|
| `data-navius-accessible-icon` | on the hidden `<span>` | Marker attribute identifying the label span |

No public state enums or context class; the component is stateless.

## Keyboard

No keyboard interaction implemented in this family.

## Accessibility

- No ARIA role or `aria-*` attribute is wired directly by this component.
- The hidden `<span>` renders `@Label` as its text content and is visually hidden via an inline sr-only style (`position:absolute;width:1px;height:1px;clip:rect(0 0 0 0);clip-path:inset(50%);overflow:hidden;white-space:nowrap;border:0;padding:0;margin:-1px`), so it remains in the accessibility tree and readable by screen readers while invisible on screen.
- The component's own doc comments note that consumers are responsible for marking the icon itself `aria-hidden` / `focusable="false"` if desired; this utility only guarantees an accessible name is present in the DOM alongside the icon.

## WPF strategy

Tier B (custom lookless control). There is no single native WPF control that matches "render arbitrary content plus an invisible accessible-name element." Implement as a small `ContentControl` (or plain `UserControl`) whose `AutomationProperties.Name` is bound to the `Label` parameter, and whose content is the icon (a `Path`/`Image`/vector resource). The sr-only `<span>` pattern does not translate: WPF's accessibility tree is driven by `AutomationPeer.GetName()` / `AutomationProperties.Name`, not a hidden text node, so the correct port is to set `AutomationProperties.Name="{Binding Label}"` directly on the icon-hosting element (or override `OnCreateAutomationPeer` to return a peer whose `GetNameCore()` returns Label). CSS-based visual hiding (clip/clip-path) has no WPF equivalent and is not needed once the name is supplied via automation properties instead of DOM text.

## Open questions

- Whether the WPF port should still emit a real (but `Visibility="Collapsed"` or zero-size) label element for tooling that inspects visual tree text, or rely purely on `AutomationProperties.Name`.
- Whether `Attributes`/arbitrary attribute forwarding has any WPF analog worth preserving (WPF has no free-form attribute bag equivalent to Blazor's `CaptureUnmatchedValues`).

## WPF implementation notes

Implemented as `Navius.Wpf.Primitives.Controls.NaviusAccessibleIcon` (`src/Navius.Wpf.Primitives/Controls/AccessibleIcon/`),
a lightweight `ContentControl` that applies `AutomationProperties.Name` directly to its `Content`
(when `Content` is a `DependencyObject`) and, via `NaviusAccessibleIconAutomationPeer`, excludes
itself from both the control and content views of the UIA tree when `Label` is null/empty
(`IsControlElementCore`/`IsContentElementCore` both false) -- the WPF analog of never rendering the
hidden label span. `Attributes` forwarding was dropped per `docs/adr/0003-web-substrate-utilities-retired.md`'s
reasoning (no free-form attribute bag in WPF). This is also the concrete replacement pattern for
the retired `NaviusVisuallyHidden` (docs/parity/visually-hidden.md).

## M6 audit (2026-07-09)

Adversarially re-verified `NaviusAccessibleIcon`/`NaviusAccessibleIconAutomationPeer` against this
doc's claims: `AutomationProperties.Name` propagation on content change/`Label` change, the
`GetNameCore`/`GetAutomationControlTypeCore() => Image`/`IsControlElementCore`/`IsContentElementCore`
peer behavior, and the sr-only-span-has-no-WPF-equivalent reasoning all check out against the code
and `tests/Navius.Wpf.Tests/AccessibleIconTests.cs`. No confirmed or plausible disparities found.
