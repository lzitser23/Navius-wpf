# ToggleGroup

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusToggleGroup | `<div role="group" data-navius-togglegroup>` | Root: owns the pressed-value set (single or multiple selection), cascades `ToggleGroupContext`, engages the JS roving-focus engine over its items |
| NaviusToggleGroupItem | `<button data-navius-togglegroup-item>` | One toggle button; registers itself with the context, reads pressed/tab-stop state |

## Parameters

### NaviusToggleGroup

| Name | Type | Default | Notes |
|---|---|---|---|
| Multiple | `bool` | false | `true`: many items can be pressed at once; `false` (default): single, radio-like (Base UI's `multiple`) |
| Value | `IReadOnlyList<string>?` | null | Controlled pressed set, paired with `ValueChanged` |
| ValueChanged | `EventCallback<IReadOnlyList<string>>` | | Controlled-ness determined by `ValueChanged.HasDelegate` |
| DefaultValue | `IReadOnlyList<string>?` | null | Uncontrolled initial pressed set |
| Disabled | `bool` | false | Disables every item in the group |
| Orientation | `string` | "horizontal" | Drives the roving-focus arrow-key axis |
| RovingFocus | `bool` | true | `true`: single Tab stop, arrows move roving focus. `false`: every enabled item is `tabindex="0"`, arrow navigation disabled |
| Loop | `bool` | true | Arrow navigation wraps past first/last when true; stops at ends when false |
| Dir | `string?` | null | `"ltr"`/`"rtl"`; falls back to cascaded `NaviusDirection`, then `"ltr"` |
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

### NaviusToggleGroupItem

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | `string` | "" | Identifies the item in the pressed set |
| Disabled | `bool` | false | Per-item disabled; effective disabled is `Disabled || Context.Disabled` |
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

## Events

| Part | Event | Signature |
|---|---|---|
| NaviusToggleGroup | ValueChanged | `EventCallback<IReadOnlyList<string>>`, fired on item click when controlled |

## State + data attributes

| Element | Attribute | Meaning |
|---|---|---|
| Root | `role="group"`, `data-navius-togglegroup`, `data-orientation`, `data-multiple` (present when `Multiple`), `data-disabled`, `dir` | |
| Item | `aria-pressed` (`"true"`/`"false"`), `tabindex` (0 for the roving/tab-stop item, -1 otherwise), native `disabled`, `data-pressed` (present when pressed), `data-disabled`, `data-navius-togglegroup-item` | Note: the item's own code comment claims a `data-state="on"|"off"` attribute, but the rendered markup only carries `data-pressed`/`aria-pressed`; no `data-state` attribute is actually emitted |
| ToggleGroupContext (C# state) | `Multiple`, `Orientation`, `Disabled`, `RovingFocus`, pressed-value `HashSet<string>`, registered items (value/disabled) | Shared cascaded state; `Changed` event drives part re-render |

## Keyboard

| Key | Behavior |
|---|---|
| Space / Enter (on a focused item) | Native `<button>` activation triggers `@onclick` → `Context.RequestToggleAsync(Value)` |
| Arrow keys (when `RovingFocus=true`) | Move DOM focus between enabled items along `Orientation`'s axis; handled entirely by the JS engine (`CreateRovingFocusAsync`) over `[data-navius-togglegroup-item]:not([data-disabled])`, not in C# |
| Home / End (when `RovingFocus=true`) | Jump focus to first/last enabled item (engine-provided) |

`Loop` controls whether the engine's roving controller wraps at the edges. `Dir` is passed into the engine so horizontal arrow semantics flip under RTL. When `RovingFocus=false`, the engine is never engaged (`OnAfterRenderAsync` only creates it when `RovingFocus` is true), so every enabled item is simply `tabindex="0"` and arrow keys do nothing beyond native browser behavior.

Selection logic (`ComputeNext`): in single mode (`Multiple=false`), clicking the already-pressed item clears the selection entirely (toggles off, none pressed); clicking a different item replaces the selection with just that value. In multiple mode, clicking toggles that value's membership in the set independently of others.

## Accessibility

- Root: `role="group"`.
- Item: `aria-pressed` reflects on/off state.
- Roving tabindex: `IsTabStop` determines the single seated Tab stop as the first pressed-and-enabled item, falling back to the first enabled item, until the user navigates (after which the JS roving-focus engine owns which item currently has `tabindex="0"`).
- Disabled items are excluded from the roving selector (`:not([data-disabled])`) and get native `disabled`, removing them from tab order entirely.
- When `RovingFocus=false`, every enabled item is independently tabbable (`tabindex="0"`), useful when the group should not behave like a single composite widget for keyboard users.

## WPF strategy

Tier A: derive from a `ToggleButton`-based item inside a WPF `ListBox`-like container, or more precisely model on `System.Windows.Controls.Primitives.Selector` semantics: single mode maps closely to `ListBox` with `SelectionMode="Single"` (deselectable, unlike native `RadioButton` groups which cannot go empty) and multiple mode to `SelectionMode="Multiple"`/`Extended`, with each item a `ToggleButton`-styled `ListBoxItem`. WPF's built-in `KeyboardNavigation` already provides roving-tabindex-equivalent arrow navigation within a `ListBox`/`ItemsControl`, replacing the JS `createRovingFocus` engine; `Loop` maps to a custom `KeyDown` wrap-around handler layered on top since native WPF navigation does not wrap by default. `AutomationPeer`: `ListBoxAutomationPeer`/`ListBoxItemAutomationPeer` exposes UIA `SelectionPattern`, which is a reasonable analogue to `role="group"` + `aria-pressed`, though not an exact 1:1 (ARIA has no native "toggle group" role either, so both platforms are approximating). `data-multiple`/`data-orientation`/`data-disabled` become dependency properties driving `Style` triggers.

## Open questions

- Single-mode "click the pressed item to clear" (deselect-to-empty) is not `RadioButton`'s native behavior and not `ListBox` `SelectionMode="Single"`'s either (clicking the selected item is normally a no-op); the WPF port needs an explicit override for this, confirm it's wanted.
- `RovingFocus=false` (every item independently tabbable, no arrow-key composite-widget behavior) has no direct `ListBox` equivalent since `ListBox` always behaves as one composite widget for keyboard purposes; may require a plain `ItemsControl`-of-`ToggleButton`s path instead for that mode.
- Confirm whether WPF's native `KeyboardNavigation.DirectionalNavigation` is a sufficient replacement for the engine's `createRovingFocus`, or whether custom `PreviewKeyDown` handling is needed for parity (especially the `Loop` wrap and RTL flip).
