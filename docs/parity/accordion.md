# Accordion

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusAccordion | `<div>` | Root; owns open-value state (controlled/uncontrolled), cascades `AccordionContext` |
| NaviusAccordionItem | `<div>` | One accordion section; cascades its `Value` + disabled state to header/trigger/panel |
| NaviusAccordionHeader | `<h1>`-`<h6>` (dynamic, default `<h3>`, driven by `Level`) | Heading wrapper around the Trigger, built via `RenderTreeBuilder` for a dynamic tag |
| NaviusAccordionTrigger | `<button type="button">` | Toggles its item's open state; owns keyboard roving focus |
| NaviusAccordionPanel | `<div>` (conditionally rendered via `@if (_rendered)`) | The collapsible content region, `role="region"` |

## Parameters

### NaviusAccordion

| Name | Type | Default | Notes |
|---|---|---|---|
| Type | string | `"single"` | `"single"` or `"multiple"` |
| Value | string? | none | Controlled open value for `Type="single"` |
| Values | IEnumerable<string>? | none | Controlled open values for `Type="multiple"` |
| ValueChanged | EventCallback<string?> | none | Fires with new open value (single mode); enables `@bind-Value` |
| ValuesChanged | EventCallback<IEnumerable<string>> | none | Fires with new open values (multiple mode); enables `@bind-Values` |
| DefaultValue | string? | none | Uncontrolled initial open value (single mode) |
| DefaultValues | IEnumerable<string>? | none | Uncontrolled initial open values (multiple mode) |
| Collapsible | bool | `false` | Single mode only; when false the sole open item cannot close itself (the spec default) |
| Disabled | bool | `false` | Disables the whole accordion; cascades to every item |
| Orientation | string | `"vertical"` | `"vertical"` or `"horizontal"`; drives arrow-key direction + `data-orientation` |
| Dir | string? | none | `"ltr"` \| `"rtl"`; reverses horizontal arrow mapping under rtl; falls back to cascaded `NaviusDirection` |
| ChildContent | RenderFragment? | none | |
| Attributes | IDictionary<string, object>? | none | Captured unmatched values, forwarded to the root `<div>` |

### NaviusAccordionItem

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | string | `""` | Unique identifier for this section |
| Disabled | bool | `false` | Disables this section's trigger (can't toggle or be focused) |
| ChildContent | RenderFragment? | none | |
| Attributes | IDictionary<string, object>? | none | Captured unmatched values, forwarded to the item `<div>` |

### NaviusAccordionHeader

| Name | Type | Default | Notes |
|---|---|---|---|
| Level | int | `3` | Heading level 1-6 (renders `<h1>`-`<h6>`, clamped) |
| ChildContent | RenderFragment? | none | Should be the Trigger |
| Attributes | IDictionary<string, object>? | none | Captured unmatched values, forwarded to the heading element |

### NaviusAccordionTrigger

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | RenderFragment? | none | |
| Attributes | IDictionary<string, object>? | none | Captured unmatched values, forwarded to the `<button>` |

### NaviusAccordionPanel

| Name | Type | Default | Notes |
|---|---|---|---|
| KeepMounted | bool | `false` | Keep the panel mounted (hidden) while closed instead of removing it from the DOM |
| ChildContent | RenderFragment? | none | |
| Attributes | IDictionary<string, object>? | none | Captured unmatched values, forwarded to the panel `<div>` |

## Events

| Part | Event | Signature |
|---|---|---|
| NaviusAccordion | ValueChanged | `EventCallback<string?>` (single mode) |
| NaviusAccordion | ValuesChanged | `EventCallback<IEnumerable<string>>` (multiple mode) |

No other part exposes an `EventCallback<T>` parameter (Item/Header/Trigger/Panel interact with open state only via the cascaded `AccordionContext`, not their own events).

## State + data attributes

| Attribute | Part | Notes |
|---|---|---|
| `data-navius-accordion` | Root | Marker |
| `data-orientation` | Root, Panel | `"vertical"` or `"horizontal"` (`AccordionContext.DataOrientation`) |
| `data-disabled` | Root, Item, Header, Trigger, Panel | Empty string when disabled, else omitted |
| `dir` | Root | Set to the effective dir unless `"ltr"`/empty |
| `data-navius-accordion-item` | Item | Marker |
| `data-open` | Item, Header, Trigger (`data-panel-open` on Trigger), Panel | Empty string when the item is open, else omitted |
| `data-index` | Item, Header, Trigger (via `Context.IndexOf`), Panel | Zero-based DOM-order index of the item's registered trigger |
| `data-navius-accordion-header` | Header | Marker |
| `data-navius-accordion-trigger` | Trigger | Marker |
| `data-panel-open` | Trigger | Empty string when open (Trigger's own name for the open flag) |
| `role="region"` | Panel | ARIA role, see Accessibility |
| `data-closed` | Panel | Empty string when not open |
| `data-starting-style` | Panel | Empty string for one frame while entering (transition-phase attr) |
| `data-ending-style` | Panel | Empty string while the exit transition runs |
| `data-navius-accordion-panel` | Panel | Marker |

`AccordionContext` (public surface): `Type`, `Collapsible`, `RootDisabled`, `Orientation`, `Dir`, `BaseId`, `IsOpen(value)`, `DataOrientation`, `TriggerId(value)`, `ContentId(value)`, `IsItemDisabled(itemDisabled)`, `ToggleAsync(value, itemDisabled)`, `Register`/`Unregister`/`IndexOf` (roving-focus registry), `HandleKeyDownAsync(current, key)`, and the `Changed` event parts subscribe to for re-render.

## Keyboard

Handled in `NaviusAccordionTrigger.OnKeyDownAsync` (dispatches to `AccordionContext.HandleKeyDownAsync`):

| Key | Behavior |
|---|---|
| ArrowUp | (vertical orientation) Move focus to the previous enabled trigger, wrapping |
| ArrowDown | (vertical orientation) Move focus to the next enabled trigger, wrapping |
| ArrowLeft | (horizontal orientation) Move focus to the previous enabled trigger in visual order, reversed under `dir="rtl"`, wrapping |
| ArrowRight | (horizontal orientation) Move focus to the next enabled trigger in visual order, reversed under `dir="rtl"`, wrapping |
| Home | Move focus to the first enabled trigger |
| End | Move focus to the last enabled trigger |
| Space / Enter | Toggles the item; handled natively by the `<button>` element (`@onclick`), not via explicit key-code branching |

Disabled triggers are skipped when computing "enabled" targets for arrow/Home/End navigation; disabled triggers cannot receive focus via these keys and `disabled="@IsDisabled"` blocks native tab/click activation.

## Accessibility

- Trigger: `aria-expanded` (`"true"`/`"false"`), `aria-controls` pointing at the panel id, `aria-disabled` (`"true"` when disabled, else omitted), native `disabled` attribute mirrors `IsDisabled`.
- Panel: `role="region"`, `aria-labelledby` pointing at the trigger id, `id` set to `Context.ContentId(Value)`, `hidden` attribute applied once fully closed and settled (never while animating) unless `KeepMounted`.
- Header renders a real heading element (`<h1>`-`<h6>`, default `<h3>`) so the document outline stays correct; level is overridable via `Level`.
- Ids are derived deterministically from the item value via `TriggerId`/`ContentId` (based on `AccordionContext.BaseId`, a per-root GUID), so trigger/panel ARIA wiring needs no explicit registration step beyond the roving-focus trigger registry.
- Closed panels are removed from the DOM (unless `KeepMounted`) after the exit animation finishes, per the "shared presence pattern" (ADR-0007) referenced in the Panel's doc comment; no explicit focus-trap or focus-restoration logic exists in this family (it is not a modal/overlay component).

## WPF strategy

Tier A (derive from `System.Windows.Controls.Expander` composed via an `ItemsControl`, or a lookless `HeaderedContentControl`-based custom control). Model the Root as an `ItemsControl`/`Selector`-like container owning the open-value set (single vs multiple mirrors WPF's `TreeView`/`TabControl` single-selection vs a custom multi-expand collection), each Item as an `Expander`-derived lookless control (`IsExpanded` bound to open state, `Header` template hosting the Trigger content), and the Trigger as the `Expander`'s built-in toggle (a `ToggleButton` in its default template) so `ExpandCollapsePattern` (`aria-expanded` -> `ExpandCollapseState`) is native. `role="region"` on the Panel maps to `AutomationProperties.AutomationId`/`LandmarkType` region via a custom `AutomationPeer` since `Expander`'s stock peer does not expose a region landmark. Arrow-key roving focus across triggers needs a custom `KeyboardNavigationMode`/manual `Focus()` dispatch (WPF's `Expander` does not natively rove focus this way) since focus is currently at the trigger level via manual `ElementReference.FocusAsync`. The size-observer-driven CSS enter/exit transition (`--accordion-panel-width/height`, `data-starting-style`/`data-ending-style`) does not translate directly and should become a WPF `DoubleAnimation` on `Height`/`RowDefinition.Height`, likely reusing `Expander`'s own expand/collapse visual states if a `Style` override suffices, else a custom `ControlTemplate` with a `VisualStateManager`.

## Open questions

- Whether "multiple" mode maps better to a collection of independent `Expander`s (each with its own `IsExpanded`) or a genuinely custom lookless control that owns a shared open-set, given WPF has no first-class multi-expand accordion primitive.
- Whether the JS `SizeObserver`-driven natural-size publishing (used for smooth height/width transitions) is even necessary in WPF, where `Grid.Height`/`Auto` sizing with a `DoubleAnimation` can measure desired size directly without a ResizeObserver equivalent.
- How `KeepMounted` (panel stays in the tree but hidden) should map: WPF `Visibility.Collapsed` vs `Visibility.Hidden` have different layout/automation implications, and the choice affects whether `Expander.Content` virtualization is desired.
- Whether the dynamic heading level (`Level` 1-6, `<h1>`-`<h6>`) has any meaningful WPF equivalent, since WPF has no native "heading" element/semantics comparable to HTML's document outline; this may become a no-op or purely an `AutomationProperties.HeadingLevel` (UIA 4159+) setting.

## WPF implementation notes

Implemented as a lookless Tier B family: `Controls/Accordion/NaviusAccordion.cs` (root), `NaviusAccordionItem.cs`, `NaviusAccordionHeader.cs`, `NaviusAccordionTrigger.cs`, `NaviusAccordionPanel.cs`, styled by `Themes/Accordion.xaml`.

- Open question 1 (single vs. genuinely custom multi-expand) resolved in favor of a custom control: `NaviusAccordion` is a lookless `ContentControl` root that owns either one `Value` string (Type="single") or a `Values` list (Type="multiple") directly, following the "root owns state, discovers descendants via the logical tree" shape already used by `NaviusRadioGroup`, rather than composing per-item `Expander`s.
- Collapsible reuse: literal control composition (Accordion wrapping `NaviusCollapsible` instances per item) was rejected. `NaviusAccordionPanel` additionally needs role-region-ish automation semantics and per-item disabled/index cascading that would be awkward to thread through Collapsible's simpler two-state context. Instead, only the open/close height-animation logic is shared, factored into an internal `PanelHeightAnimator` helper (`Controls/Internal/PanelHeightAnimator.cs`) used by both `NaviusCollapsiblePanel` and `NaviusAccordionPanel`.
- Open question 2 (SizeObserver) resolved: not needed. `PanelHeightAnimator` measures natural height directly via `UIElement.Measure`, no resize-observer equivalent required.
- Open question 3 (KeepMounted mapping) resolved: `Visibility.Collapsed` is used uniformly for the closed state (no `Hidden` vs. `Collapsed` distinction). `KeepMounted=false` (default) additionally caches and clears `Content` once the close animation's `Completed` callback fires, approximating DOM removal; `KeepMounted=true` leaves `Content` in place and only toggles `Visibility`.
- The "exit completes before collapse" transition-phase rule is reproduced via `DoubleAnimation.Completed` (the same mechanism answering Collapsible's equivalent open question); no two-phase starting-style mount frame is needed since Height animates from a hard 0 rather than a CSS starting-style.
- Loop is not exposed as a property: the contract's own accordion keyboard table always wraps ("Move focus to the previous enabled trigger, wrapping") with no non-looping variant described, so arrow navigation here wraps unconditionally.
- Open question 4 (Level / heading semantics) resolved: the dynamic `<h1>`-`<h6>` element swap is dropped (no WPF document-outline equivalent); `Level` instead drives `AutomationProperties.HeadingLevelProperty` (UIA `AutomationHeadingLevel.Level1`..`Level6`) on `NaviusAccordionHeader`.
- `role="region"` on the Panel is approximated as plain `AutomationControlType.Group` on a custom automation peer; WPF's `AutomationControlType` enum has no direct "region" value, and this is recorded as a known gap rather than a false claim of an exact landmark match.
- `data-index` is implemented literally as an `Index` int property on `NaviusAccordionItem`, recomputed by the root on every open-state sync.
- Disabled is not reimplemented as its own named parameter: each Item's native `IsEnabled` is reused, but WPF's `IsEnabled` does **not** automatically cascade through a `ContentControl`'s logical `Content` (verified empirically, see collapsible.md's notes). `SyncDescendants` explicitly pushes each Item's `IsEnabled` down onto its Trigger and re-subscribes to `IsEnabledChanged` so later toggles keep propagating, matching "Disabled cascades to every item."

## M6 audit (2026-07-09)

**CONFIRMED, fixed.** `Controls/Accordion/NaviusAccordionItem.cs`'s own doc comment directly
contradicted `NaviusAccordion.cs`'s doc comment (and this family's own implementation): the Item
file claimed IsEnabled "cascades to its Trigger... via WPF's native IsEnabled property-value
inheritance," while the Accordion root's file (correctly) says the opposite -- that native
`IsEnabled` does NOT auto-cascade through a `ContentControl`'s logical `Content`, which is why
`SyncDescendants` explicitly pushes it down. The actual runtime behavior was always correct (via
`SyncDescendants`); only the Item's comment was wrong. Fixed by correcting the comment in
`NaviusAccordionItem.cs` to match the accurate claim already in `NaviusAccordion.cs` and
`collapsible.md`. No behavioral change, no new test needed (existing `Disabled_CascadesFromItem_BlocksToggle`
in `AccordionTests.cs` already covers the real behavior).

Otherwise re-verified adversarially and found sound: the full keyboard table (ArrowUp/Down/Left/Right
with rtl flip, Home/End, always-wrap per the contract's own table), the dual-pattern
`NaviusAccordionTriggerAutomationPeer` (Invoke + ExpandCollapse), the Header's
`AutomationProperties.HeadingLevel` mapping (Level1-6), the Panel's `Group`-type peer approximating
`role="region"` (honestly disclosed as a known gap, not a false claim), and the
`PanelHeightAnimator`/`KeepMounted` mount-lifecycle claims. No hardcoded colors found in
`Themes/Accordion.xaml` beyond `DynamicResource` tokens (not independently re-checked line-by-line
in this pass beyond the structural template, which matches the doc's part list).

## Post-release fixes (2026-07-11)

- **Disabled trigger now refuses UIA activation (PR #3).** `IInvokeProvider.Invoke` and
  `IExpandCollapseProvider.Expand`/`Collapse` on `NaviusAccordionTriggerAutomationPeer` previously
  raised `Click` regardless of `IsEnabled`, so a disabled accordion section could still be toggled
  through UIA. All three now throw `ElementNotEnabledException` when the trigger is disabled.
- **UIA Invoke now queues via the dispatcher; ExpandCollapse stays synchronous (PR #9, #10).**
  `Invoke` queues activation with `Dispatcher.BeginInvoke(DispatcherPriority.Input, ...)`, satisfying
  the UIA contract that `IInvokeProvider.Invoke` return immediately. `Expand`/`Collapse` are
  deliberately left synchronous: the `IExpandCollapseProvider` contract requires the requested state
  to be reached before the call returns, so these call the activation hook directly instead of
  queuing it.
- **UIA activation now executes the bound Command (PR #9).** Both paths route through a new internal
  `AutomationInvoke() => OnClick()` hook rather than a bare `RaiseEvent(ButtonBase.ClickEvent)`. The
  bare `RaiseEvent` raised `Click` but skipped `ButtonBase.OnClick`, where command execution lives, so
  a `Command` bound to the trigger never ran when activated via UIA; it now does, exactly once per
  activation.
