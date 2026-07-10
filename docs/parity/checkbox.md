# Checkbox

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| `NaviusCheckbox` | `<button type="button" role="checkbox">` (plus a hidden `<input type="checkbox">` via `NaviusBubbleInput` when `Name` is set and it is not a `Parent` checkbox) | Tri-state checkbox root: owns checked/indeterminate state, toggles on click, cascades `CheckboxContext` to its indicator |
| `NaviusCheckboxIndicator` | `<span>` | Visual check/minus glyph, mounted only when checked or indeterminate (or `KeepMounted`) |
| `NaviusCheckboxGroup` | `<div role="group">` | Groups child checkboxes; owns the set of checked child `Name`s and drives a `Parent="true"` select-all checkbox's roll-up state |

## Parameters

### NaviusCheckbox

| Name | Type | Default | Notes |
|---|---|---|---|
| `Checked` | `bool` | `false` | Controlled boolean checked state (`@bind-Checked`) |
| `CheckedChanged` | `EventCallback<bool>` | none | Pairs with `Checked` |
| `DefaultChecked` | `bool` | `false` | Uncontrolled initial boolean state |
| `CheckedState` | `bool?` | `none` | Controlled tri-state value; `null` = indeterminate; takes precedence over `Checked` (`@bind-CheckedState`) |
| `CheckedStateChanged` | `EventCallback<bool?>` | none | Pairs with `CheckedState` |
| `DefaultCheckedState` | `bool?` | `none` | Uncontrolled initial tri-state value |
| `Disabled` | `bool` | `false` | |
| `ReadOnly` | `bool` | `false` | Focusable but value cannot be changed |
| `Required` | `bool` | `false` | Marks the bubble input required for native form validation |
| `Parent` | `bool` | `false` | Inside a `NaviusCheckboxGroup`: marks this as the select-all checkbox rolling up over `AllValues`; ignored outside a group |
| `Name` | `string?` | `none` | Group membership identity; standalone also renders the hidden bubble input |
| `Value` | `string` | `"on"` | Submitted form value when checked |
| `ChildContent` | `RenderFragment?` | `none` | |
| `Attributes` | `IDictionary<string, object>?` | `none` | Captures unmatched attributes (`CaptureUnmatchedValues`) |

### NaviusCheckboxIndicator

| Name | Type | Default | Notes |
|---|---|---|---|
| `KeepMounted` | `bool` | `false` | Keep the indicator mounted (hidden) even when unchecked |
| `ChildContent` | `RenderFragment?` | `none` | |
| `Attributes` | `IDictionary<string, object>?` | `none` | Captures unmatched attributes |

### NaviusCheckboxGroup

| Name | Type | Default | Notes |
|---|---|---|---|
| `Value` | `IReadOnlyList<string>?` | `none` | Controlled set of checked child names (`@bind-Value`) |
| `ValueChanged` | `EventCallback<IReadOnlyList<string>>` | none | Pairs with `Value` |
| `DefaultValue` | `IReadOnlyList<string>?` | `none` | Uncontrolled initial set of checked child names |
| `AllValues` | `IReadOnlyList<string>?` | `none` | Every child name in the group; required to drive a `Parent` checkbox |
| `Disabled` | `bool` | `false` | Disables every checkbox in the group |
| `ChildContent` | `RenderFragment?` | `none` | |
| `Attributes` | `IDictionary<string, object>?` | `none` | Captures unmatched attributes |

## Events

| Part | Event | Payload |
|---|---|---|
| `NaviusCheckbox` | `CheckedChanged` | `bool` |
| `NaviusCheckbox` | `CheckedStateChanged` | `bool?` |
| `NaviusCheckboxGroup` | `ValueChanged` | `IReadOnlyList<string>` |

## State + data attributes

`NaviusCheckbox` (button):
- `data-checked` (present when `CurrentChecked == true`)
- `data-unchecked` (present when `CurrentChecked == false`)
- `data-indeterminate` (present when `CurrentChecked is null`)
- `data-disabled`, `data-readonly`, `data-required`
- `data-navius-checkbox` (marker attribute)
- `aria-checked` mirrors state (`"true"` / `"false"` / `"mixed"`)

`NaviusCheckboxIndicator` (span): same discrete set (`data-checked` / `data-unchecked` / `data-indeterminate` / `data-disabled` / `data-readonly` / `data-required`) plus `data-navius-checkbox-indicator`.

`NaviusCheckboxGroup` (div): `data-disabled` only (never `data-state`), plus `data-navius-checkbox-group`.

`CheckboxContext` (internal shared state class): `Checked` (`bool?`), `Disabled`, `ReadOnly`, `Required`, `IsPresent` (`true` when checked or indeterminate).

`CheckboxGroupContext` (internal shared state class): `Disabled`, `Value` (checked names), `AllValues`, `IsChecked(name)`, `ParentState` (`bool?` roll-up: `true` all checked, `false` none checked, `null` some checked).

## Keyboard

No custom keydown handler is implemented for `NaviusCheckbox`; it renders a native `<button type="button">`, so Space/Enter activation (click) comes for free from browser button semantics rather than an explicit handler in code.

| Key | Behavior |
|---|---|
| Space / Enter | Activates via native `<button>` click semantics (toggles the checkbox); no custom `@onkeydown` handler exists in the code |

## Accessibility

- `role="checkbox"` on the root button
- `aria-checked` set to `"true"` / `"false"` / `"mixed"` based on `CurrentChecked`
- `aria-readonly="true"` when `ReadOnly`
- `aria-required="true"` when `Required`
- `NaviusCheckboxGroup` renders `role="group"`
- No explicit focus-management code (relies on native button focus/tab order); no `aria-labelledby`/`aria-controls` wiring in this family

## WPF strategy

Tier A (derive from native `System.Windows.Controls.CheckBox`). WPF's `CheckBox` already models tri-state via `IsThreeState` + nullable `IsChecked`, which maps directly onto `CheckedState` (`bool?`, `null` = indeterminate); `Checked`/`DefaultChecked` map onto the boolean overload. `role="checkbox"` + `aria-checked` map to `CheckBoxAutomationPeer` with `TogglePattern` (WPF's built-in peer already reports `ToggleState.Indeterminate` for null). `NaviusCheckboxGroup`'s roll-up-over-membership model (`Value` as a set of checked names, `AllValues`, `Parent` select-all checkbox) has no native WPF group control; it must be reimplemented as a custom `ItemsControl`-based container coordinating child `CheckBox.IsChecked` bindings, since WPF has no built-in "checkbox group with derived select-all indeterminate" concept. The bubble `<input type="checkbox">` (hidden native form mirror for HTML form submission) has no WPF equivalent and should simply be dropped, since WPF has no HTML form submission model.

## Open questions

- Should the WPF port keep both a boolean (`IsChecked`) and tri-state (`CheckedState`) parameter pair, or collapse to WPF's single nullable `IsChecked` (three-state) property, given `CheckedState` already subsumes `Checked`?
- Should `NaviusCheckboxGroup`'s `Parent`/select-all roll-up be a first-class WPF control, or is it expected to be composed by app code using individual `CheckBox` bindings and a converter?
- Is the "toggling from indeterminate goes to checked" rule (seen in `ToggleAsync`) expected to be preserved exactly, given WPF's native tri-state `CheckBox` cycles unchecked to checked to indeterminate to unchecked by default?

## WPF implementation notes

- Implemented under `Controls/Checkbox/`: `NaviusCheckbox` (`CheckBox`), `NaviusCheckboxIndicator` (`ContentControl`), `NaviusCheckboxGroup` (`ContentControl`, Tier B).
- Resolved open question 1: collapsed `Checked`/`CheckedState` onto WPF's single nullable `CheckBox.IsChecked`; `IsThreeState = true` is set in the constructor so indeterminate is reachable programmatically.
- Resolved open question 3: `NaviusCheckbox.OnToggle()` is overridden to always binary-toggle (`IsChecked = IsChecked != true`), replacing WPF's native 3-way click cycle. This means indeterminate is reachable only by setting `IsChecked = null` in code (e.g. the group's roll-up), never via a user click, exactly matching the source's `ToggleAsync` rule.
- `ReadOnly` and `Required` are new dependency properties (no native WPF equivalent); `ReadOnly` is enforced inside the `OnToggle()` override so the control stays focusable (no `IsEnabled` change) but the value cannot change. `Disabled` was **not** added as a new property: it maps onto the inherited `IsEnabled`, which WPF already cascades to descendants for free, so `NaviusCheckboxGroup.Disabled` needed no extra code.
- `GroupValue` (string?) and `IsSelectAll` (bool) are new properties on `NaviusCheckbox` that replace the contract's `Name`/`Parent`. Renaming was required, not stylistic: `Name` collides with `FrameworkElement.Name` (a reserved CLR member tied to `x:Name`/`NameScope`), and `Parent` collides with the read-only `Visual.Parent` member.
- Resolved open question 2: `NaviusCheckboxGroup` is a first-class Tier B `ContentControl` (not a converter-composed pattern). It has no `ItemsControl`/items-source model; like the source (a `div` wrapping a `RenderFragment`), it wraps arbitrary XAML content and discovers descendant `NaviusCheckbox` instances via a logical-tree walk, listening for `ToggleButton.Checked`/`Unchecked` bubbling to update `Value` and roll up the `IsSelectAll` checkbox's tri-state.
- `NaviusCheckboxIndicator` is a real composable part (not folded into the checkbox's own template glyph): it has `IsChecked`/`KeepMounted` and toggles its own `Visibility`, wired into `NaviusCheckbox`'s default template via a `TemplateBinding` of `IsChecked`.
- Dropped: the hidden `<input type="checkbox">` bubble input (`Name`/`Value`/form submission) and `Attributes` splat, per the porting brief (no WPF form-submission model).
- Not implemented: a custom `AutomationPeer` for `NaviusCheckbox` exposing `aria-readonly`/`aria-required` (the native `CheckBoxAutomationPeer` already covers tri-state via `ToggleState.Indeterminate`, but has no read-only/required surface); flagged as a gap rather than built, to keep scope proportional. `NaviusCheckboxGroup` does get a minimal custom peer (`AutomationControlType.Group`) since its native `ContentControl` peer has no group semantics at all.

## M6 audit (2026-07-09)

Adversarially re-verified `NaviusCheckbox`/`NaviusCheckboxIndicator`/`NaviusCheckboxGroup` against
this doc's claims: the `IsThreeState=true` + `OnToggle()` override that replaces WPF's native
3-way click cycle with the contract's binary-toggle rule (indeterminate reachable only
programmatically), `ReadOnly` staying focusable while blocking value changes, the
`GroupValue`/`IsSelectAll` renames (and the stated reason -- `Name`/`Parent` collide with
`FrameworkElement.Name`/`Visual.Parent`), the group's routed-event-bubbling roll-up (`Checked`/`Unchecked`,
guarded by `_isSyncing` against reentrancy, and correctly NOT re-triggered by the select-all
checkbox's own `Indeterminate` transitions since only `Checked`/`Unchecked` are subscribed), and the
indicator's `Visibility` toggle all check out against the code and `CheckboxTests.cs`. Theme
(`Themes/Checkbox.xaml`) uses only `DynamicResource` tokens, no hardcoded colors. No confirmed or
plausible disparities found.
