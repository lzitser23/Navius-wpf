# ContextMenu

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusContextMenu | none (renders `ChildContent` inside a `CascadingValue`) | Root. Owns open state (controlled via `Open`/`OpenChanged` or uncontrolled via `DefaultOpen`), cascades `ContextMenuContext`. |
| NaviusContextMenuTrigger | `<div>` | The right-clickable surface. Suppresses the native browser context menu, opens the menu anchored at the pointer point (or long-press point on touch, or the trigger's own rect on the keyboard menu key). No ARIA. |
| NaviusContextMenuPortal | none (renders `ChildContent`; sets flags on context) | Records custom mount container / `KeepMounted` into the context; actual DOM teleport is done by the Popup via `<NaviusPortal>`. |
| NaviusContextMenuPositioner | none (renders `ChildContent`; sets flags on context) | Publishes placement options (side/align/offsets/collision) into the context; default side `"right"`, align `"start"`. |
| NaviusContextMenuPopup | `<div>` (anchor div) + `<div>` (positioner div) + `<div role="menu">` | The menu surface. Renders a 0x0 fixed anchor div at the captured cursor point, a positioner div, and the `role="menu"` content div. Engages anchored positioning, dismissable layer, scroll lock (when modal) and roving focus. |
| NaviusContextMenuArrow | `<svg>` | Optional arrow pointing at the anchor point. `aria-hidden="true"`. Registers itself with the context so the Popup wires it into the positioner. |
| NaviusContextMenuGroup | `<div role="group">` | Groups items; a child Label registers its id here for `aria-labelledby`. |
| NaviusContextMenuLabel | `<div>` | Non-interactive group label. Not focusable by arrow keys. Registers its generated id with an ancestor `ContextMenuGroupContext`. |
| NaviusContextMenuItem | `<div role="menuitem">` | A menu item. `tabindex="-1"`. Activates on click or Enter/Space, then closes the menu unless `OnSelect` calls `PreventDefault()`. |
| NaviusContextMenuCheckboxItem | `<div role="menuitemcheckbox">` | Tri-state checkbox item (`Checked`: bool?, null = indeterminate). Controlled via `@bind-Checked` or uncontrolled via `DefaultChecked`. |
| NaviusContextMenuRadioGroup | `<div role="group">` | Holds a set of RadioItems. Controlled via `@bind-Value` or uncontrolled via `DefaultValue`. |
| NaviusContextMenuRadioItem | `<div role="menuitemradio">` | A radio option within a RadioGroup. Selecting sets the group's value. |
| NaviusContextMenuItemIndicator | `<span>` (conditionally rendered) | Renders only while the parent Checkbox/RadioItem is checked or indeterminate (unless `ForceMount`). |
| NaviusContextMenuSeparator | `<div role="separator">` | Visual/semantic divider. `aria-orientation="horizontal"`. |
| NaviusContextMenuSub | none (renders `ChildContent`; cascades `ContextMenuSubContext`) | A nested submenu. Owns its own open state (controlled via `@bind-Open` or uncontrolled via `DefaultOpen`). |
| NaviusContextMenuSubTrigger | `<div role="menuitem">` | The item that opens a submenu. `aria-haspopup="menu"`, `aria-expanded` tracks sub open state. Opens on `pointerenter` (hover-intent), click, or ArrowRight/Enter/Space (ArrowLeft in rtl). |
| NaviusContextMenuSubContent | `<div role="menu">` (conditionally rendered) | The nested submenu surface. Rendered inline (not portaled) inside the parent content's DOM subtree. Self-manages positioning/dismiss/roving focus via direct JS interop calls (not via the shared overlay base). |

## Parameters

### NaviusContextMenu
| Name | Type | Default | Notes |
|---|---|---|---|
| Open | bool | `false` (implicit) | Controlled open state. |
| OpenChanged | EventCallback\<bool\> | none | Presence of a delegate marks the component controlled. |
| DefaultOpen | bool | `false` (implicit) | Uncontrolled initial open state. |
| Dir | string? | none | `"ltr"` or `"rtl"`. Falls back to cascaded `NaviusDirection`, then `"ltr"`. |
| ChildContent | RenderFragment? | none | |

### NaviusContextMenuTrigger
| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | none | |
| Disabled | bool | `false` (implicit) | Suppresses context-menu open on right-click, long-press, and the keyboard menu key. |
| Attributes | IDictionary\<string, object\>? | none | Captured unmatched attributes. |

### NaviusContextMenuPortal
| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | none | |
| Container | string? | none | CSS selector of a custom mount container; null teleports into `document.body`. |
| KeepMounted | bool | `false` (implicit) | Keep the popup mounted while closed (for exit animations). |

### NaviusContextMenuPositioner
No component-local `[Parameter]` declared directly in this file; inherits placement parameters from `OverlayPositionerBase` (side/align/offsets/collision), not read here as this file only overrides `DefaultSide` (`"right"`) and `DefaultAlign` (`"start"`).

### NaviusContextMenuPopup
| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | none | |
| Loop | bool | `false` (implicit) | When true, arrow navigation wraps at the ends (spec default false). |

(Additional parameters such as `KeepMounted`, `Attributes`, and animation-related fields are inherited from `OverlayAnchoredPopupBase`/`OverlayPopupBase`, not declared in this file.)

### NaviusContextMenuArrow
| Name | Type | Default | Notes |
|---|---|---|---|
| Width | double | `10` | |
| Height | double | `5` | |
| Attributes | IDictionary\<string, object\>? | none | |

### NaviusContextMenuGroup
| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | none | |
| Attributes | IDictionary\<string, object\>? | none | |

### NaviusContextMenuLabel
| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | none | |
| Attributes | IDictionary\<string, object\>? | none | |

### NaviusContextMenuItem
| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | none | |
| OnSelect | EventCallback\<NaviusSelectEventArgs\> | none | Cancelable; `PreventDefault()` keeps the menu open. |
| Disabled | bool | `false` (implicit) | Skipped by roving focus (`data-disabled`); does not activate. |
| TextValue | string? | none | Overrides type-ahead match text for icon/complex content. |
| Attributes | IDictionary\<string, object\>? | none | |

### NaviusContextMenuCheckboxItem
| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | none | |
| Checked | bool? | `null` (implicit) | Controlled tri-state (null = indeterminate). Use `@bind-Checked`. |
| CheckedChanged | EventCallback\<bool?\> | none | |
| DefaultChecked | bool? | `null` (implicit) | Uncontrolled initial value. |
| OnSelect | EventCallback\<NaviusSelectEventArgs\> | none | Cancelable; `PreventDefault()` keeps the menu open after toggling. |
| Disabled | bool | `false` (implicit) | |
| TextValue | string? | none | |
| Attributes | IDictionary\<string, object\>? | none | |

### NaviusContextMenuRadioGroup
| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | none | |
| Value | string? | none | Controlled selected value. Use `@bind-Value`. |
| ValueChanged | EventCallback\<string?\> | none | |
| DefaultValue | string? | none | Uncontrolled initial value. |
| Attributes | IDictionary\<string, object\>? | none | |

### NaviusContextMenuRadioItem
| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | none | |
| Value | string | `default!` (required, no literal default) | This item's value within the radio group. |
| OnSelect | EventCallback\<NaviusSelectEventArgs\> | none | Cancelable. |
| Disabled | bool | `false` (implicit) | |
| TextValue | string? | none | |
| Attributes | IDictionary\<string, object\>? | none | |

### NaviusContextMenuItemIndicator
| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | none | |
| ForceMount | bool | `false` (implicit) | Keep mounted even when unchecked (for exit animations). |
| Attributes | IDictionary\<string, object\>? | none | |

### NaviusContextMenuSeparator
| Name | Type | Default | Notes |
|---|---|---|---|
| Attributes | IDictionary\<string, object\>? | none | |

### NaviusContextMenuSub
| Name | Type | Default | Notes |
|---|---|---|---|
| Open | bool | `false` (implicit) | Controlled open state. |
| OpenChanged | EventCallback\<bool\> | none | Presence detected explicitly via `SetParametersAsync` to determine controlled mode. |
| DefaultOpen | bool | `false` (implicit) | Uncontrolled initial value. |
| ChildContent | RenderFragment? | none | |

### NaviusContextMenuSubTrigger
| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | none | |
| Disabled | bool | `false` (implicit) | Skipped by roving focus; never opens. |
| TextValue | string? | none | |
| Dir | string? | none | Falls back to cascaded `NaviusDirection`, then `"ltr"`. Swaps open key (ArrowRight vs ArrowLeft). |
| Attributes | IDictionary\<string, object\>? | none | |

### NaviusContextMenuSubContent
| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | none | |
| Side | string | `"right"` | Submenus open to the side; rtl flips `"right"` to `"left"`. |
| Align | string | `"start"` | |
| SideOffset | double | `0` (implicit) | |
| AlignOffset | double | `0` (implicit) | |
| AvoidCollisions | bool | `true` | |
| CollisionPadding | double? | none | |
| Loop | bool | `false` (implicit) | Roving wraps at ends only when true (spec submenus default false). |
| ForceMount | bool | `false` (implicit) | Keep mounted while closed. |
| Dir | string? | none | Falls back to cascaded `NaviusDirection`, then `"ltr"`. |
| Attributes | IDictionary\<string, object\>? | none | |

## Events

| Part | Event | Payload |
|---|---|---|
| NaviusContextMenu | OpenChanged | `bool` |
| NaviusContextMenuItem | OnSelect | `NaviusSelectEventArgs` (cancelable via `PreventDefault()`) |
| NaviusContextMenuCheckboxItem | CheckedChanged | `bool?` |
| NaviusContextMenuCheckboxItem | OnSelect | `NaviusSelectEventArgs` |
| NaviusContextMenuRadioGroup | ValueChanged | `string?` |
| NaviusContextMenuRadioItem | OnSelect | `NaviusSelectEventArgs` |
| NaviusContextMenuSub | OpenChanged | `bool` |

## State + data attributes

- `ContextMenuContext`: `Open` (bool), `AnchorX`/`AnchorY` (double, cursor point), `Modal` (bool, default true), `Dir`/`IsRtl`, `PortalContainer`, `PortalKeepMounted`, `HasArrow`, `HasTrigger`.
- `ContextMenuSubContext`: `Open` (bool), `HasTrigger` (bool).
- `ContextMenuCheckableContext`: `Checked` (bool?, tri-state), `IsShown` (true when checked or indeterminate).
- `ContextMenuRadioGroupContext`: `Value` (string?), `IsSelected(value)`.
- `ContextMenuGroupContext`: `LabelId` (string?).
- Data attributes rendered: `data-navius-context-menu-anchor`, `data-navius-context-menu-positioner`, `data-navius-context-menu-popup`, `data-open`/`data-closed`, `data-starting-style`/`data-ending-style` (Popup); `data-navius-context-menu-trigger`, `data-popup-open`, `data-disabled` (Trigger); `data-navius-context-menu-arrow` (Arrow); `data-navius-context-menu-group` (Group); `data-navius-context-menu-label` (Label); `data-navius-context-menu-item`, `data-disabled`, `data-navius-text-value` (Item); `data-navius-context-menu-checkbox-item`, `data-checked`/`data-unchecked`/`data-indeterminate`, `data-disabled`, `data-navius-text-value` (CheckboxItem); `data-navius-context-menu-radio-group` (RadioGroup); `data-navius-context-menu-radio-item`, `data-checked`/`data-unchecked`, `data-disabled`, `data-navius-text-value` (RadioItem); `data-checked`/`data-unchecked`/`data-indeterminate`, `data-navius-context-menu-item-indicator` (ItemIndicator); `data-orientation="horizontal"`, `data-navius-context-menu-separator` (Separator); `data-navius-context-menu-sub-trigger`, `data-popup-open`, `data-disabled`, `data-navius-text-value` (SubTrigger); `data-navius-context-menu-sub-content`, `data-open`/`data-closed`, `data-side`, `data-align` (SubContent); `data-highlighted` (roving-focus engine, mirrors focus for the currently highlighted item).

## Keyboard

Roving focus (JS `createRovingFocus`, orientation `"vertical"`) governs the popup and each submenu's item set (`[role="menuitem"]`, `[role="menuitemcheckbox"]`, `[role="menuitemradio"]`, all `:not([data-disabled])`).

| Key | Behavior |
|---|---|
| ArrowDown | Move focus to the next item (Popup / SubContent, vertical orientation). |
| ArrowUp | Move focus to the previous item. |
| Home | Focus the first item. |
| End | Focus the last item. |
| Space | `preventDefault` only (stops page scroll); activation handled by the focused item's own keydown handler. |
| a-z / 0-9 / any single printable char | Type-ahead: focuses the next item (search starts after the currently focused one, wraps) whose text (or `data-navius-text-value`) starts with that character. |
| Enter / Space (on Item / CheckboxItem / RadioItem) | Activates the item: fires `OnSelect`; closes the menu unless `PreventDefault()` was called. Space is prevent-defaulted at the item level during the keydown to suppress page scroll. |
| ArrowRight (ArrowLeft in rtl), on SubTrigger | Opens the submenu (also opens on Enter/Space and on `pointerenter`/click). |
| ArrowLeft (ArrowRight in rtl), inside SubContent | Closes the submenu and returns focus to the SubTrigger. |
| Escape | Closes the popup (dismissable layer, `closeOnEscape: true` by default) via `OnDismiss`. |
| Shift+F10 or ContextMenu key, on Trigger | Opens the menu anchored at the trigger element's own rect (APG keyboard equivalent to right-click). |

Outside pointer-down also dismisses the popup (dismissable layer, `closeOnOutside` default true); the "inside" reference for the root Popup is the popup element itself (not the trigger), so a large trigger area does not swallow the dismiss click.

## Accessibility

- Popup: `role="menu"`, `tabindex="-1"`, `dir` reflects `Context.IsRtl`.
- Item: `role="menuitem"`, `tabindex="-1"`, `data-disabled`/`aria-disabled="true"` when disabled.
- CheckboxItem: `role="menuitemcheckbox"`, `aria-checked` = `"true"`/`"false"`/`"mixed"` (tri-state), `aria-disabled`.
- RadioItem: `role="menuitemradio"`, `aria-checked` = `"true"`/`"false"`, `aria-disabled`.
- Group: `role="group"`, `aria-labelledby` bound to a child Label's generated id.
- Label: no explicit ARIA role (non-interactive text); provides the `id` referenced by the Group's `aria-labelledby`.
- Separator: `role="separator"`, `aria-orientation="horizontal"`.
- SubTrigger: `role="menuitem"` (so it participates in the parent's roving focus), `aria-haspopup="menu"`, `aria-expanded`, `aria-controls` (set to the sub content id only while open), `aria-disabled`.
- SubContent: `role="menu"`, `tabindex="-1"`, `aria-labelledby` set to the SubTrigger's id.
- Arrow: `aria-hidden="true"`.
- Trigger: no ARIA (it's a surface, not a control), per spec parity comment in the code.
- Focus management: on Popup open, roving focus auto-focuses the first item (unless `OpenAutoFocusPrevented`); on close, focus returns to the trigger area (`Context.TriggerElement`). On SubContent close, focus returns to `Sub.TriggerElement`. The Popup uses roving focus, not a focus trap (`TrapFocus => false`).

## WPF strategy

Tier B (custom lookless control)

WPF's built-in `System.Windows.Controls.ContextMenu`/`MenuItem` do not natively support point-anchored positioning driven by long-press or the keyboard menu key, tri-state checkable items surfaced through a general `ItemIndicator` slot, or the exact composable part-per-part API surface (Portal/Positioner/Arrow/Group/Label as separate components) this library exposes. Build a lookless `ContextMenu`-derived (or `Popup`-hosted) control set with `ControlTemplate`/`Style` parts named to match (PART_Popup, PART_Anchor, etc.), and back checkbox/radio items with `MenuItem.IsCheckable`/`MenuItem` grouping conventions or custom `ToggleButton`-based items. Map ARIA roles to `AutomationPeer`s: `role="menu"` → `MenuAutomationPeer`/`MenuItemAutomationPeer` pattern (`ControlType.Menu`), `role="menuitem"` → `ControlType.MenuItem`, `role="menuitemcheckbox"`/`menuitemradio` → `ControlType.CheckBox`/`ControlType.RadioButton` exposed via a `MenuItemAutomationPeer` with `ToggleProvider`/`SelectionItemProvider`. Things that will NOT translate cleanly: browser portal-to-`document.body` rendering (WPF popups already float in their own window, so `NaviusContextMenuPortal`'s `Container` concept has no WPF equivalent and should collapse to a no-op), the JS-driven long-press touch gesture (needs a native `TouchDown`/`Stylus` timer reimplementation), and the exact pixel-based cursor anchoring math (`AnchorX`/`AnchorY` fixed-position div) which should map to WPF's `Popup.Placement=Absolute`/`HorizontalOffset`/`VerticalOffset` instead of a DOM anchor element.

## Open questions

- Should the WPF port keep the same fine-grained part decomposition (Portal, Positioner, Arrow as separate elements) or collapse them into template parts of a single `ContextMenu` control, given WPF's `Popup` already owns placement/positioning?
- Tri-state `CheckboxItem` (`bool?`) has no first-class WPF `MenuItem` equivalent (WPF `MenuItem.IsChecked` is `bool`); does the port need a custom indeterminate visual state or is `bool?` binding sufficient?
- Long-press-to-open on touch and Shift+F10/`ContextMenu` key handling both call back into interop for element-rect measurement; in WPF this measurement is trivial (`UIElement.PointToScreen`), so should the C# API keep an explicit `RequestOpenAtAsync(x, y)` surface, or should WPF's native `ContextMenuOpening`/`FrameworkElement.ContextMenu` be the entry point instead?
- The type-ahead search text prefers `data-navius-text-value` over element text content; the WPF equivalent (`TextSearch.Text` attached property) exists natively on `MenuItem`; should the port just rely on that instead of reimplementing custom type-ahead logic?
- `Modal` (scroll-lock + outside-pointer guard) is a DOM/browser concept; does the WPF port need any equivalent, or does native `Popup.StaysOpen=false` fully cover dismiss-on-outside-click?
