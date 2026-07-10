# ADR-0003: Web substrate utilities retired (WPF)

Status: accepted

## Context

Four web Navius families exist purely to compensate for gaps in the browser/Blazor substrate,
not to render a reusable visual primitive: `NaviusSlot` (attribute-splatting/`asChild`
approximation, itself already an acknowledged deviation from the spec per the web port's own
ADR-0003), `NaviusCspProvider` (Content-Security-Policy nonce cascading), `NaviusDirectionProvider`
(reading-direction cascading), and `NaviusVisuallyHidden` (the sr-only CSS clip-rect technique for
AT-visible-but-sighted-invisible content). All four render no meaningful visual, cascade a value or
merge attributes rather than compose UI, and were classified Tier C ("reinterpret or retire") by
the parity extraction (`docs/parity/{slot,csp-provider,direction-provider,visually-hidden}.md`,
"WPF strategy" sections).

WPF has a native answer to each underlying problem that makes a literal port either meaningless or
actively worse than the platform-idiomatic alternative:

- **Slot/asChild**: WPF composes via `ControlTemplate`/`ContentPresenter`/`TemplateBinding`, not
  DOM attribute splatting. There is no child-element "props object" to merge onto.
- **CspProvider**: CSP is a browser response-header/inline-script-allowlisting mechanism; WPF has
  no script/style injection model for it to police.
- **DirectionProvider**: `FrameworkElement.FlowDirection` already cascades `LeftToRight`/
  `RightToLeft` down the visual tree natively, including automatic layout/text/scrollbar
  mirroring the web version does not attempt.
- **VisuallyHidden**: WPF's `AutomationPeer`/`AutomationProperties.Name` model exposes accessible
  names independently of visual rendering; there is no DOM-text-node-vs-CSS-visibility split for a
  clip-rect trick to solve.

## Decision

Retire all four families from the WPF port. No `NaviusSlot`, `NaviusCspProvider`,
`NaviusDirectionProvider`, or `NaviusVisuallyHidden` control is added to
`Navius.Wpf.Primitives`. Each underlying need is met by an existing native WPF mechanism instead:

- Composition/`asChild`-style extension points: `ControlTemplate` + named template parts (the
  pattern every other family in this port already uses), not a generic slot component.
- CSP nonces: not applicable; no stub, no cascading value.
- Reading direction: consumers set `FlowDirection` directly on the relevant `FrameworkElement`.
- AT-only accessible names: consumers set `AutomationProperties.Name` (or override
  `AutomationPeer.GetNameCore()`) directly on the element that needs one, the same technique
  `NaviusAccessibleIcon` uses (`docs/parity/accessible-icon.md`).

## Consequences

- Consumer/porting code that expects a 1:1 `NaviusSlot`/`NaviusCspProvider`/
  `NaviusDirectionProvider`/`NaviusVisuallyHidden` type will not compile against
  `Navius.Wpf.Primitives`; this is intentional, not a missed-parity gap.
- Any future control that genuinely needs an `asChild`-style extension point designs its own
  named template parts rather than reaching for a shared generic slot primitive.
- If a future feature needs to inject or gate dynamically generated XAML/scripting under a
  locked-down environment (the nearest WPF analog to a CSP concern), that is a new, separately
  justified decision, not a revival of `NaviusCspProvider`.
