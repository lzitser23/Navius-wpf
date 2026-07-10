# VisuallyHidden

Single-file component: only `NaviusVisuallyHidden`.

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusVisuallyHidden | `<span data-navius-visually-hidden>` | Hides content visually while keeping it in the accessibility tree, via the spec's exact "sr-only" CSS style block |

## Parameters

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` (CaptureUnmatchedValues) | null | A consumer-supplied `style` value is appended after the base sr-only style (extends, cannot override it away); all other attributes forward unchanged |

## Events

None.

## State + data attributes

| Element | Attribute | Meaning |
|---|---|---|
| Root | `data-navius-visually-hidden` | Marker |
| Root | inline `style` | Fixed base: `position:absolute;border:0;width:1px;height:1px;padding:0;margin:-1px;overflow:hidden;clip:rect(0 0 0 0);white-space:nowrap;word-wrap:normal`. Any consumer-supplied `style` from `Attributes` is appended after it (`{SrOnly};{extra}`), so later declarations can add to but not remove the hiding behavior for properties not touched by the base rule |

No `role` or ARIA attributes are set by this component itself; it is a pure CSS visibility trick that keeps the element in the accessibility tree/DOM (unlike `display:none` or `hidden`, which remove it) while making it visually invisible.

## Keyboard

None. This is a styling-only wrapper with no interaction of its own.

## Accessibility

- The entire purpose of the component: content remains present for assistive technology (screen readers) while being visually hidden from sighted users, via the standard "clip-to-1px" CSS technique rather than `display:none`/`visibility:hidden`/`aria-hidden`, all of which would remove it from the accessibility tree.
- No ARIA role or attributes are added; whatever semantics the child content already has (e.g. a label, a live region) are preserved as-is.

## WPF strategy

Tier C: reinterpret. WPF has no CSS/DOM-visibility concept to replicate; visually-hidden-but-AT-visible content is not a well-defined idea in the WPF/UIA model the way it is in the web accessibility tree, since UIA exposes `AutomationPeer`s independent of visual rendering in the first place. The nearest WPF equivalent for "present to a screen reader, not to sighted users" is typically achieved via `AutomationProperties.Name`/`AutomationProperties.HelpText` set directly on a control (no separate hidden element needed), or by giving an element `Opacity="0"`/zero size while still being part of the visual tree (which does keep it in the UIA tree, unlike `Visibility.Collapsed`, which removes it from both). A literal `NaviusVisuallyHidden`-as-`Control` port (a lookless control rendering a template that keeps content zero-sized/clipped) is possible for API-shape parity, but the underlying accessibility problem it solves in HTML (native AT reads DOM text regardless of CSS visibility unless explicitly hidden) does not have a matching problem in WPF, where `AutomationProperties.Name` is the idiomatic way to supply AT-only text.

## Open questions

- Is a literal `NaviusVisuallyHidden` WPF control needed at all, or should each consuming WPF control just set `AutomationProperties.Name`/`AutomationProperties.LabeledBy` directly instead of wrapping visually-hidden text nodes (the pattern this component exists to support in Blazor, e.g. accessible-but-unstyled labels)?
- If a control is built for structural/API parity (e.g. because ported markup literally contains `<NaviusVisuallyHidden>` wrappers), confirm `Opacity="0"` + zero `Width`/`Height` (which stays in the UIA tree) is the right primitive versus `Visibility.Hidden` (which does NOT get pruned from the visual tree layout-wise but WPF's automation framework treatment differs from web `visibility:hidden`); this needs verification against actual screen-reader (Narrator/NVDA+UIA) behavior before committing to an approach.

## WPF implementation notes

Retired; see docs/adr/0003-web-substrate-utilities-retired.md. `NaviusAccessibleIcon`
(docs/parity/accessible-icon.md) is the concrete example of the replacement pattern: it sets
`AutomationProperties.Name` directly rather than wrapping a visually-hidden text node.

## M6 audit (2026-07-09)

ADR-only check, verified TRUE on all three points:

- Retirement is real: no `NaviusVisuallyHidden.cs` and no `src/Navius.Wpf.Primitives/Controls/VisuallyHidden/` exist (globbed). The only `VisuallyHidden` artifact in the tree is the web Blazor `src/Navius.Primitives/Components/VisuallyHidden/NaviusVisuallyHidden.razor`, i.e. the substrate this ADR retires, not a WPF control.
- The ADR's reasoning holds: `docs/adr/0003-web-substrate-utilities-retired.md` correctly names `AutomationProperties.Name` (or `AutomationPeer.GetNameCore()`) as the WPF-idiomatic replacement, since UIA exposes accessible names independently of visual rendering and there is no DOM-text-vs-CSS-visibility split for a clip-rect trick to solve.
- Doc-completeness note: the task premise (this section empty, a bare header) was stale. The "WPF implementation notes" section already carried the same `Retired; see docs/adr/0003-...` pointer as `slot.md` (plus the concrete `NaviusAccessibleIcon` example), so it was already consistent and complete; no content backfill was required.
