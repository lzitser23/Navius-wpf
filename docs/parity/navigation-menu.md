# NavigationMenu

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusNavigationMenu | `<nav role="navigation">` | Root: owns the authoritative open value (controlled/uncontrolled), orientation, hover delay/closeDelay, cascades `NavigationMenuContext` |
| NaviusNavigationMenuList | `<ul>` (no `role=menubar`) | Top-level trigger/link row; hosts the roving-focus controller across triggers and top-level links |
| NaviusNavigationMenuItem | `<li>` | One menu item; cascades itself and its `Value` to Trigger/Content beneath it |
| NaviusNavigationMenuTrigger | `<button>` | Disclosure button for an item's content; owns aria-expanded/aria-controls, hover/focus/keyboard open, roving tabindex seat registration |
| NaviusNavigationMenuIcon | `<span aria-hidden="true">` | Presentational chevron slot inside a Trigger; mirrors the item's open state |
| NaviusNavigationMenuLink | `<a>` | Navigational anchor, usable as a top-level item (in place of Trigger+Content) or inside a Content panel |
| NaviusNavigationMenuPortal | none (flag-setter) | Records custom mount container + KeepMounted into the context; actual DOM teleport done by the Popup |
| NaviusNavigationMenuPositioner | none (flag-setter, renders ChildContent) | Collects placement options (side/align/offsets/collision) and publishes them into the context |
| NaviusNavigationMenuPopup | `<div>` positioner wrapper + `<div>` popup surface | The navigation-menu surface (Base UI's Popup); standalone (per-item) or shared (root-level, viewport) layout; owns presence, positioning, dismissable layer |
| NaviusNavigationMenuArrow | `<svg>` | Marker inside the Popup that the positioner aligns to point at the active trigger |
| NaviusNavigationMenuViewport | `<div>` + inner slot `<div>` | Shared resizing content container; Content panels teleport into its slot in shared-viewport layout |
| NaviusNavigationMenuContent | `<div tabindex="-1">` | The panel disclosed by a trigger; carries discrete panel attributes, teleports into the Viewport in shared mode |
| NaviusNavigationMenuBackdrop | `<div>` | Presentational scrim behind the popup, open whenever any item is active |
| NaviusNavigationMenuSub | `<div>` (NOT `<nav>`) | Nested submenu root placed inside a Content panel; owns its own open value/orientation and cascades a fresh `NavigationMenuContext` |

## Parameters

**NaviusNavigationMenu**

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | string? | null | Controlled open value (value of open item); pair with ValueChanged |
| ValueChanged | EventCallback\<string?\> | - | Presence makes the component controlled |
| DefaultValue | string? | null | Initial open value for uncontrolled use |
| Orientation | string | "horizontal" | "horizontal" or "vertical"; drives arrow-key axis and data-orientation |
| Delay | int | 50 | ms before hover opens an item's content (Base UI `delay`) |
| CloseDelay | int | 50 | ms before leaving the menu closes open content (Base UI `closeDelay`) |
| OnOpenChangeComplete | EventCallback\<bool\> | - | Fires after popup enter/exit presence settles |
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary\<string,object\>? | null | Captured unmatched attributes (e.g. class) |

**NaviusNavigationMenuList** (inherits `NavigationMenuPart`, no own [Parameter]s beyond own listed here)

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary\<string,object\>? | null | |

**NaviusNavigationMenuItem**

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | string | "" | Matched against the root's open value |
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary\<string,object\>? | null | |

**NaviusNavigationMenuTrigger** (inherits `NavigationMenuPart`)

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Disabled | bool | false | |
| NativeButton | bool | true | Render-contract parity with Base UI's `nativeButton` |
| Attributes | IDictionary\<string,object\>? | null | |

**NaviusNavigationMenuIcon** (inherits `NavigationMenuPart`)

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary\<string,object\>? | null | |

**NaviusNavigationMenuLink**

| Name | Type | Default | Notes |
|---|---|---|---|
| Href | string? | null | Link target |
| Active | bool | false | Marks current page: sets data-active + aria-current="page" |
| OnSelect | EventCallback\<NaviusSelectEventArgs\> | - | Cancelable; on activation |
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary\<string,object\>? | null | |

**NaviusNavigationMenuPortal**

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Container | string? | null | CSS selector of custom mount container; null = document.body |
| KeepMounted | bool | false | Keep the popup mounted while closed (exit animations) |

**NaviusNavigationMenuPositioner** (all [Parameter]s inherited from `OverlayPositionerBase`)

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | inherited |
| Side | string? | null → "bottom" (DefaultSide override) | inherited; bottom/top/left/right |
| Align | string? | null → "center" (DefaultAlign override) | inherited; start/center/end |
| SideOffset | double | 0 | inherited |
| AlignOffset | double | 0 | inherited |
| Flip | bool | true | inherited; folded into AvoidCollisions |
| AvoidCollisions | bool | true | inherited |
| CollisionPadding | double? | null | inherited |
| Sticky | string? | null | inherited; "partial"/"always" |
| HideWhenDetached | bool | false | inherited; drives data-anchor-hidden |
| ArrowPadding | double | 0 | inherited |
| Attributes | IDictionary\<string,object\>? | null | inherited |

**NaviusNavigationMenuPopup** (inherits `OverlayPopupBase` → `OverlayPresence`)

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | own |
| KeepMounted | bool | false | inherited from OverlayPopupBase |
| OnOpenAutoFocus | EventCallback\<NaviusOpenAutoFocusEventArgs\> | - | inherited; cancelable |
| OnCloseAutoFocus | EventCallback\<NaviusCloseAutoFocusEventArgs\> | - | inherited; cancelable |
| OnEscapeKeyDown | EventCallback\<NaviusEscapeKeyDownEventArgs\> | - | inherited; cancelable |
| OnPointerDownOutside | EventCallback\<NaviusPointerDownOutsideEventArgs\> | - | inherited; cancelable |
| OnFocusOutside | EventCallback\<NaviusFocusOutsideEventArgs\> | - | inherited; cancelable |
| OnInteractOutside | EventCallback\<NaviusInteractOutsideEventArgs\> | - | inherited; cancelable |
| Attributes | IDictionary\<string,object\>? | null | inherited |

Notes: Popup overrides `TrapFocus` → false, `MoveFocusInside` → false, `ShouldStayMounted` → `KeepMounted || Context.PortalKeepMounted` (all computed, not [Parameter]s).

**NaviusNavigationMenuArrow**

| Name | Type | Default | Notes |
|---|---|---|---|
| Width | double | 10 | |
| Height | double | 5 | |
| ChildContent | RenderFragment? | null | Default renders a downward triangle polygon |
| Attributes | IDictionary\<string,object\>? | null | |

**NaviusNavigationMenuViewport** (inherits `NavigationMenuPart`)

| Name | Type | Default | Notes |
|---|---|---|---|
| ForceMount | bool | false | Accepted for prop parity; container is always present regardless |
| Attributes | IDictionary\<string,object\>? | null | |

**NaviusNavigationMenuContent**

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| ForceMount | bool | false | Keep panel mounted while closed (data-closed) for exit animation |
| Attributes | IDictionary\<string,object\>? | null | |

**NaviusNavigationMenuBackdrop** (inherits `OverlayPresence`, no other [Parameter]s from base)

| Name | Type | Default | Notes |
|---|---|---|---|
| KeepMounted | bool | false | Keep backdrop mounted while closed |
| Attributes | IDictionary\<string,object\>? | null | |

**NaviusNavigationMenuSub**

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | string? | null | Controlled open value of the sub-item |
| ValueChanged | EventCallback\<string?\> | - | |
| DefaultValue | string? | null | Initial open value for uncontrolled use |
| Orientation | string | "vertical" | Sub menus default to vertical (root defaults horizontal) |
| Delay | int | 50 | Hover-open delay |
| CloseDelay | int | 50 | Hover-close delay |
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary\<string,object\>? | null | |

Total: 14 parts, 68 parameters across all parts (including inherited).

## Events

| Part | Event | Signature | Notes |
|---|---|---|---|
| NaviusNavigationMenu | ValueChanged | EventCallback\<string?\> | Controlled-mode value sync |
| NaviusNavigationMenu | OnOpenChangeComplete | EventCallback\<bool\> | Fires when the shared popup's presence transition settles |
| NaviusNavigationMenuLink | OnSelect | EventCallback\<NaviusSelectEventArgs\> | Cancelable; PreventDefault keeps the disclosure open after selection |
| NaviusNavigationMenuPopup | OnOpenAutoFocus | EventCallback\<NaviusOpenAutoFocusEventArgs\> | Inherited; cancelable, fires before focus moves in |
| NaviusNavigationMenuPopup | OnCloseAutoFocus | EventCallback\<NaviusCloseAutoFocusEventArgs\> | Inherited; cancelable, skips returning focus to trigger |
| NaviusNavigationMenuPopup | OnEscapeKeyDown | EventCallback\<NaviusEscapeKeyDownEventArgs\> | Inherited; cancelable |
| NaviusNavigationMenuPopup | OnPointerDownOutside | EventCallback\<NaviusPointerDownOutsideEventArgs\> | Inherited; cancelable |
| NaviusNavigationMenuPopup | OnFocusOutside | EventCallback\<NaviusFocusOutsideEventArgs\> | Inherited; cancelable |
| NaviusNavigationMenuPopup | OnInteractOutside | EventCallback\<NaviusInteractOutsideEventArgs\> | Inherited; cancelable |
| NaviusNavigationMenuSub | ValueChanged | EventCallback\<string?\> | Controlled-mode value sync for the submenu |

## State + data attributes

Public state (`NavigationMenuContext`):
- `Value` (string?): the value of the currently open item, null = nothing open
- `Open` (bool): `Value is not null`
- `Orientation`, `IsRoot`, `PreviousValue`, `ActivationDirection` (left/right/up/down, derived from trigger document order + orientation)
- `HasViewport` (bool): true once either the root Popup or a Viewport part registers (shared-viewport layout)
- `IsOpen(value)`, `IsTabStop(value)`, `TriggerIndex(value)`, trigger registry (`RegisterTrigger`/`TryGetTrigger`)

Data attributes emitted (Base UI discrete contract: no `data-state` anywhere):

| Attribute | Where | Meaning |
|---|---|---|
| data-navius-navigationmenu | Root `<nav>` | Marker |
| data-orientation | Root, List, Item, Trigger, Content, Popup, Sub, Viewport | Mirrors Orientation |
| data-navius-navigationmenu-list / -item / -trigger / -link / -icon / -popup / -positioner / -arrow / -viewport / -viewport-slot / -content / -backdrop / -sub | respective element | Structural markers |
| data-popup-open | List, Trigger, Icon | Present (empty string) while the associated item is open |
| data-pressed | Trigger | Present while pointer-pressed |
| data-disabled | Trigger | Present when Disabled |
| data-navius-navigationmenu-rovable | Trigger, top-level Link | Marks elements the roving-focus engine cycles across |
| data-active | Link | Present when Active=true |
| aria-current | Link | "page" when Active=true |
| data-open / data-closed | Backdrop, Popup, Content | Discrete presence state (mutually exclusive) |
| data-starting-style / data-ending-style | Backdrop, Popup, Content | One-frame enter marker / exit-in-progress marker |
| data-activation-direction | Popup, Content | left/right/up/down; direction the active item moved (drives directional enter animation) |
| data-side / data-align / data-anchor-hidden | Popup (mirrored by the positioning engine, not C#) | Resolved placement relative to the anchor |

CSS custom properties (Viewport, mirrored by JS interop): `--navius-navigation-menu-viewport-width/height` (also `--navius-navigationmenu-viewport-*`).

## Keyboard

| Key | Behavior |
|---|---|
| Tab | The trigger row is a single roving-tabindex composite seat: only the open item's trigger (or the first trigger if none open) is in the Tab order; Tab moves in/out of the row |
| ArrowLeft/ArrowRight (horizontal) or ArrowUp/ArrowDown (vertical) | Roving-focus engine moves focus across `[data-navius-navigationmenu-rovable]:not([data-disabled])` (triggers + top-level links) in the List |
| Home / End | Roving-focus engine moves focus to the first/last rovable trigger or link in the List |
| Space / Enter (on Trigger) | Toggle this item's content (`RequestToggleAsync`) |
| ArrowDown (on Trigger, orientation=horizontal) | "Enter content": open the item and focus the first focusable child of its panel |
| ArrowRight (on Trigger, orientation=vertical) | "Enter content": open the item and focus the first focusable child of its panel |
| Escape | Dismissable layer on the Popup closes it; if focus was inside the popup and is still restorable, focus returns to the trigger |

Content itself adds no keydown handling beyond pointer-enter/leave (used for hover-intent bookkeeping, not keyboard).

## Accessibility

- Root renders `<nav role="navigation">`: explicitly NOT `role=menu`/`role=menubar`; this is a site-navigation landmark, not a menu widget.
- List renders a plain `<ul>` (no `role=menubar`); Item renders a plain `<li>`.
- Trigger is a native `<button>` with `aria-expanded` ("true"/"false") and `aria-controls` pointing at the popup id it owns (the shared Popup's ContentId in viewport mode, else its own standalone Popup id); `aria-controls` is null while closed. Trigger `id` is wired for the Popup's `aria-labelledby`.
- Popup carries `aria-labelledby` referencing the active trigger's id; it has no ARIA role of its own (matches the "site navigation, not a menu" stance) and no scroll-lock/focus-trap (`Modal` is always false).
- Link sets `aria-current="page"` when `Active=true`; native `<a>` gives native focus/activation.
- Icon is `aria-hidden="true"` (presentational chevron).
- Focus management: hover-open leaves focus on the trigger (`MoveFocusInside=false` on the Popup); a keyboard ("enter content") open moves focus to the first focusable descendant of the panel (APG pattern), implemented via `Context.ConsumeKeyboardOpen`/`FocusPanelRequested` to also cover the already-open case. On close/dismiss, if focus is still inside the popup and restorable, focus returns to the trigger that owned it (`Context.TryGetTrigger` + `FocusAsync`).
- No focus trap (`TrapFocus=false`): the roving controller lives on the trigger row (List), not inside the Popup.

## WPF strategy

Tier B (custom lookless control). There is no native WPF control that models a non-modal, hover/keyboard-disclosed site-navigation row with a morphing shared popup; base off `Control`/`ItemsControl` with a `Popup`-hosted panel rather than `Menu`/`MenuItem` (whose `role=menu` semantics and UIA `MenuItem` pattern are the wrong contract here). Map the root `nav` to `AutomationControlType.Custom` (no UIA landmark-navigation equivalent) or `Group`; map the Trigger to a `ButtonAutomationPeer`-derived peer exposing `IExpandCollapseProvider` driven by `aria-expanded`; the Popup content can use a `Pane`/`Group` peer with `LabeledBy` pointing at the Trigger peer. Several mechanisms will not translate directly and need WPF-native replacements: the Floating-UI positioning/arrow engine (→ `Popup.CustomPopupPlacementCallback` + a hand-rolled collision/flip routine), DOM teleportation of Content into a shared Viewport slot (→ real visual-tree reparenting or swap-the-content-of-one-fixed-panel instead of "moving" elements), ResizeObserver-driven viewport CSS vars (→ `SizeChanged` handlers), and the JS `RovingFocus`/`DismissableLayer` interop (→ WPF `KeyboardNavigation` + manual arrow-key handling + `Popup.StaysOpen=false`/mouse-capture for outside-dismiss).

## Open questions

- Shared-viewport teleportation (one popup morphing between items) has no direct WPF analog: decide whether the port keeps a single shared `Popup` that swaps its content (recommended) or falls back to one Popup per item (loses the "single resizing surface" effect and the morph/re-anchor animation).
- The Floating-UI-based positioner (used identically by every overlay family) should probably become one shared WPF positioning helper: confirm whether that helper is being built centrally (Overlays parity doc) rather than per-family.
- Hover intent uses cancelable `Task.Delay` via `CancellationTokenSource`; a `DispatcherTimer`-based port is the obvious analog: confirm no additional UI-thread reentrancy concerns for the shared/moving-anchor case (switching instantly when already open).
- `data-activation-direction` drives a directional enter animation; decide whether the WPF port needs an equivalent directional transition for v1 or can defer it.
- The Popup carries no ARIA role by design (site-nav pattern): confirm the intended UIA `AutomationControlType` for the ported Popup panel (Custom vs Group vs Pane) so parity docs across families stay consistent.
- `NaviusNavigationMenuSub` reuses the exact same context/parts recursively (nested submenu-in-a-panel): confirm whether nested submenus are in scope for the WPF port's first pass or deferred.
- The Base UI discrete `data-*` state contract (`data-open`/`data-closed`/`data-starting-style`/`data-ending-style`/`data-popup-open`/`data-pressed`/`data-disabled`) needs a WPF equivalent: dependency properties + `VisualStateManager` states, or attached properties mirroring the `data-*` names for template triggers?

## WPF implementation notes

Implemented in `src/Navius.Wpf.Primitives/Controls/NavigationMenu/` (+ `Themes/NavigationMenu.xaml`, gallery `Pages/NavigationMenuPage.xaml(.cs)`, tests `tests/Navius.Wpf.Tests/NavigationMenuTests.cs`, 15 tests). Tier B custom lookless controls, per the strategy above; no native control derivation beyond `Button`-for-Trigger/Link.

### Scope: per-item popup mode only; shared viewport is a stub

This M2 port implements only the per-item popup mode: each open `NaviusNavigationMenuContent` owns its own standalone `NaviusAnchoredPopup` (Positioning/ + Controls/NaviusAnchoredPopup.cs, composed as-is, never edited), anchored to its own Trigger. `NaviusNavigationMenu.UseSharedViewport` (default `false`) throws `NotSupportedException` immediately if set `true`, with a message pointing back to this section, rather than silently falling back or faking the morph animation. The shared/morphing viewport (one popup that resizes and re-anchors as the active item changes) is real, non-trivial WPF work (`Popup.CustomPopupPlacementCallback` + swap-the-content-of-one-fixed-panel + a directional transition) and is left as an explicit M3+ follow-up; see the gallery page's "Try UseSharedViewport (throws)" button for a live demo of the failure.

### Type collapse: Popup + Positioner + Portal fold into Content

Unlike Menubar (which leans on native `Menu`/`MenuItem`), NavigationMenu has no native substrate to lean on, but the contract's `NaviusNavigationMenuPopup`/`Positioner`/`Portal` still collapse into one type: `NaviusNavigationMenuContent`'s default `ControlTemplate` (Themes/NavigationMenu.xaml) hosts a `NaviusAnchoredPopup` directly, and `Side`/`Align`/`SideOffset`/`AlignOffset` are folded onto `Content` itself as the only positioning knobs the substrate actually exposes (default `Side=Bottom`, `Align=Center`, matching the contract's Positioner defaults for this family specifically, which differ from Menubar's `Align=Start`). **Not implemented** (the substrate has no equivalent): `Flip`/`AvoidCollisions` as independent toggles (the substrate always flips+shifts), `CollisionPadding`, `Sticky`, `HideWhenDetached`, `ArrowPadding`, `Container` (moot: WPF's `Popup` already renders outside the normal visual flow, no DOM-teleport concept needed). `NaviusNavigationMenuViewport` is a present-but-inert stub (only meaningful in shared-viewport mode).

### Full part list mapped

| Web part | WPF type | Notes |
|---|---|---|
| `NaviusNavigationMenu` | `NaviusNavigationMenu : NavigationMenuHostBase` | Root; `UseSharedViewport` stub (see above) |
| `NaviusNavigationMenuSub` | `NaviusNavigationMenuSub : NavigationMenuHostBase` | Shares all host logic with the root via a common abstract base; establishes its own ambient scope for its subtree (DP-inheritance shadowing) |
| `NaviusNavigationMenuList` | `NaviusNavigationMenuList : ItemsControl` | Roving-focus controller (own `PreviewKeyDown` handler, same pattern as `NaviusRadioGroup` elsewhere in this repo, since WPF has no aria-orientation-aware composite-widget primitive to reuse); swaps its own `ItemsPanel` horizontal/vertical to match the ambient host's `Orientation` |
| `NaviusNavigationMenuItem` | `NaviusNavigationMenuItem : ContentControl` | Cascades itself via an inherited attached `Owner` property (WPF's analog to Blazor `CascadingValue`) |
| `NaviusNavigationMenuTrigger` | `NaviusNavigationMenuTrigger : Button` | Hover-intent Delay/CloseDelay via `DispatcherTimer`; APG "enter content" keyboard pattern; `AutomationPeer` implements `IExpandCollapseProvider` (per the strategy note above) |
| `NaviusNavigationMenuIcon` | `NaviusNavigationMenuIcon : ContentControl` | Read-only `IsOpen` DP mirrors the owning item's open state for a template trigger (rotate chevron) |
| `NaviusNavigationMenuLink` | `NaviusNavigationMenuLink : Button` | `Href` informational only (no navigation side effect authored here); `Active` sets `AutomationProperties.ItemStatus="page"` |
| `NaviusNavigationMenuPortal` + `Positioner` + `Popup` | folded into `NaviusNavigationMenuContent` | See "Type collapse" above |
| `NaviusNavigationMenuArrow` | `NaviusNavigationMenuArrow : Shape` | Reuses `FrameworkElement.Width`/`Height` (metadata-overridden to 10x5) rather than redundant new DPs |
| `NaviusNavigationMenuViewport` | `NaviusNavigationMenuViewport : ContentControl` | Inert stub, see above |
| `NaviusNavigationMenuContent` | `NaviusNavigationMenuContent : ContentControl` | See "Type collapse" above; `ForceMount` accepted but has no exit-animation phase to preserve in this port |
| `NaviusNavigationMenuBackdrop` | `NaviusNavigationMenuBackdrop : OverlayBackdrop` | Reuses `Overlays/OverlayBackdrop.cs` (composed via inheritance, never edited) but re-registers its own `DefaultStyleKey`/`Style` (see below) |

`Attributes` is not ported anywhere in this family, same rationale as Menubar.

### Dismissal (outside-press / Escape) is window-hook-based, with a known nested-popup gap

`NavigationMenuHostBase` hooks `Window.PreviewMouseDown`/`PreviewKeyDown` (mirrors `Overlays/OverlayStack.cs`'s pattern, but is not that shared service) to close on an outside press or Escape. Because `NaviusAnchoredPopup`-backed Content panels render through a separate, transparent child HWND (`AllowsTransparency=true`), Win32 input for a press *inside* an open Content panel never reaches the main Window's routed-event tree at all, so the window-level hook only ever needs to ask "was this press inside my own trigger List" to correctly identify a genuine outside press. The known gap: a click landing inside *another* host's popup (e.g. clicking inside a root Content panel while a nested `NaviusNavigationMenuSub` inside a *different* item's panel is open) is invisible to both hooks and won't auto-close either one. Escape has the mirror-image gap: key-tunneling from the Window does not cross into a popup's own `PresentationSource`, so `NaviusNavigationMenuContent` additionally attaches its own local `PreviewKeyDown` handler so Escape still closes correctly while focus is inside that specific popup.

### `NaviusNavigationMenuBackdrop` needs its own `DefaultStyleKey`

Discovered via the test suite: a subclass that does not call its own `DefaultStyleKeyProperty.OverrideMetadata` does **not** reliably resolve the base type's implicit style through a plain `element.Resources` scope (confirmed experimentally: an un-parented `OverlayBackdrop` resolves its style from a themed scope; an un-parented `NaviusNavigationMenuBackdrop` with the identical scope does not, despite nominally inheriting the same `DefaultStyleKey` metadata). `NaviusNavigationMenuBackdrop` now registers its own `DefaultStyleKeyProperty` override and Themes/NavigationMenu.xaml carries a matching `Style` (visually identical to `OverlayBackdrop`'s). Worth flagging for the orchestrator: any other family reusing an existing control purely via inheritance (without adding new visual state) should expect the same gotcha.

### Deferred per the Open Questions above

`data-activation-direction` (directional enter animation) is not implemented; `OnOpenChangeComplete` fires synchronously right after `Value` changes (no separate presence-transition phase exists in this port to wait for). The Popup's `AutomationControlType` is left as the default `FrameworkElementAutomationPeer` inference rather than an explicit Custom/Group/Pane choice, pending the cross-family consistency decision the Open Questions call for.

## M6 audit (2026-07-09)

Adversarial parity re-audit of the NavigationMenu family against the C#/XAML. Hover-intent timing is real: `NavigationMenuHostBase.Delay`/`CloseDelay` default `50` ms and drive `DispatcherTimer`s in `NaviusNavigationMenuTrigger` (open on enter, close on leave, cancel-on-reenter via `CancelPendingClose`), matching the contract's `delay`/`closeDelay=50`. The "enter content" keys are wired exactly (`ArrowDown` horizontal / `ArrowRight` vertical in `OnKeyDown`); the List roving controller cycles rovable triggers/links on Arrow/Home/End with the correct per-orientation axis; the Trigger's `ButtonAutomationPeer` genuinely implements `IExpandCollapseProvider` reflecting and driving `host.Value` (test-exercised). The `NaviusNavigationMenuArrow` default size is metadata-overridden to 10x5 and its geometry is computed from `Width`/`Height` (not hardcoded); there is no active-trigger-tracking "indicator" to mis-position because shared-viewport mode is an explicit stub (`UseSharedViewport=true` throws `NotSupportedException`, test-verified). `Themes/NavigationMenu.xaml` is `DynamicResource`-only with all keys resolving.

### CONFIRMED (fixed)

- **Doc test count wrong.** Notes claimed `NavigationMenuTests.cs` has "13 tests"; it actually has **15** `[StaFact]` methods. Corrected to 15.

### PLAUSIBLE (not fixed)

- Dismissal nested-popup gap: a press inside one host's open Content panel while a *different* host's (or nested `NaviusNavigationMenuSub`'s) popup is open is invisible to both window-level hooks and won't auto-close the other. Already documented as a known gap in the WPF notes; traces into `NaviusAnchoredPopup`'s separate transparent child-HWND rendering (forbidden shared infra), so reported rather than fixed.
- `NaviusNavigationMenuLink.Active` maps `aria-current="page"` onto `AutomationProperties.ItemStatus="page"`. Defensible (no exact UIA aria-current equivalent) but an approximation; noted for cross-family consistency review.
