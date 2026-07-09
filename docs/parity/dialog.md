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
