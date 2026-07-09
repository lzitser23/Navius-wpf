# Dialog

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusDialog | none (renders `@ChildContent` inside a `CascadingValue`) | Root. Owns open state (controlled via `@bind-Open` or uncontrolled via `DefaultOpen`) and cascades `DialogContext` to parts. |
| NaviusDialogTrigger | `<button type="button">` | Opens/toggles the dialog. Captures itself as `Context.TriggerElement`. |
| NaviusDialogPortal | none (renders `@ChildContent`; flag-setter only) | Optional wrapper that pushes `Container` / `ForceMount` into `DialogContext` so Overlay/Content honour them. The actual DOM teleport happens per-part via `NaviusPortal`. |
| NaviusDialogBackdrop | `<div>` (inside `NaviusPortal`) | Presentational scrim, portaled as a sibling of the popup. No click handler (outside dismissal is owned by the popup's dismissable layer). |
| NaviusDialogPopup | `<div role="dialog">` (inside `NaviusPortal`) | The dialog panel. Portaled to the body; engages dismissable layer, focus trap (modal), and scroll lock (modal) on open. |
| NaviusDialogTitle | `<h2>` | Labels the dialog; `id` is read by aria-labelledby on the popup. Registers presence via `Context.RegisterTitle()`. |
| NaviusDialogDescription | `<p>` | Describes the dialog; `id` is read by aria-describedby on the popup. Registers presence via `Context.RegisterDescription()`. |
| NaviusDialogClose | `<button type="button">` | Any button inside the popup that requests close. |

## Parameters

### NaviusDialog

| Name | Type | Default | Notes |
|---|---|---|---|
| Open | bool | false | Controlled open state; use with `OpenChanged` (`@bind-Open`). |
| OpenChanged | EventCallback\<bool\> | none | Presence of a delegate makes the component "controlled" (`IsControlled`). |
| DefaultOpen | bool | false | Initial open state when used uncontrolled. |
| Modal | bool | true | Mirrors the spec `modal`. When false, no focus trap, no scroll lock, outside content stays interactive. |
| ChildContent | RenderFragment? | null | Child parts. |

### NaviusDialogTrigger

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | Button content. |
| Attributes | IDictionary\<string, object\>? | null | Captured unmatched attributes (`CaptureUnmatchedValues`), forwarded to the `<button>`. |

### NaviusDialogPortal

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | Wrapped Overlay/Content. |
| Container | string? | null | CSS selector for the portal mount target; null = `document.body`. |
| ForceMount | bool | false | Force-mounts both Overlay and Content while closed (exit transitions driven by `data-closed` instead of unmounting). |

### NaviusDialogBackdrop

| Name | Type | Default | Notes |
|---|---|---|---|
| KeepMounted | bool | false | Keeps the backdrop mounted while closed (for exit animations). |
| Attributes | IDictionary\<string, object\>? | null | Captured unmatched attributes, forwarded to the `<div>`. |

Also inherits from `OverlayPresence` (base class, no additional `[Parameter]`s beyond what's declared on the razor above; `OverlayPresence` itself declares none).

### NaviusDialogPopup

Declared directly on the popup:

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | Popup content. |

Inherited from `OverlayPopupBase` (applies to NaviusDialogPopup since it `@inherits OverlayPopupBase`):

| Name | Type | Default | Notes |
|---|---|---|---|
| KeepMounted | bool | false | Keep the popup mounted (hidden) while closed so an exit transition can run. |
| OnOpenAutoFocus | EventCallback\<NaviusOpenAutoFocusEventArgs\> | none | Cancelable; PreventDefault keeps focus where it is when the popup opens. |
| OnCloseAutoFocus | EventCallback\<NaviusCloseAutoFocusEventArgs\> | none | Cancelable; PreventDefault skips returning focus to the trigger on close. |
| OnEscapeKeyDown | EventCallback\<NaviusEscapeKeyDownEventArgs\> | none | Cancelable; PreventDefault keeps the popup open when Escape is pressed. |
| OnPointerDownOutside | EventCallback\<NaviusPointerDownOutsideEventArgs\> | none | Cancelable; PreventDefault keeps the popup open on an outside pointer-down. |
| OnFocusOutside | EventCallback\<NaviusFocusOutsideEventArgs\> | none | Cancelable; PreventDefault keeps the popup open when focus moves outside. |
| OnInteractOutside | EventCallback\<NaviusInteractOutsideEventArgs\> | none | Cancelable; PreventDefault keeps the popup open on any outside interaction. |
| Attributes | IDictionary\<string, object\>? | null | Captured unmatched attributes, forwarded to the `<div>`. |

### NaviusDialogTitle

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | Title content. |
| Attributes | IDictionary\<string, object\>? | null | Captured unmatched attributes. |

### NaviusDialogDescription

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | Description content. |
| Attributes | IDictionary\<string, object\>? | null | Captured unmatched attributes. |

### NaviusDialogClose

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | Button content. |
| Attributes | IDictionary\<string, object\>? | null | Captured unmatched attributes. |

## Events

| Part | Event | Signature | Fires when |
|---|---|---|---|
| NaviusDialog | OpenChanged | EventCallback\<bool\> | Any part requests an open-state change (trigger click, close click, dismiss) while controlled; the root forwards the requested value instead of mutating internal state. |
| NaviusDialogPopup (via OverlayPopupBase) | OnOpenAutoFocus | EventCallback\<NaviusOpenAutoFocusEventArgs\> | Before focus moves into the popup on engage (only if `MoveFocusInside`), so the handler can `PreventDefault` to keep focus elsewhere. |
| NaviusDialogPopup (via OverlayPopupBase) | OnCloseAutoFocus | EventCallback\<NaviusCloseAutoFocusEventArgs\> | On disengage (close), before focus would return to the trigger; `PreventDefault` skips the return-focus step. |
| NaviusDialogPopup (via OverlayPopupBase) | OnEscapeKeyDown | EventCallback\<NaviusEscapeKeyDownEventArgs\> | The JS dismissable layer invokes `OnDismiss("escape")`, which raises this before closing; `PreventDefault` keeps the dialog open. |
| NaviusDialogPopup (via OverlayPopupBase) | OnPointerDownOutside | EventCallback\<NaviusPointerDownOutsideEventArgs\> | The JS dismissable layer invokes `OnDismiss("outside")` on an outside pointer-down; raised before close. |
| NaviusDialogPopup (via OverlayPopupBase) | OnFocusOutside | EventCallback\<NaviusFocusOutsideEventArgs\> | Raised alongside `OnPointerDownOutside` in the "outside" dismiss path. |
| NaviusDialogPopup (via OverlayPopupBase) | OnInteractOutside | EventCallback\<NaviusInteractOutsideEventArgs\> | Raised alongside the other two in the "outside" dismiss path; any of the three `PreventDefault` calls keeps the dialog open. |

## State + data attributes

| Attribute / class | Part(s) | Meaning |
|---|---|---|
| `data-popup-open` | Trigger | Present (empty string) when `Context.Open`; absent (`null`) when closed. |
| `data-navius-dialog-trigger` | Trigger | Static marker attribute (styling hook). |
| `data-open` | Backdrop, Popup | Present when `Context.Open`. |
| `data-closed` | Backdrop, Popup | Present when not open. |
| `data-starting-style` | Backdrop, Popup | Present for the one frame after mount while `Entering` (before the transition-in frame commits). |
| `data-ending-style` | Backdrop, Popup | Present while `Exiting` (exit transition running, deferred unmount). |
| `data-navius-dialog-backdrop` / `data-navius-dialog-popup` / `data-navius-dialog-title` / `data-navius-dialog-description` / `data-navius-dialog-close` | respective part | Static marker attributes. |
| `id` | Popup | `Context.ContentId` (`navius-dialog-{guid}`). |
| `id` | Title | `Context.TitleId` = `{ContentId}-title`. |
| `id` | Description | `Context.DescriptionId` = `{ContentId}-desc`. |

Internal (non-DOM) state: `DialogContext.Open` (authoritative), `DialogContext.Modal`, `HasTitle`/`HasDescription` (ref-counted presence), `HasTrigger`, `PortalContainer`/`PortalForceMount`. `OverlayPresence` tracks `Rendered`, `Entering`, `Exiting` booleans that drive the four data-attributes above.

## Keyboard

| Key | Behavior |
|---|---|
| Escape | Closes the dialog, confirmed by the shared `OverlayPopupBase.OnDismiss("escape")` path (`CloseOnEscape` defaults to true) and by the "Drawer" e2e test in `tests/e2e/specs/wave2.spec.ts` which confirms the same shared mechanism closes on `page.keyboard.press('Escape')` for the sibling Drawer component. No Dialog-specific e2e spec exists; behavior is inferred from the shared `OverlayPopupBase` code path also used by Dialog. |
| Tab (focus cycling) | When `Modal` is true, `TrapFocus` is true and `OverlayPopupBase.EngageAsync` calls `Interop.CreateFocusTrapAsync(Element, ...)`, which hands focus-trapping to the JS focus-trap engine. The actual Tab-cycling key logic lives in JS/TS interop code outside this family's `.razor`/`.cs` files, so the exact cycling behavior (first/last focusable wrap-around) is not visible in the C# source. |

No explicit `@onkeydown`/`OnKeyDown` handler exists anywhere in the Dialog family's own `.razor`/`.cs` files; all keyboard handling is delegated to the shared `OverlayPopupBase` dismissable-layer / focus-trap JS interop.

## Accessibility

- Popup: `role="dialog"`, `aria-modal="true"` when `Context.Modal` else omitted, `aria-labelledby` set to `Context.TitleId` only when `Context.HasTitle` (else omitted, avoiding a dangling IDREF), `aria-describedby` set to `Context.DescriptionId` only when `Context.HasDescription`, `tabindex="-1"`.
- Trigger: `aria-haspopup="dialog"`, `aria-expanded` reflects `Context.Open`, `aria-controls` set to `Context.ContentId`.
- Dev-time warning: `NaviusDialogPopup.EngageAsync` logs a warning (`ILogger`) if the popup opens without a mounted `NaviusDialogTitle`, since a dialog needs an accessible name.
- Focus management: on engage (open), if `MoveFocusInside` (always true for Dialog, not overridden) and not prevented via `OnOpenAutoFocus`, focus moves into the popup: either the focus trap's configured initial element (modal) or `Element.FocusAsync()` directly (non-modal). On disengage (close), if a focus trap was active it releases and (unless `OnCloseAutoFocus` was prevented) restores focus; if there was no trap (non-modal) the code explicitly calls `OverlayContext.TriggerElement.FocusAsync()` to return focus to the trigger, gated on `Interop.IsFocusRestorableAsync`.
- Scroll lock: `Interop.LockScrollAsync()` / `UnlockScrollAsync()` called when `Modal` (`LockPageScroll` defaults to `OverlayContext.Modal`).

## WPF strategy

Tier B (custom lookless control).

Base class: a custom `Dialog` control hosted through `System.Windows.Controls.Primitives.Popup` (or a `Window`/`AdornerLayer`-based overlay) is more appropriate than deriving directly from `Popup`, since the family needs a root/context object (open state, title/description ref-counting) plus separate Trigger/Backdrop/Popup/Title/Description/Close sub-parts, similar to how `ContentDialog`-style custom controls are built in WPF. Map `role="dialog"` to `AutomationProperties.AutomationControlType = ControlType.Window` (or a custom `AutomationPeer` overriding `GetAutomationControlType()` to return `Window`/`Pane`), wire `aria-labelledby`/`aria-describedby` equivalents through `AutomationProperties.LabeledBy` and `AutomationProperties.HelpText`/`Name`. Things that will NOT translate cleanly: the JS-interop-driven dismissable layer, focus trap, and scroll lock (`NaviusJsInterop.CreateDismissableLayerAsync`/`CreateFocusTrapAsync`/`LockScrollAsync`) have no DOM/JS equivalent in WPF and must be reimplemented with native focus management (`Keyboard.Focus`, `FocusManager`, `PreviewKeyDown` for Escape/Tab) and a modal `Window.ShowDialog()` (which natively blocks input to owner) or manual input-capture for a non-Window-based popup; the `data-starting-style`/`data-ending-style` CSS-transition choreography (`Entering`/`Exiting` + `NextFrameAsync`/`WaitForAnimationsAsync`) needs a WPF storyboard/`VisualStateManager` reimplementation since there is no CSS transition engine.

## Open questions

- The code shows `CloseOnEscape` and `CloseOnOutside` as overridable booleans on `OverlayPopupBase` (both default true) but the Dialog family does not override either, so exact behavior for a non-default configuration is unconfirmed for Dialog specifically.
- Tab-cycling / focus-trap wrap-around behavior is delegated entirely to JS interop (`CreateFocusTrapAsync`); the WPF port needs a product decision on what wrap-around semantics to implement natively since the C# source gives no algorithm to port.
- `NaviusDialogPortal.Container` is documented as a CSS selector; WPF has no DOM/CSS selector concept, so the port needs a decision on how a consumer specifies an alternate visual-tree host (e.g. a named `AdornerDecorator` or a `Window` reference).
- Non-modal Dialog (`Modal="false"`) behavior (no trap, no scroll lock, outside content interactive) is asserted in comments but no non-modal Dialog e2e coverage was found; edge-case behavior when combined with the trigger-toggle click race is unconfirmed by tests for Dialog specifically.

## WPF implementation notes

Shipped in `Navius.Wpf.Primitives.Controls.Dialog.NaviusDialog` (net8.0-windows/net10.0-windows). Diverges from the strategy note above in one deliberate way: rather than a `Popup`- or `Window`-hosted overlay, or a decomposed Root/Trigger/Portal/Backdrop/Popup/Title/Description/Close set of control classes, this codebase already folds multi-part web families into one lookless `ContentControl` (see `NaviusRadioGroup`/`NaviusCheckboxGroup`), so Dialog (and AlertDialog/Drawer) follow the same shape: one `ContentControl` whose own `ControlTemplate` **is** the backdrop + panel, sharing state-machine plumbing via the new abstract `Controls.OverlaySurface.NaviusOverlaySurfaceBase`.

- **Hosting.** `System.Windows.Controls.Primitives.Popup` was rejected (not just "less appropriate" than an alternative, but broken for this use case): a `Popup` renders through its own top-level surface, and neither `OverlayStack`'s window-level `PreviewKeyDown`/`PreviewMouseDown` hooks nor its visual/logical-descendant outside-press checks can see into it, so Escape and outside-click routing would silently stop firing. Instead, a new `Controls.OverlaySurface.NaviusOverlayLayer` (a plain `Grid`, the WPF analogue of the web's `NaviusPortal`/`document.body` teleport target) is registered per-`Window` the same way `OverlayStack.GetFor` already is. Consumers declare `NaviusOverlayLayer` once, stretched over their window's root, and declare each `NaviusDialog`/`NaviusAlertDialog`/`NaviusDrawer` as its **direct XAML child** (never re-parented at runtime, since a XAML-declared child already has a logical parent and WPF throws on a second `Children.Add`); the control defaults to `Visibility.Collapsed` and only becomes visible while open. A window with no `NaviusOverlayLayer` still parses fine; opening a surface in it logs a `Trace.TraceWarning` and reverts `IsOpen` to `false` via `SetCurrentValue` (dev-time diagnostic, mirrors the source's own "missing Title" `ILogger` warning).
- **Role/AutomationPeer.** `role="dialog"` maps to a custom `NaviusDialogAutomationPeer : FrameworkElementAutomationPeer` overriding `GetAutomationControlTypeCore() => AutomationControlType.Window` and `IsDialogCore() => true` (available since .NET 6, confirmed present on net8.0-windows/net10.0-windows). `Title`/`Description` are plain string DPs; `Engage()` sets `AutomationProperties.SetName`/`SetHelpText` directly on the control rather than overriding `GetNameCore`/`GetHelpTextCore`, since the default peer already reads those attached properties.
- **Focus trap / Tab-cycling.** Delegated entirely to the existing `OverlayStack.Push(root, options)` (`TrapFocus = ModalEffective`), which already implements `KeyboardNavigation.TabNavigation="Cycle"` plus first-focusable-descendant initial focus — this resolves the doc's "product decision on wrap-around semantics" open question: cycle, matching native WPF `KeyboardNavigationMode.Cycle` rather than a bespoke reimplementation.
- **Container/portal-target.** Resolved by not needing one: `NaviusOverlayLayer.GetFor(window)` replaces the web's CSS-selector `Container` string entirely (this doc's "how does a consumer specify an alternate visual-tree host" open question). There is no `Container` parameter on `NaviusDialog`; a window may have at most one registered layer via the `ConditionalWeakTable<Window, NaviusOverlayLayer>` registry.
- **CloseOnEscape/CloseOnOutside.** `CloseOnEscape` is fixed `true` (not exposed as a DP, matching the doc's observation that Dialog never overrides it). `CloseOnOutsideClick` **is** exposed as a settable DP (default `true`), resolving the doc's open question in favor of making it configurable, since the web contract explicitly calls it "optional per contract."
- **Enter/exit transitions.** No CSS engine to defer to, so this port picks a concrete answer: `Opacity` fades 0→1 (enter) / 1→0 (exit) over 150ms via `DoubleAnimation`, mirroring the web's `data-starting-style`/`data-ending-style`/`data-open`/`data-closed` choreography (`OverlayPresence.Entering`/`Exiting`). The exit animation's `Completed` callback is what actually unmounts (`Visibility = Collapsed`, `IsOpen` synced back to `false`), so an exit transition is never truncated, per the Open/close API contract in the task brief. `Navius.Wpf.Primitives` does not reference `Navius.Wpf.Motion` for this (plain `DoubleAnimation`, per the hard constraint that this family stay motion-library-free).
- **A11y delta vs. the web contract.** The source's dev-time "missing title" warning only exists on `NaviusDialogPopup`; this port applies the same `AutomationProperties.SetName` treatment uniformly across Dialog/AlertDialog/Drawer rather than singling out Dialog, since a docked Drawer sheet is just as much an accessible-name gap as a modal Dialog if `Title` is left unset.

## M6 audit (2026-07-09)

Adversarial re-verification of every claim in the sections above against the shipped C#/XAML. Scope: dialog, drawer, popover, preview-card (this agent's families).

### CONFIRMED (fixed)

- **Canceled `Closing` desynced `IsOpen` (code bug, fixed).** The base summary advertises a "cancelable Closing forwarded from the underlying `OverlaySession`", and a handler setting `e.Cancel = true` did keep the `OverlaySession` open (`OverlaySession.RequestClose` returns false and does not pop). But `NaviusOverlaySurfaceBase.OnIsOpenChanged` ignored that return value, so a canceled close left the surface visible while the two-way `IsOpen` DP was stuck at `false`: a lying property, and, worse, a subsequent `Open()`/`IsOpen = true` was silently swallowed because `Engage()` is guarded by `_session is null` (still non-null after a canceled close). Fixed in `Controls/OverlaySurface/NaviusOverlaySurfaceBase.cs` `OnIsOpenChanged`: when `RequestClose` returns false the surface reverts `IsOpen` to `true` via `SetCurrentValue`, keeping the DP consistent with the still-open session. Regression test: `DialogTests.CancelingClosing_KeepsTheDialogOpenAndIsOpenStaysTrue` (shows a real window hosting a `NaviusOverlayLayer`, opens the dialog, cancels the close, asserts `IsOpen` stays true, the surface stays `Visible`, and the session is still topmost). This fix also covers Drawer (same shared base).

### Verified accurate (no change needed)

- Escape closes via the shared `OverlayStack` window/input-root `PreviewKeyDown` path: `CloseOnEscapeEffective` is fixed `true`, flowed to `OverlayOptions.CloseOnEscape`, matched by `OverlayDismissPolicy.FindEscapeTarget` (`OverlayStack.cs:145-162`).
- `CloseOnOutsideClick` is a settable DP defaulting `true` (`NaviusDialog.cs:29-33`), forwarded via `CloseOnOutsideClickEffective`.
- `NaviusDialogAutomationPeer` overrides `GetAutomationControlTypeCore() => Window` and `IsDialogCore() => true` exactly as documented (`NaviusDialog.cs:73,77`).
- `Themes/Dialog.xaml` uses only `DynamicResource` for themeable brushes/radii; all referenced keys (`Navius.Popover`, `Navius.PopoverForeground`, `Navius.Border`, `Navius.MutedForeground`, `Navius.Radius.Card`) exist in both `Tokens.Light.xaml` and `Tokens.Dark.xaml`.

### PLAUSIBLE / residual (not fixed, for the hardening agents)

- **Non-modal Dialog does not restore focus on close.** `OverlayStack.Push` only captures `RestoreFocusTarget` when `options.TrapFocus` is true, and `TrapFocusEffective => ModalEffective`, so a non-modal Dialog (`Modal="false"`) never restores focus to the pre-open element on close, unlike the web contract which restores focus even in the non-modal path. The gate lives in `Overlays/OverlayStack.cs` (outside this agent's edit boundary) and the WPF port has no discrete "trigger" element, so this is left as a residual for the Overlays owner to decide.
- **`IsDialogCore` availability comment.** `NaviusDialog.cs` and this doc's WPF strategy note say `IsDialogCore` is "available since .NET 6"; it appears to actually be a later addition. It compiles clean on the `net8.0-windows`/`net10.0-windows` targets, so this is a cosmetic comment-accuracy nit only, not verified against the exact framework version here.
