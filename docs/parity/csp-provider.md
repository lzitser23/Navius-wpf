# CspProvider

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusCspProvider | none (DOM-transparent; renders two nested `<CascadingValue>` wrappers around `ChildContent`) | Cascades a Content-Security-Policy nonce (and a "disable style elements" flag) to descendants. Renders no visible element itself, mirroring `NaviusDirectionProvider`. Per the code's own comment: Navius itself injects no `<style>`/`<script>` elements (the engine sets inline element styles and uses no eval/new Function), so under a strict CSP Navius is clean with no nonce required; this component exists for API parity and for consumers/future features that do inject styled elements. |

## Parameters

### NaviusCspProvider
| Name | Type | Default | Notes |
|---|---|---|---|
| Nonce | string? | none | Nonce to apply to any injected style/script elements; cascaded to descendants under the cascading value name `NaviusCspNonce`. |
| DisableStyleElements | bool | `false` (implicit) | When true, descendants should avoid injecting `<style>` elements (CSP without `'unsafe-inline'`); cascaded under `NaviusCspDisableStyleElements`. |
| ChildContent | RenderFragment? | none | |

## Events

None. No `EventCallback<T>` parameters are declared on this component.

## State + data attributes

No `data-*` attributes are rendered (the component renders no DOM element). No public state enums. The component exposes two named cascading values to descendants: `NaviusCspNonce` (string?) and `NaviusCspDisableStyleElements` (bool).

## Keyboard

No keyboard interaction implemented in this family.

## Accessibility

No ARIA roles or aria-* attributes; the component renders no DOM element and performs no focus management. It is a pure state-cascading provider.

## WPF strategy

Tier C (reinterpret or retire)

Content-Security-Policy nonces are a browser/HTML mechanism for allow-listing inline `<style>`/`<script>` elements against a CSP header; WPF has no script/style injection model and no CSP enforcement layer, so there is no WPF equivalent to translate to and no `AutomationPeer` mapping applies (nothing is rendered, and there would be nothing to make accessible). The component should be dropped from the WPF port, or at most kept as an inert marker/no-op cascading value for source-level API parity if consumer code references `NaviusCspProvider` conditionally across platforms.

## Open questions

- Does any WPF-side consumer code actually branch on `NaviusCspNonce`/`NaviusCspDisableStyleElements` (e.g., a shared component library targeting both Blazor and WPF), which would justify keeping a no-op stub for compile-time parity rather than dropping the component entirely?
- If Navius WPF ever needs a comparable "the app requests we avoid a mechanism" signal (e.g., avoiding dynamic `XamlReader`/`BindingExpression` reflection in locked-down environments), is that a genuinely analogous concept worth modeling, or is this family simply inapplicable outside the browser?

## WPF implementation notes

Retired; see docs/adr/0003-web-substrate-utilities-retired.md.
