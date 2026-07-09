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

## WPF implementation notes

Shipped as `Navius.Wpf.Primitives.Controls.NaviusScrollArea` (`src/Navius.Wpf.Primitives/Controls/ScrollArea/NaviusScrollArea.cs`), Tier A: derives from the native `ScrollViewer`.

- **Part collapse.** The web's five parts (Root/Viewport/Scrollbar/Thumb/Corner) collapse onto one control plus its `ControlTemplate` (`Themes/ScrollArea.xaml`): `PART_ScrollContentPresenter` is the Viewport, `PART_VerticalScrollBar`/`PART_HorizontalScrollBar` (native `ScrollBar`, re-templated) are the Scrollbar+Thumb pair, `PART_Corner` is the Corner. Using these exact native part names means `ScrollViewer.OnApplyTemplate` wires keyboard scrolling (arrows/Page/Home/End), mouse wheel, and `ScrollViewerAutomationPeer`'s UIA `ScrollPattern` for free; no `OnApplyTemplate` override was needed in `NaviusScrollArea` itself, resolving the first open question above ("native corner" question is moot since the template supplies its own `PART_Corner`, not `ScrollViewer`'s absent built-in one).
- **Overlay, not reserved-space, scrollbars.** `PART_ScrollContentPresenter` spans both grid rows/columns; the two `ScrollBar`s occupy a fixed 10px column/row on top of it (later in element order, so painted above). Content is always full-width/height; the bars float over its trailing edge rather than shrinking it, per the one-ink brand's overlay-scrollbar rule ("thin ink thumb on transparent track, hairline on hover").
- **`Hovering`/`Scrolling` state.** Resolves the doc's last open question by *not* preserving the web's single shared timer coupling `Scrolling` and a deferred `Hovering`-false: `IsHovering` is a real-time read-only DP driven by `OnMouseEnter`/`OnMouseLeave` (no delay, matches the web root's real-time `Hovering` context field), and `IsScrolling` is a separate read-only DP driven by `OnScrollChanged`, reset to false by its own `DispatcherTimer` after `ScrollHideDelay` milliseconds (default 600, matching the web default) of scroll inactivity. The `ControlTemplate`'s two `Trigger`s (`IsHovering`, `IsScrolling`) each fade the scrollbars to `Opacity="1"`; both default to `Opacity="0"` in their own `Style`, so "fade in on hover or scroll, fade out after the delay" falls out of ordinary WPF trigger precedence without any extra state machine.
- **`Type` (auto/always/hover/scroll) not ported.** Only the `hover`/`scroll` fade behavior described above is implemented; there is no `Type` DP switching between "always visible," "never fade," etc. Native `ScrollBarVisibility` (`Auto` by default, `Disabled` for the horizontal axis) already governs *presence*; `IsHovering`/`IsScrolling` govern the overlay *fade*. Adding a `Type`-equivalent DP is deferred to a follow-up if a consumer needs it.
- **Thumb geometry.** Confirms the doc's third open question in favor of the native `Track`: no `ThumbSizeRatio`/`ThumbOffsetRatio` math was ported. The re-templated `ScrollBar`'s own `Track` computes thumb size/position from `Value`/`Maximum`/`ViewportSize`, which are `TemplateBinding`s onto `NaviusScrollArea`'s own `VerticalOffset`/`ScrollableHeight`/`ViewportHeight` (and the horizontal equivalents) - exactly the ratio math the web hand-rolled, for free.
- **Drag gesture.** Confirms the doc's second-to-last open question: the native `Thumb`/`Track` drag-to-scroll behavior supersedes the web's JS `dragScrollThumb` module entirely; nothing was ported, including RTL handling (WPF's own `FlowDirection` is the framework-level RTL mechanism per the WPF strategy note above, not a manual sign flip).
- **`ForceMount` not ported.** No WPF equivalent DP; `Visibility="{TemplateBinding Computed*ScrollBarVisibility}"` is the only presence control, resolving the doc's remaining open question in favor of not needing an "always mounted, opacity 0" mode - `Opacity` already independently gates visibility from `Visibility`, so a consumer wanting a bar always in the visual tree can already get that by not needing `ForceMount` at all.
- **No ARIA-parity gap to carry over.** The web family deliberately uses no ARIA roles anywhere (plain `div`s, relying on native `overflow: auto` semantics); the WPF port's native `ScrollViewer`/`ScrollBar` automation peers are a strict accessibility upgrade over that baseline, not a gap.
