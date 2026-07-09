# ScrollArea

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusScrollArea | `div` (position:relative container) | Root: owns `ScrollAreaContext`, tracks pointer hover for `type="hover"`, cascades context to all parts |
| NaviusScrollAreaViewport | `div` (outer, `overflow:auto` expected from consumer) wrapping an inner `div` content wrapper | The actual native-scrolling element; observes scroll metrics via the JS engine and reports them to the context |
| NaviusScrollAreaScrollbar | `div` (plain, no ARIA) | Custom scrollbar track for one axis; gated on overflow/type; cascades its `Orientation` to the Thumb |
| NaviusScrollAreaThumb | `div` (absolutely positioned, inline geometry style) | Draggable handle sized/positioned from context-derived ratios; wires pointer-drag via the JS engine |
| NaviusScrollAreaCorner | `div` | Fills the gap where vertical and horizontal scrollbars meet; renders only when both axes overflow |

## Parameters

**NaviusScrollArea**

| Name | Type | Default | Notes |
|---|---|---|---|
| Type | string | "hover" | Scrollbar visibility mode: `auto` \| `always` \| `hover` \| `scroll` |
| Orientation | string | "vertical" | Reflected as `data-orientation` only; does not gate which scrollbars render (each Scrollbar decides via its own `Orientation`) |
| ScrollHideDelay | int | 600 | Milliseconds before hiding the scrollbar after scroll stops / pointer leaves, for `type="hover"`/`type="scroll"` |
| Dir | string? | null | `ltr` \| `rtl`; falls back to cascaded `NaviusDirection`, then `ltr` |
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary<string,object>? | null | Captures unmatched attributes (e.g. `class`) onto the root div |

**NaviusScrollAreaViewport**

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | Rendered inside the inner `data-navius-scrollarea-content` wrapper |
| Attributes | IDictionary<string,object>? | null | Captured onto the outer viewport div |

Consumer must supply `overflow: auto` (or axis-specific overflow) and a bounded size on this element; no styling is applied by the component itself.

**NaviusScrollAreaScrollbar**

| Name | Type | Default | Notes |
|---|---|---|---|
| Orientation | string | "vertical" | `vertical` \| `horizontal`; cascaded to the Thumb as `ScrollAreaScrollbarOrientation` |
| ForceMount | bool | false | Keep in the DOM even when the axis has no overflow (consumer drives visibility via CSS) |
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary<string,object>? | null | |

**NaviusScrollAreaThumb**

| Name | Type | Default | Notes |
|---|---|---|---|
| Attributes | IDictionary<string,object>? | null | Orientation is read from the cascaded `ScrollAreaScrollbarOrientation` parameter, not a direct `[Parameter]` on the thumb |

**NaviusScrollAreaCorner**

| Name | Type | Default | Notes |
|---|---|---|---|
| Attributes | IDictionary<string,object>? | null | |

## Events

None of the ScrollArea parts expose `EventCallback` parameters. All interaction (hover, scroll, drag) is handled internally via C# state (`ScrollAreaContext`) and JS interop callbacks (`OnScrollMetrics`, `OnScrollActivity` on the viewport, both `[JSInvokable]`, not consumer-facing events).

## State + data attributes

**NaviusScrollArea (root div)**

- `data-navius-scrollarea`
- `data-orientation`: mirrors the `Orientation` parameter
- `data-scrolling`: present (empty string) while `ScrollAreaContext.Scrolling` is true
- `data-has-overflow-x`: present while `HasHorizontalOverflow`
- `data-has-overflow-y`: present while `HasVerticalOverflow`
- `dir`: only emitted when an explicit `Dir` or cascaded direction was supplied

**NaviusScrollAreaViewport**

- `data-navius-scrollarea-viewport` on the outer div
- `data-navius-scrollarea-content` on the inner content wrapper

**NaviusScrollAreaScrollbar**

- `data-navius-scrollarea-scrollbar`
- `data-orientation`
- `data-hovering`: present while `Context.Hovering`
- `data-scrolling`: present while `Context.Scrolling`

**NaviusScrollAreaThumb**

- `data-navius-scrollarea-thumb`
- `data-orientation`
- `data-scrolling`
- inline `style` carrying derived geometry: vertical thumb sets `height`/`top` percentages, horizontal sets `width`/`left` percentages (computed from `ThumbSizeRatio` / `ThumbOffsetRatio`)

**NaviusScrollAreaCorner**

- `data-navius-scrollarea-corner` (only rendered when `ScrollAreaContext.ShowCorner` is true, i.e. both axes overflow)

**Public state on `ScrollAreaContext`** (not attributes, but consumed by parts): `Type`, `ScrollHideDelay`, `Dir`, `Metrics` (ScrollTop/ScrollHeight/ClientHeight/ScrollLeft/ScrollWidth/ClientWidth), `ViewportElement`/`HasViewport`, `Hovering`, `Scrolling`, `HasVerticalOverflow`, `HasHorizontalOverflow`, `HasOverflow(orientation)`, `IsScrollbarVisible(orientation)`, `ThumbSizeRatio(orientation)`, `ThumbOffsetRatio(orientation)`, `ShowCorner`.

## Keyboard

No component in this family attaches its own `@onkeydown` handler. Keyboard scrolling of the viewport comes entirely from the browser's native `overflow: auto` behavior on the `NaviusScrollAreaViewport` element (arrow keys, Space/Shift+Space, Page Up/Down, Home/End when the scroll container itself has focus): nothing in the C# code intercepts or overrides these keys.

| Key | Behavior |
|---|---|
| (native overflow keys: arrows, Space, PageUp/PageDown, Home/End) | Handled entirely by the browser's native scroll container; not implemented or intercepted in C# |

## Accessibility

- No ARIA roles or attributes are applied anywhere in this family. The code comments explicitly call this out: the Scrollbar renders a plain `div` (not `role="scrollbar"`) because "the native overflow keeps the real a11y semantics, and a redundant ARIA scrollbar would be noise to AT."
- The Corner is "purely presentational: no role, no aria."
- Focus management: none of the parts manage focus; the Viewport is a normal scrollable `div` (no `tabindex` set in markup), so keyboard-scrollability depends on the browser's default focusability rules for overflow containers.

## WPF strategy

**Tier A** (derive from native WPF control).

Base on `ScrollViewer` with a custom `ControlTemplate`/`ScrollViewer.Resources` restyle, since WPF's `ScrollViewer` already provides native keyboard scrolling, wheel/touch support, and a `ScrollContentPresenter` equivalent to the Viewport/content-wrapper split. The root's hover-driven visibility (`type="hover"`/`"scroll"`) maps naturally to WPF `Trigger`s on `IsMouseOver` plus a `DispatcherTimer` mirroring `ScrollHideDelay`. Thumb geometry (size/offset ratios) is exactly what `ScrollBar`'s `Track` already computes internally, so a lookless `ScrollBar` template can likely replace the hand-rolled `ThumbSizeRatio`/`ThumbOffsetRatio` math outright rather than porting it. `AutomationPeer`: `ScrollViewerAutomationPeer`/`ScrollBarAutomationPeer` are already appropriate since no custom ARIA role was ever used here: there is no non-standard semantics to reproduce. The RTL horizontal thumb-offset normalization (`Dir == "rtl"` sign flip) will not translate 1:1; WPF's `FlowDirection` already handles RTL layout at the framework level, so the manual sign-flip logic should be dropped rather than ported.

## Open questions

- Should the WPF port keep the "corner only renders when both axes overflow" rule, or let `ScrollViewer`'s native corner (when both scrollbars are visible) handle this automatically?
- `ForceMount` on the Scrollbar (keep mounted with no overflow, consumer drives visibility via CSS) has no direct `ScrollViewer` analogue; does the WPF port need an equivalent "always in the visual tree, opacity 0" mode, or can `Visibility` binding subsume it?
- The pointer-drag thumb gesture is delegated entirely to a JS module (`dragScrollThumb`) in the Blazor version; WPF's `Thumb`/`Track` already implements drag-to-scroll natively, so is there any behavior from the JS dragger (e.g. RTL handling, clamping) that must be explicitly preserved, or does the native `Track` supersede it entirely?
- `ScrollHideDelay`'s single shared `Timer` toggles both `Scrolling` and a deferred `Hovering`-false together (`OnHideElapsedAsync`); confirm whether the WPF port needs this exact coupling or can use two independent timers/triggers.
