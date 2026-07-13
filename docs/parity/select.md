# Select

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusSelect | no visible DOM (plus optional hidden `<input>` mirror(s) via `NaviusBubbleInput`) | Root: owns value/open state (controlled or uncontrolled), cascades `SelectContext` |
| NaviusSelectTrigger | `button` | The control that opens the listbox; `role="combobox"` |
| NaviusSelectValue | `span` | Shows the selected item's label (or the placeholder) inside the trigger |
| NaviusSelectIcon | `span` (default chevron-down `svg`) | Decorative affordance inside the trigger |
| NaviusSelectPortal | no DOM of its own | Flag-setter: records portal container / `KeepMounted` into the context; actual teleport happens in the Popup via `NaviusPortal` |
| NaviusSelectPositioner | no DOM of its own (renders `ChildContent`) | Flag-setter: publishes placement options (side/align/offsets/collision) into the context; the Popup renders the actual positioning `div` |
| NaviusSelectPopup | Portal > positioning `div` (`data-navius-select-positioner`) > listbox `div` (`data-navius-select-popup`) | The listbox surface; `role="listbox"`; engages anchored positioning, dismissable layer, and roving focus |
| NaviusSelectArrow | `span` (default triangle `svg`) | Optional pointer triangle positioned by the engine against the trigger |
| NaviusSelectViewport | `div` | Scrollable container wrapping items/groups/separators; observes its own scroll geometry |
| NaviusSelectGroup | `div` | `role="group"` wrapping related items, labelled by a child Label |
| NaviusSelectLabel | `div` | Accessible heading for a Group (adopts the group's id for `aria-labelledby`) |
| NaviusSelectItem | `div` | `role="option"`; selectable row; cascades `SelectItemContext` |
| NaviusSelectItemText | `span` | Label content inside an Item; optionally registers a plain-string label for the trigger |
| NaviusSelectItemIndicator | `span` (only when selected) | Renders `ChildContent` (e.g. a check glyph) only when the parent item is selected |
| NaviusSelectSeparator | `div` | Decorative divider between item sections |
| NaviusSelectScrollUpButton | `div` (only when scrollable up) | Mounts only when the Viewport is scrolled down from the top |
| NaviusSelectScrollDownButton | `div` (only when scrollable down) | Mounts only when there is more content below the fold |

## Parameters

**NaviusSelect**

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | string? | null | Controlled selected value; use `@bind-Value` |
| ValueChanged | EventCallback<string?> | - | Fires on every selection, controlled or not |
| DefaultValue | string? | null | Uncontrolled initial value |
| Multiple | bool | false | Enables multi-select: clicking toggles the value in the set and keeps the popup open |
| Values | IReadOnlyList<string> | Array.Empty<string>() | Controlled selected set; use `@bind-Values`; only used when `Multiple` |
| ValuesChanged | EventCallback<IReadOnlyList<string>> | - | Fires on every toggle, controlled or not |
| DefaultValues | IReadOnlyList<string>? | null | Uncontrolled initial selected set; only used when `Multiple` |
| Open | bool | false | Controlled open state; use `@bind-Open` |
| OpenChanged | EventCallback<bool> | - | Fires on every open-state change, controlled or not |
| DefaultOpen | bool | false | Uncontrolled initial open state |
| Placeholder | string? | null | Shown by `NaviusSelectValue` when nothing is selected |
| Disabled | bool | false | Disables the whole control; trigger becomes non-interactive |
| Name | string? | null | When set, renders a hidden native mirror (`<input type="hidden">` per value, or one for the single value) for form submission |
| Required | bool | false | Marks the hidden form mirror(s) required for native validation |
| Dir | string? | null | `ltr` \| `rtl`; falls back to cascaded `NaviusDirection` |
| ChildContent | RenderFragment? | null | |

**NaviusSelectTrigger**

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary<string,object>? | null | Captured onto the `button` |

**NaviusSelectValue**

| Name | Type | Default | Notes |
|---|---|---|---|
| Placeholder | string? | null | Overrides the root's placeholder text when nothing is selected |
| ChildContent | RenderFragment? | null | Overrides the auto label entirely when supplied |
| Attributes | IDictionary<string,object>? | null | |

**NaviusSelectIcon**

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | Overrides the default chevron-down svg |
| Attributes | IDictionary<string,object>? | null | |

**NaviusSelectPortal**

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Container | string? | null | CSS selector of a custom mount container; null teleports into `document.body` |
| KeepMounted | bool | false | Keep the popup mounted while closed (for exit animations) |

**NaviusSelectPositioner** (inherits `OverlayPositionerBase`)

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | Inherited from `OverlayPositionerBase` |
| Side | string? | null → "bottom" | Inherited; falls back to `DefaultSide`, which the Select razor does not override (stays "bottom") |
| Align | string? | null → "start" | Inherited; Select overrides `DefaultAlign` to `"start"` (vs. the base's `"center"`) |
| SideOffset | double | 0 | Inherited |
| AlignOffset | double | 0 | Inherited |
| Flip | bool | true | Inherited; folded into `AvoidCollisions` |
| AvoidCollisions | bool | true | Inherited |
| CollisionPadding | double? | null | Inherited |
| Sticky | string? | null | Inherited; `DefaultSticky` not overridden by Select (stays null) |
| HideWhenDetached | bool | false | Inherited |
| ArrowPadding | double | 0 | Inherited |
| Attributes | IDictionary<string,object>? | null | Inherited; published into `SelectContext.PositionerAttributes`, applied by the Popup to the positioning div |

**NaviusSelectPopup** (inherits `OverlayAnchoredPopupBase` → `OverlayPopupBase` → `OverlayPresence`)

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | Declared directly on the Popup |
| Loop | bool | false | Declared directly; when true, arrow navigation wraps at the ends (the spec Select default is false) |
| KeepMounted | bool | false | Inherited from `OverlayPopupBase`; Popup's `ShouldStayMounted` overrides to `KeepMounted \|\| Context.PortalKeepMounted` |
| OnOpenAutoFocus | EventCallback<NaviusOpenAutoFocusEventArgs> | - | Inherited from `OverlayPopupBase` |
| OnCloseAutoFocus | EventCallback<NaviusCloseAutoFocusEventArgs> | - | Inherited |
| OnEscapeKeyDown | EventCallback<NaviusEscapeKeyDownEventArgs> | - | Inherited |
| OnPointerDownOutside | EventCallback<NaviusPointerDownOutsideEventArgs> | - | Inherited |
| OnFocusOutside | EventCallback<NaviusFocusOutsideEventArgs> | - | Inherited |
| OnInteractOutside | EventCallback<NaviusInteractOutsideEventArgs> | - | Inherited |
| Attributes | IDictionary<string,object>? | null | Inherited; applied to the listbox `div` (the popup element itself, not the positioner) |

**NaviusSelectArrow**

| Name | Type | Default | Notes |
|---|---|---|---|
| Width | int | 10 | Arrow svg width in px |
| Height | int | 5 | Arrow svg height in px |
| ChildContent | RenderFragment? | null | Overrides the default triangle shape |
| Attributes | IDictionary<string,object>? | null | |

**NaviusSelectViewport**

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary<string,object>? | null | |

**NaviusSelectGroup**

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary<string,object>? | null | |

**NaviusSelectLabel**

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary<string,object>? | null | |

**NaviusSelectItem**

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | string | "" | The opaque value key for this option |
| Disabled | bool | false | Sets `aria-disabled`/`data-disabled`; skipped by roving focus and selection |
| TextValue | string? | null | Explicit text for type-ahead and the trigger label; also registered as the value's display label |
| OnSelect | EventCallback<NaviusSelectEventArgs> | - | Cancelable; `PreventDefault` keeps the listbox open / skips applying the value |
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary<string,object>? | null | |

**NaviusSelectItemText**

| Name | Type | Default | Notes |
|---|---|---|---|
| Text | string? | null | Plain-string label registered for this item's value; takes priority over the parent Item's `TextValue`; falls back to `TextValue` then the raw value key when omitted |
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary<string,object>? | null | |

**NaviusSelectItemIndicator**

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | |
| Attributes | IDictionary<string,object>? | null | |

**NaviusSelectSeparator**

| Name | Type | Default | Notes |
|---|---|---|---|
| Attributes | IDictionary<string,object>? | null | |

**NaviusSelectScrollUpButton / NaviusSelectScrollDownButton** (identical parameter shape)

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | null | Overrides the default chevron svg |
| Attributes | IDictionary<string,object>? | null | |

## Events

| Part | Event | Signature | Cancelable |
|---|---|---|---|
| NaviusSelect | ValueChanged | EventCallback<string?> | No |
| NaviusSelect | ValuesChanged | EventCallback<IReadOnlyList<string>> | No |
| NaviusSelect | OpenChanged | EventCallback<bool> | No |
| NaviusSelectItem | OnSelect | EventCallback<NaviusSelectEventArgs> | Yes: `PreventDefault` skips selection/close |
| NaviusSelectPopup (inherited) | OnOpenAutoFocus | EventCallback<NaviusOpenAutoFocusEventArgs> | Yes: keeps focus where it is on open |
| NaviusSelectPopup (inherited) | OnCloseAutoFocus | EventCallback<NaviusCloseAutoFocusEventArgs> | Yes: skips returning focus to the trigger on close |
| NaviusSelectPopup (inherited) | OnEscapeKeyDown | EventCallback<NaviusEscapeKeyDownEventArgs> | Yes: keeps the popup open on Escape |
| NaviusSelectPopup (inherited) | OnPointerDownOutside | EventCallback<NaviusPointerDownOutsideEventArgs> | Yes: keeps the popup open on outside pointer-down |
| NaviusSelectPopup (inherited) | OnFocusOutside | EventCallback<NaviusFocusOutsideEventArgs> | Yes: keeps the popup open when focus moves outside |
| NaviusSelectPopup (inherited) | OnInteractOutside | EventCallback<NaviusInteractOutsideEventArgs> | Yes: keeps the popup open on any outside interaction |

## State + data attributes

**NaviusSelectTrigger** (`button`)

- `role="combobox"`, `aria-haspopup="listbox"`, `aria-expanded`, `aria-controls` (→ `Context.ContentId`), `aria-required` (when `Required`)
- `data-popup-open`: present when open
- `data-disabled`: present when `Context.Disabled`
- `data-placeholder`: present when nothing is selected (`!Context.HasSelection`)
- `data-multiple`: present when `Context.Multiple`
- `dir`: mirrors `Context.Dir`
- native `disabled` attribute mirrors `Context.Disabled`

**NaviusSelectValue** (`span`)

- `data-placeholder`: present when nothing is selected

**NaviusSelectPopup** (positioning `div` + listbox `div`)

- Positioning div: `data-navius-select-positioner`, plus engine-written `data-side`/`data-align`/`data-anchor-hidden` and `--anchor-*`/`--available-*` CSS custom properties (per code comment; not directly set in the razor)
- Listbox div: `role="listbox"`, `tabindex="-1"`, `dir`, `aria-multiselectable` (when `Multiple`), `data-multiple`, `data-open`/`data-closed` (discrete open state), `data-starting-style`/`data-ending-style` (enter/exit transition frames, from `OverlayPresence.Entering`/`Exiting`)
- `id` set to `Context.ContentId` (matches the trigger's `aria-controls`)

**NaviusSelectItem** (`div`)

- `role="option"`, `tabindex="-1"`
- `aria-selected`: `"true"`/`"false"` reflecting `Context.IsSelected(Value)`
- `aria-disabled`: present when `Disabled`
- `data-selected`: present when selected
- `data-disabled`: present when `Disabled`
- `data-navius-text-value`: the type-ahead/trigger-label text (`TextValue`)
- `data-highlighted`: toggled by the JS roving-focus engine (not set in the C# item markup itself)

**NaviusSelectArrow**

- `aria-hidden="true"`

**NaviusSelectIcon**

- `aria-hidden="true"`

**NaviusSelectItemIndicator**

- `aria-hidden="true"`; only rendered when `ItemContext.IsSelected`

**NaviusSelectSeparator**

- `aria-hidden="true"`

**NaviusSelectScrollUpButton / NaviusSelectScrollDownButton**

- `aria-hidden="true"`; mount only when `SelectViewportContext.CanScrollUp` / `CanScrollDown` is true

**NaviusSelectGroup**

- `role="group"`, `aria-labelledby` (→ the group's `SelectGroupContext.LabelId`)

**NaviusSelectViewport**

- `role="presentation"`

**Public state on `SelectContext`**: `Open`, `Value`, `Multiple`, `SelectedValues`, `HasSelection`, `Placeholder`, `Disabled`, `Required`, `Name`, `Dir`, `OpenFocusMode` ("selected" \| "first" \| "last"), `ContentId`, `TriggerElement`/`HasTrigger`, `PositionReference` (= `TriggerElement`), `Modal` (always false for Select), `PortalContainer`, `PortalKeepMounted`, `Options`/`PositionerAttributes` (placement), `ArrowElement`/`HasArrow`, and a value→text registry (`RegisterText`/`SelectedText`/`SelectedTexts`) driving the trigger label.

## Keyboard

Evidence sources: `NaviusSelectTrigger.razor` (`OnKeyDownAsync`), `NaviusSelectItem.razor` (`OnKeyDownAsync`), the shared roving-focus engine (`createRovingFocus` in `navius-interop.js`, wired by `NaviusSelectPopup.EngageAsync` via `CreateRovingFocusAsync`), and `tests/e2e/specs/navius.spec.ts` (`Select (listbox)` describe block).

**Closed trigger** (`NaviusSelectTrigger`, only handled when `!Context.Disabled && !Context.Open`):

| Key | Behavior |
|---|---|
| Enter / Space | Opens the listbox, focus lands on the FIRST option (`RequestOpenWithFocusAsync("first")`); default prevented |
| ArrowDown | Same as Enter/Space: opens landing on the FIRST option; default prevented |
| ArrowUp | Opens the listbox, focus lands on the LAST option (`RequestOpenWithFocusAsync("last")`); default prevented |
| Click | Toggles open via `RequestToggleAsync()` (`OpenFocusMode` reset to "selected") |

**Open listbox** (roving focus over `[role="option"]:not([data-disabled])`, orientation vertical, wired by `NaviusSelectPopup`):

| Key | Behavior |
|---|---|
| ArrowDown | Moves focus to the next option; wraps only if `Loop="true"` (default false per the Select spec) |
| ArrowUp | Moves focus to the previous option; wraps only if `Loop="true"` |
| Home | Jumps focus to the first option (engine default; not Select-specific override) |
| End | Jumps focus to the last option (engine default) |
| A printable character | Type-ahead: jumps to the next option whose `data-navius-text-value` (or text content) starts with that character, searching forward from the item after the current one |
| Space | `preventDefault` only (stops page scroll); activation is handled by the focused item's own key handler, not the roving-focus engine |
| Enter or Space (on a focused `NaviusSelectItem`) | Selects the item (`SelectAsync`): sets the value and, in single-select mode, closes the popup; multi-select stays open |
| Escape | Closes the popup via the shared dismissable layer (`OverlayPopupBase.OnDismiss`, reason "escape"); confirmed by e2e test |
| Click on an item | Same selection path as Enter/Space |
| Click outside | Closes via the dismissable layer (reason "outside") |

On reopen, roving focus lands on the currently-selected option (`OpenFocusMode = "selected"`, the default reset by `RequestSetOpenAsync`/`RequestToggleAsync`), else the first option.

## Accessibility

- Trigger: `role="combobox"`, `aria-haspopup="listbox"`, `aria-expanded`, `aria-controls` pointing at the popup's `id` (`Context.ContentId`), `aria-required` when `Required`, native `disabled`.
- Popup: `role="listbox"`, `tabindex="-1"`, `aria-multiselectable` when `Multiple`.
- Item: `role="option"`, `tabindex="-1"` (roving, not natively tabbable), `aria-selected`, `aria-disabled` when disabled.
- Group: `role="group"`, `aria-labelledby` referencing the Label's generated id.
- Icon, Arrow, ItemIndicator, Separator, ScrollUpButton, ScrollDownButton are all `aria-hidden="true"` (purely decorative; the code comments note keyboard users navigate via item roving, not the scroll buttons).
- Focus management (inherited from `OverlayPopupBase`/`OverlayPresence`): `TrapFocus` is overridden to `false` on the Popup ("a listbox manages focus with roving tabindex, not a focus trap"). On open, the base still focuses the popup element (`MoveFocusInside`, default true) so focus lands inside, then roving focus (`AutoFocus: !OpenAutoFocusPrevented`) moves it onto an option per `OpenFocusMode`. On close, unless `OnCloseAutoFocus` is prevented, focus returns to the trigger (`OverlayContext.TriggerElement.FocusAsync()`).
- The select is **non-modal** (`SelectContext.Modal` is always false): no scroll lock, no focus trap (this differs from Dialog-family popups that inherit the same base).
- Dismissable layer (via `OverlayPopupBase.EngageAsync`): closes on Escape and on outside pointer-down, both cancelable through `OnEscapeKeyDown`/`OnPointerDownOutside`/`OnFocusOutside`/`OnInteractOutside`.

## WPF strategy

**Tier A** (derive from native WPF control) for the Trigger/Value/overall shell, layered with **Tier B** (custom lookless control) for the Popup/listbox internals.

`ComboBox` is the natural base: it already supplies a `ToggleButton`-driven trigger, a `Popup` for the dropdown, `ComboBoxItem` for options, and built-in `AutomationPeer`s (`ComboBoxAutomationPeer`, `ComboBoxItemAutomationPeer`) that map directly onto the `combobox`/`listbox`/`option` ARIA roles used here: `AutomationProperties` and `SelectionItemPattern`/`ExpandCollapsePattern` cover `aria-expanded`/`aria-selected`/`aria-controls` largely for free. Multi-select (`Multiple`, toggling `Values`/`SelectedValues` while staying open) has no native `ComboBox` equivalent and will need a custom selection model (closer to a `ListBox` with `SelectionMode="Multiple"` embedded in the popup) layered on top, or a fully custom `Selector`-derived control if fidelity with the toggle-and-stay-open semantics matters. Base UI's `Portal`/`Positioner` (teleport to `document.body` or a custom container, engine-computed anchored placement with `Side`/`Align`/`SideOffset`/`AlignOffset`/collision avoidance/flip) diverges from `ComboBox`'s default `Popup`, which does simple `Placement` enum positioning without collision-avoidance flipping or a separate arrow-pointer element (`NaviusSelectArrow`): a `Popup` with `CustomPopupPlacementCallback` or a `Popup` + manual `PlacementMode.Custom` will be needed to reproduce side/align/collision behavior; the arrow triangle has no `ComboBox` analogue and must be custom-drawn. `NaviusSelectViewport`'s `CanScrollUp`/`CanScrollDown`-gated scroll buttons don't exist on `ComboBox` either (native `ComboBox` popups just get a `ScrollViewer`); if the ScrollUp/Down buttons must be pixel-for-pixel replicated, wire two `Button`s that observe a `ScrollViewer`'s `ScrollChanged` the way `SelectViewportContext.UpdateScrollState` does here. Roving-focus with type-ahead and Home/End is close to `ComboBox`'s native keyboard handling already, but the `Loop`-default-false / land-on-selected-else-first / land-on-first-or-last-from-closed-trigger semantics are specific enough that they may need explicit `PreviewKeyDown` handling rather than relying on `ComboBox` defaults.

## Open questions

- Multi-select: does the WPF port need the exact "toggle in set, keep popup open, only Escape/outside closes" model, or is a more WPF-idiomatic multi-select (e.g. checkboxes, explicit "Apply" button) acceptable? Native `ComboBox` has no multi-select mode at all.
- `NaviusSelectPortal`'s `Container` (CSS selector for a custom mount point) has no WPF equivalent (`Popup` doesn't render into an arbitrary logical-tree location the way a DOM portal does): does the port need an adorner-layer-based portal, or is `Popup` teleporting to a top-level window sufficient?
- The engine-driven positioner writes `--anchor-*`/`--available-*` CSS custom properties onto the positioner div per the code comment, but the exact property list isn't visible in the C# reviewed here (it's implemented in the JS `Interop.CreatePositionerAsync` call): confirm the full set with whoever owns `NaviusJsInterop`/the JS engine before treating this doc as complete on that point.
- `TextValue` vs. `NaviusSelectItemText.Text` vs. rendered `ChildContent` for trigger-label resolution is a three-way priority chain (Text > TextValue > raw value key) baked into `SelectContext`'s text registry; the WPF port needs an equivalent registry or a simpler `DisplayMemberPath`-style convention: worth deciding early since it affects the `ComboBoxItem` template contract.
- The dismissable layer's cancelable event superset (`OnEscapeKeyDown`, `OnPointerDownOutside`, `OnFocusOutside`, `OnInteractOutside`) is called out in `OverlayPopupBase`'s own doc comment as "a documented deviation" from Base UI's single `onOpenChange(reason)`, kept because Blazor can't reproduce `event.preventBaseUIHandler()`. WPF *can* generally intercept and cancel routed events natively: worth deciding whether to collapse this back to a single cancelable `Closing`-style event instead of porting the four-callback superset verbatim.

## WPF implementation notes

These notes record the decisions made porting Select to `Navius.Wpf.Primitives`. New files: `Controls/Select/NaviusSelectBase.cs`, `NaviusSelect.cs`, `NaviusSelectItem.cs`, `NaviusSelectEventArgs.cs`, `SelectSelectionEngine.cs`, plus `Themes/Select.xaml`.

### Tier B (custom lookless control), not ComboBox

The "WPF strategy" section above leans toward `ComboBox` as the base, then immediately lists the ways ComboBox fights this contract: no native multi-select at all, no toggle-and-stay-open, and keyboard defaults (`Loop`, land-on-selected-else-first, land-on-first-or-last-from-a-closed-trigger) that would each need to be re-driven through `PreviewKeyDown` anyway. It also grants the escape hatch: "or a fully custom `Selector`-derived control if fidelity with the toggle-and-stay-open semantics matters." I took that hatch uniformly. `NaviusSelectBase` derives from `ItemsControl` (not `Selector`, not `ComboBox`) and owns selection itself: it walks its own item containers and sets a plain `IsSelectedValue` flag on each (mirroring `NaviusRadioGroup.SyncCheckedFromValue`/`FindDescendants<T>`), rather than fighting `ComboBoxItem`/`Selector.IsSelected` plumbing. This is faster and far more deterministic to test than bending ComboBox's internal selection model. The cost is the AutomationPeer gap (see below).

### Generic control, single shared style

`NaviusSelect<TItem>` must be generic, but `DefaultStyleKeyProperty.OverrideMetadata` resolves per **closed** generic type, so `NaviusSelect<string>` and `NaviusSelect<int>` would otherwise each need their own `<Style TargetType>`. Fix: all visual/template-bound state lives on the non-generic `NaviusSelectBase : ItemsControl`, and the generic subclass overrides `DefaultStyleKey` to point at `typeof(NaviusSelectBase)`. `Themes/Select.xaml` therefore has exactly one `<Style TargetType="{x:Type select:NaviusSelectBase}">` that every closed instantiation shares. The generic subclass adds only TItem-typed wrappers (`Value`, `Values`/`SelectedValues`, `ValueSelected`/`ValuesSelected` CLR events) over object-typed base storage (`RawValue`, `RawValues`).

One extra wrinkle: `DefaultStyleKey` only routes the **theme** style (Generic.xaml). Because this wave does not touch Generic.xaml and instead ships `Select.xaml` as an **implicit** style merged into a page's ambient Resources, and WPF resolves implicit styles by the element's runtime type (the closed generic, which never matches the base-typed key), the generic ctor additionally calls `SetResourceReference(StyleProperty, typeof(NaviusSelectBase))`. That makes each closed instantiation pick up the single shared style wherever `Select.xaml` is in scope, and it is harmless (Style stays unresolved) when no such resource is present.

### Part collapse (14 web parts -> 3 WPF types)

- **Root + Trigger + Value + Icon + Portal + Positioner + Popup + Viewport** collapse onto `NaviusSelectBase` and its one `ControlTemplate`. The trigger is `PART_Trigger` (a `ToggleButton` whose `IsChecked` two-way-binds to `IsOpen`); `NaviusSelectValue` is the trigger's label `TextBlock` bound to a computed `DisplayText`; `NaviusSelectIcon` is the chevron `Path` (rotates 180 on open). `Portal` is a no-op (a WPF `Popup` already floats in its own window layer, nothing to teleport) and its `Container`/`KeepMounted` open questions are dropped. `Positioner` folds into `Side`/`Align`/`SideOffset`/`AlignOffset` directly on the control, forwarded to the shared `NaviusAnchoredPopup` (`PART_Popup`) exactly as Popover/Menu do. `Viewport` is a plain `ScrollViewer` around the `ItemsPresenter`.
- **Item + ItemText + ItemIndicator** collapse onto `NaviusSelectItem : ContentControl` (originally `Control`; re-based 2026-07-12 for ItemTemplate support, see "XAML-friendly root + ItemTemplate" below). The label is `TextValue` (also the type-ahead/trigger-label text); the indicator is a check-glyph `Path` shown by a template trigger on `IsSelectedValue`. The three-way `Text > TextValue > raw-value` label chain (an open question above) is resolved to a simpler two-step convention: `DisplayText = TextValue ?? Value?.ToString()`. `ChildContent` (arbitrary item content) is dropped in favor of the string label, consistent with this repo dropping web-only surface (`Attributes`/`Class`). *(Superseded 2026-07-12: arbitrary row content is now available through `ItemTemplate`; see "XAML-friendly root + ItemTemplate" below. The string label remains the default and still powers the trigger label and type-ahead.)*
- **Arrow** is not ported: the wave's hard rule is hairline borders / no shadows / no pointer triangle, and Popover.xaml's `DropShadowEffect` was deliberately **not** copied here. **ScrollUpButton / ScrollDownButton** are not ported: the `ScrollViewer` supplies a standard scrollbar, and the contract itself notes these buttons are decorative (`aria-hidden`) and keyboard users navigate by item roving, not by the buttons. **Group / Label / Separator** are out of scope for this listbox-of-strings port (no grouping in the demo); they can be added later as needed.

### Escape / dismiss strategy

Chosen: **hybrid** rather than a single owner. Escape is handled locally in the control's own `PreviewKeyDown` (the same handler that owns all keyboard), so there is one testable source of truth and it works headlessly with no `Window`. Outside-press dismissal is delegated to `OverlayStack`/`OverlaySession` (as Popover does), because the popup lives in `NaviusAnchoredPopup`'s separate `Popup` HwndSource and only the stack's window-level hook can see a press landing outside it. The session is pushed with `CloseOnEscape = false` (the key handler owns Escape), `CloseOnOutsideClick = true`, `TrapFocus = false` (a listbox uses roving highlight, not a focus trap), `RestoreFocus = false`. Both `PART_PopupContent` **and** `PART_Trigger` are registered as input roots so a press on the trigger counts as "inside" and does not race the trigger's own toggle (otherwise clicking the trigger to close would outside-close then immediately reopen).

### Roving focus adaptation (a real keyboard-table delta)

The contract moves real DOM focus onto the highlighted option. In WPF the popup is a separate HwndSource, and moving focus into it while keeping the `PreviewKeyDown` handler live is fragile. Instead, **focus stays on the trigger and highlight is visual-only**: items are non-focusable (`Focusable=false`), the owner sets a plain `IsHighlightedValue` flag (the contract's `data-highlighted`) on the current option, and every key tunnels through the base control's single handler. This is the one deliberate deviation from the contract's keyboard model; behavior is otherwise faithful.

### Keyboard coverage vs the contract table

Closed trigger: Enter / Space / ArrowDown open and highlight the **first** option; ArrowUp opens and highlights the **last** option; Click toggles (via the `ToggleButton`). Open listbox: ArrowDown/ArrowUp move highlight (clamp when `Loop=false`, the Select default; wrap when `Loop=true`): `Loop` is **fully implemented** here, unlike the Menu port where native ContextMenu gave no wrap toggle; Home/End jump to first/last; a printable character does forward-wrapping first-character type-ahead; Enter/Space on the highlighted option activates it (single-select commits and closes, multi-select toggles and stays open); Escape closes; click on an item activates; click outside closes. On reopen, highlight lands on the currently-selected option, else the first (contract's `OpenFocusMode="selected"`). The one delta is the focus model above (highlight is visual, not a real focus move). Type-ahead is single-character only (no multi-key buffer/timeout), which matches the contract's described "starts with that character" behavior for the common case.

### Cancelable Select event

`Controls/Select/NaviusSelectEventArgs : RoutedEventArgs` with `PreventDefault()` is a **new, distinctly-namespaced** class, not the Menu family's `Controls.Menus.NaviusSelectEventArgs` (same shape, different namespace, no collision, per the assignment). Items raise it on activation, then hand the final args to the owner (a direct stamped back-reference, not routed-event bubbling, so it works headlessly and across the popup's separate HwndSource); the owner runs after any consumer handler and commits only when not prevented, so `PreventDefault()` faithfully keeps the listbox open and skips applying the value.

### Pure engine

`SelectSelectionEngine` holds the toggle/commit/highlight/type-ahead state machines as plain static methods over primitives/collections (`MoveHighlight`, `ToggleMultiple`, `ResolveSingleCommit`, `FindTypeaheadMatch`), unit-tested directly with no WPF Application or STA thread, per the "logic as pure testable methods" rule.

### AutomationPeer gap

Because this is not ComboBox-derived, the free `ComboBoxAutomationPeer`/`ComboBoxItemAutomationPeer` (with `IExpandCollapseProvider`/`ISelectionProvider`/`ISelectionItemProvider`) are unavailable. A minimal `NaviusSelectAutomationPeer` reports `AutomationControlType.ComboBox` for the root and `NaviusSelectItemAutomationPeer` reports `AutomationControlType.ListItem` for items (with the item's label as its name). The root peer does provide the two patterns a reader actually needs: a read-only `IValueProvider` (ValuePattern) surfacing `DisplayText`, and `IExpandCollapseProvider` over `IsOpen` (both wired via `GetPattern`). `ISelectionProvider`/`ISelectionItemProvider` are the remaining gap (a reader cannot enumerate selected items over UIA), noted here rather than burning time on a full selection surface for this wave.

### Web-only params dropped

`Attributes`, `Class`, and native-form mirroring (`NaviusBubbleInput` / hidden `<input>`) are dropped per this repo's precedent. `Name` and `Required` are kept as inert marker properties for API parity only (no validation wiring). `Dir`/RTL, `DefaultValue`/`DefaultOpen` uncontrolled seeds, and the four-callback dismiss superset (`OnEscapeKeyDown`/`OnPointerDownOutside`/`OnFocusOutside`/`OnInteractOutside`) are not ported in this wave; open/value state is controlled via the two-way `IsOpen`/`RawValue`/`RawValues` DPs.

## M6 audit (2026-07-09)

Confirmed issue found + FIXED (doc-vs-code contradiction on AutomationPeer patterns):
- The "WPF implementation notes -> AutomationPeer gap" paragraph originally published "Full UIA pattern providers (ExpandCollapse / Selection / SelectionItem) are **not** implemented." That was FALSE: `NaviusSelectAutomationPeer` (NaviusSelectBase.cs:627-664) implements `IValueProvider` (read-only ValuePattern surfacing `DisplayText`) AND `IExpandCollapseProvider` (over `IsOpen`), both routed through `GetPattern` (NaviusSelectBase.cs:639-643). The code is better than the doc claimed; only `ISelectionProvider`/`ISelectionItemProvider` are genuinely absent.
- Fix: corrected the stale sentence in the AutomationPeer-gap paragraph to describe the real state (ValuePattern + ExpandCollapse present; Selection/SelectionItem the remaining gap). No code change needed - the code was already correct.
- The claimed pattern support was untested. Added regression tests to SelectTests.cs: `AutomationPeer_ExposesReadOnlyValuePattern_SurfacingDisplayText` (IsReadOnly true, Value tracks placeholder then selected text) and `AutomationPeer_ExposesExpandCollapsePattern_TrackingOpenState` (Expand/Collapse toggles `IsOpen`, state reflects it).

Plausible / residual (not fixed): `ISelectionProvider`/`ISelectionItemProvider` remain unimplemented (a reader cannot enumerate selected items over UIA); documented gap, out of scope for this wave. Group/Label/Separator sub-parts, `Dir`/RTL, uncontrolled `DefaultValue`/`DefaultOpen` seeds, and the four-callback dismiss superset remain deliberately unported per the notes above.

Verified TRUE under adversarial check (the "Space is dead" hunt):
- Space is genuinely wired in BOTH states. Closed trigger: Enter/Space/Down open landing on the FIRST option, Up opens landing on LAST (`HandlePreviewKeyDown` NaviusSelectBase.cs:485-502, proven by `ClosedTrigger_ArrowDownOpens_HighlightsFirst`/`ClosedTrigger_ArrowUpOpens_HighlightsLast`). Open listbox: Enter/Space call `_highlighted.RaiseSelectEvent()` -> commit (NaviusSelectBase.cs:522-525), proven by `OpenListbox_ArrowKeysMoveHighlight_ThenEnterCommits`. No dead key found.
- Down/Up (clamp when `Loop=false`, the Select default), Home/End, Escape-close, and first-character type-ahead are all wired and each covered by a passing test. Multi-select toggle-and-stay-open and single-select commit-and-close, plus cancelable `PreventDefault`, all verified. `Loop` default false, `Align` default Start match the contract.
- Root peer reports `AutomationControlType.ComboBox`, item peer `AutomationControlType.ListItem` with `DisplayText` as name - both returned from `OnCreateAutomationPeer` and tested.

## XAML-friendly root + ItemTemplate (2026-07-12)

PR #13 added an object-typed `NaviusSelect` root (no generic type argument, `Value`/`Values`
object-typed) so plain XAML can declare a Select without generics; it shares `NaviusSelectBase`'s
single style like every closed `NaviusSelect<T>`. This maintainer follow-up completes its
data-binding contract:

- **ItemsSource / live refresh.** Native `ItemsControl` machinery: an `INotifyCollectionChanged`
  source regenerates containers on change, and `OnItemsChanged` re-stamps owners, selection state,
  and the trigger label.
- **DisplayMemberPath.** Resolved by reflection into each data-bound container's `TextValue` at
  container preparation. Dotted property paths ("Owner.Name") are supported per WPF's
  `DisplayMemberPath` convention; indexers and attached properties are not. A path change
  re-resolves every data-bound container's label and the trigger label live
  (`OnDisplayMemberPathChanged` override). Containers declared directly as `NaviusSelectItem`s own
  their `TextValue` and are never re-stamped.
- **ItemTemplate / ItemTemplateSelector.** `NaviusSelectItem` now derives from `ContentControl`, so
  standard container preparation stamps `Content`/`ContentTemplate`/`ContentTemplateSelector` onto
  data-bound rows, and the theme swaps the plain `DisplayText` label for a `ContentPresenter`
  whenever a template or selector is present (a `MultiTrigger` on both being null shows the label).
  `DisplayText` (`TextValue ?? Value?.ToString()`) still powers the trigger label and type-ahead
  regardless of template, so templated rows keep working keyboards and readers. Setting both
  `DisplayMemberPath` and `ItemTemplate` throws natively (`ItemsControl`'s own guard).
