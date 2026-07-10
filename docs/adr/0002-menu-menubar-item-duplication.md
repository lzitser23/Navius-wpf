# ADR-0002: Menu/Menubar item-type duplication

Status: proposed

## Context

`Controls/Menu/` (namespace `Navius.Wpf.Primitives.Controls.Menus`) and `Controls/Menubar/`
(namespace `Navius.Wpf.Primitives.Controls.Menubar`) each ship their own leaf/checkbox/radio/
separator/label item types, all Tier A (deriving from the native `MenuItem`/`Separator`). On
the surface they look like straight duplicates: `NaviusMenuItem` / `NaviusMenubarItem`,
`NaviusMenuCheckboxItem` / `NaviusMenubarCheckboxItem`, `NaviusMenuRadioItem` /
`NaviusMenubarRadioItem`, plus a group-label type in each family. This ADR was requested to
assess whether the two families can safely consolidate (Menubar's items subclassing or
aliasing Menu's) without changing either family's public API shape or breaking either family's
tests.

Reading both families in full turned up genuine, deliberately-reasoned behavioral differences,
not copy-paste duplication:

1. **Event model.** `Controls/Menu`'s three item types share `NaviusMenuItemBase`, which raises
   a bubbling `RoutedEvent` (`Select`, carrying a `NaviusSelectEventArgs` with
   `IsDefaultPrevented`/`PreventDefault()`). `Controls/Menubar`'s item types have no shared base
   and each declares its own plain CLR `EventHandler<NaviusMenubarSelectEventArgs>` `Select`
   event, cancelled via a `bool Cancel` field. These are different event types with different
   cancel-signalling members (`IsDefaultPrevented` vs. `Cancel`); a consumer's handler code is
   not interchangeable between the two.

2. **Close/command semantics on activation.** `NaviusMenuItem.OnClick` never calls
   `base.OnClick()`; it always raises `Select`, executes any bound `Command` itself
   (`ExecuteCommand()`, handling `RoutedCommand` vs. plain `ICommand` explicitly because
   `Keyboard.FocusedElement` is unreliable in this environment), and only then closes the owning
   `ContextMenu` if `Select` wasn't cancelled. `NaviusMenubarItem.OnClick` raises `Select`, and
   if not cancelled, defers entirely to `base.OnClick()` (letting native `MenuItem` execute the
   command and close the menu chain); if cancelled, it raises `Click` manually and stops. The two
   control-flow shapes are inverted, not just differently named.

3. **Checkbox API shape.** `NaviusMenuCheckboxItem` exposes both `Checked` (`bool?`) and a
   separate public `IsIndeterminate` (`bool`) DP, driven by an `OnCheckedChanged` callback.
   `NaviusMenubarCheckboxItem` exposes only `Checked`; indeterminate is read directly off the
   nullable `Checked` value by the template (same `{x:Null}`-trigger technique as
   `Themes/Checkbox.xaml`), with no `IsIndeterminate` member at all. Subclassing one from the
   other would either add or remove a public member, which is a real API-shape change even
   though it happens to be additive in one direction.

4. **Radio-group coordination.** `NaviusMenuRadioItem` uses a plain `GroupName` string and
   enforces mutual exclusion by walking `ItemsControl.ItemsControlFromItemContainer` siblings
   at click time. `NaviusMenubarRadioItem` instead points a `Group` property at an explicit
   `NaviusMenubarRadioGroup` coordinator object (its own `DependencyObject` with a `Value` DP
   and a `ValueChanged` event), and additionally makes `Value` *required* (throws in
   `OnInitialized` if null/empty) - Menu's `Value` has no such requirement and defaults to
   `string.Empty`. These are different mechanisms with different property types
   (`string GroupName` vs. `NaviusMenubarRadioGroup? Group`), not an accidental divergence: both
   files' doc comments independently explain why a wrapping `ItemsControl`-based group breaks
   native keyboard roving in their respective host control, and arrive at different answers
   (sibling GroupName string for Menu/ContextMenu's popup nesting, an out-of-tree coordinator
   object for Menubar's top-level `Menu` control).

5. **Group-label mechanism.** `NaviusMenuGroupLabel` is a Tier B `ContentControl` (not a
   `MenuItem` at all); `NaviusMenubarLabel` is a Tier A `MenuItem` subclass
   (`Focusable = false`, `IsHitTestVisible = false`) with its own custom `AutomationPeer`
   (`NaviusMenubarLabelAutomationPeer`, reporting `AutomationControlType.Text`). Menu's version
   has no automation peer override at all. These are different base types with different
   automation contracts; forcing one to alias the other changes both families' automation
   surface.

6. **Separator.** `Controls/Menu` has no separator subclass at all (the parity doc notes plain
   `System.Windows.Controls.Separator` is reused as-is). `Controls/Menubar` has its own
   `NaviusMenubarSeparator : Separator`, whose only purpose is a distinct `DefaultStyleKey` so
   `Themes/Menubar.xaml` can target it independently of `Themes/Separator.xaml`. This one is the
   closest thing to "pure duplication" in the two families, but it isn't cross-family
   duplication to resolve (Menu simply doesn't have an equivalent type to consolidate with).

## Decision

Do not consolidate `Controls/Menu` and `Controls/Menubar` item types this wave. The two
families were independently, deliberately designed around how nesting and keyboard roving work
in their respective host controls (`ContextMenu`/popup submenus vs. the top-level `Menu`
control), and differ in event type, cancel-signalling member, checkbox API surface, radio-group
mechanism and required-ness, and label base type/automation peer. A subclass-or-alias
consolidation as scoped ("without changing public API shape or breaking either family's tests")
is not achievable given these differences: harmonizing them would require a coordinated public
API change to at least one family (most likely Menubar's event model moving to the
`RoutedEvent`/`NaviusMenuItemBase`-style cancelable pattern, and a decision on whether
`Checked`/`IsIndeterminate` and `GroupName`/`Group` should unify), which is a breaking-API
redesign, not a same-wave safe dedup.

If consolidation is revisited later, the recommended path is: first harmonize the event
contract (adopt `NaviusMenuItemBase`'s bubbling cancelable `Select` for Menubar, since it's the
more capable and already-shared pattern), decide on one checkbox tri-state shape and one
radio-group mechanism, and only then evaluate whether a shared base class is warranted - as a
deliberate breaking-version change with both families' test suites updated together, not a
quiet subclass introduced under an "additive only" constraint.

## Consequences

- `Controls/Menu/` and `Controls/Menubar/` remain independent, Tier A, with genuine (not
  accidental) behavioral divergence. No code changes were made as part of this ADR.
- The apparent duplication should not be flagged again as a quick-win cleanup without also
  proposing the event-model harmonization above; a naive subclass attempt will break one
  family's public API or its tests.
- `NaviusMenubarSeparator`'s sole reason to exist (a distinct `DefaultStyleKey` for
  `Themes/Menubar.xaml`) is unaffected by this decision; it is not cross-family duplication.
