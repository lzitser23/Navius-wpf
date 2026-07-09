# Rating

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusRating | `div` (`role="radiogroup"`) | Root. Owns the authoritative `decimal?` value plus a transient hover-preview value, cascades `RatingContext`, auto-renders `Max` `NaviusRatingItem` stars when no `ChildContent` is supplied, owns the keyboard model (arrows / Home / End / digit keys / Backspace/Delete), and renders a hidden `NaviusBubbleInput` (`type="hidden"`) when `Name` is set. |
| NaviusRatingItem | `button` (`role="radio"`), plus two `aria-hidden` overlay `span`s (`data-navius-rating-item-half="start"`/`"end"`) when `AllowHalf` and not disabled | One visual star. Roving tabindex; registers with the group in document order to get a 1-based `Index`; computes its own `full`/`half`/`empty` fill state from `RatingContext.Effective`; the half-zone overlay spans provide pointer-driven half-star selection (not separate registered parts/components). |

## Parameters

**NaviusRating**

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | `decimal?` | `null` | Controlled value; use `@bind-Value`. Controlled mode is detected via `SetParametersAsync` observing whether `Value` was supplied, not via `ValueChanged.HasDelegate`. |
| ValueChanged | `EventCallback<decimal?>` | | |
| DefaultValue | `decimal?` | `null` | Uncontrolled initial value. |
| Max | `int` | `5` | Number of visual stars; coerced to `>= 1` in `RatingContext.Configure`. |
| AllowHalf | `bool` | `false` | Enables half-star pointer zones and 0.5 keyboard steps. |
| AllowClear | `bool` | `true` | Re-selecting the current value, or arrowing below the lowest star, clears to unrated. |
| ReadOnly | `bool` | `false` | Focusable but non-editable. |
| Disabled | `bool` | `false` | |
| Required | `bool` | `false` | Mirrored onto the hidden bubble input. |
| Invalid | `bool` | `false` | Drives `aria-invalid`. |
| Name | `string?` | `null` | When set (non-empty), renders a hidden bubble input for native form submission. |
| Label | `Func<decimal, string>?` | `null` | Accessible-name factory per star value; defaults to `"N star(s)"` (`"1 star"` singular). |
| Dir | `string?` | `null` | Reading direction; falls back to cascaded `NaviusDirection`. Mirrors horizontal arrows under `"rtl"`. |
| ChildContent | `RenderFragment?` | `null` | When supplied, replaces the auto-generated `Max` `NaviusRatingItem` stars. |
| Attributes | `IDictionary<string, object>?` | `null` | `CaptureUnmatchedValues`. |

**NaviusRatingItem**

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | `null` | Star glyph content. |
| Attributes | `IDictionary<string, object>?` | `null` | `CaptureUnmatchedValues`. |

Note: `NaviusRatingItem` has no public `Index`/`Value` parameter; its 1-based `Index` is assigned internally by registration order via `RatingContext.RegisterItem`.

## Events

- **NaviusRating**: `ValueChanged` (`EventCallback<decimal?>`), fires in controlled mode on click/keyboard selection or clear. Hover preview (`SetHoverAsync`/`OnPointerLeaveAsync`) is internal state only, not exposed as a public event.
- **NaviusRatingItem**: no `EventCallback` parameters; clicks and pointer-enter route back through the cascaded `RatingContext` (`SelectAsync`, `SetHoverAsync`).

## State + data attributes

Root: `data-navius-rating`, `data-disabled` (`""` when `Disabled`), `data-readonly` (`""` when `ReadOnly`).

Item: `data-navius-rating-item`, `data-index` (1-based), `data-value` (the star's own full value, formatted invariant-culture), `data-state` (`"full"` / `"half"` / `"empty"`, computed from `RatingContext.Effective` = hover value if present else committed value), `data-checked`/`data-unchecked` (mutually exclusive), `data-highlighted` (`""` while under hover preview), `data-readonly`, `data-disabled`. Half-zone overlays: `data-navius-rating-item-half="start"` / `"end"`, `aria-hidden="true"`.

Public context state (`RatingContext`): `Max`, `AllowHalf`, `AllowClear`, `ReadOnly`, `Disabled`, `Required`, `Name`, `Label`, `Value`, `HoverValue`, `Step` (`0.5` with `AllowHalf`, else `1`), `Effective` (`HoverValue ?? Value ?? 0`).

## Keyboard

Handled on the root's `@onkeydown` (the root owns the value and moves focus onto the resulting star after every edit); native `<button>` semantics separately provide Space/Enter activation of the focused star:

| Key | Behavior |
|---|---|
| ArrowUp | Increase by `Step` (0.5 with `AllowHalf`, else 1), clamped to `Max`. |
| ArrowRight (ArrowLeft under `rtl`) | Same as ArrowUp (horizontal-next, mirrored by direction). |
| ArrowDown | Decrease by `Step`; if the result would fall below `Step`, clears to unrated when `AllowClear`, otherwise floors at `Step`. |
| ArrowLeft (ArrowRight under `rtl`) | Same as ArrowDown (horizontal-prev, mirrored by direction). |
| Home | Jump to `1`. |
| End | Jump to `Max`. |
| Backspace / Delete | Clear to unrated when `AllowClear`, otherwise no-op (stays at current value). |
| Digit `1`-`9` | Jump directly to that value, clamped to `Max`. |
| Space / Enter (on the focused star's native `button`) | Activates (selects) the focused star via native button-click semantics; re-selecting the current value clears it when `AllowClear`. Not implemented via explicit `onkeydown` code, since `<button>` handles this natively. |

After any keyboard edit, focus moves to the star that holds the new value (`Math.Ceiling` of the target, clamped to `[1, Max]`); all keyboard handling no-ops when `Disabled` or `ReadOnly`.

## Accessibility

- Root: `role="radiogroup"`, `aria-label` (defaults to `"Rating"` unless the consumer already supplied `aria-label`/`aria-labelledby` in `Attributes`), `aria-required`, `aria-invalid`, `aria-readonly`, `aria-disabled`, `dir` (explicit/cascaded only).
- Item: `role="radio"`, `aria-checked` (`"true"`/`"false"`), `aria-label` (from `Label` factory or the default `"N star(s)"`; the checked star announces its real possibly-fractional value, e.g. `"3.5 stars"`, every other star announces the whole value it would select), `aria-readonly` (only when the group is read-only).
- Roving tabindex: the star holding the current value is `tabindex="0"`; when unrated, star 1 is `tabindex="0"`; all others `-1`. Disabled stars are always `-1`.
- Half-zone overlay spans are `aria-hidden="true"` (pointer-only, not separately reachable/announced).

## WPF strategy

Tier B (custom lookless control). There is no native WPF star-rating control. Build a custom `Control` (or `ItemsControl`-derived) exposing a `decimal?` `Value` dependency property, `Max`/`AllowHalf`/`AllowClear`/`ReadOnly`/`Required`/`Invalid` properties, and a templated star-repeater; back it with a custom `AutomationPeer` implementing `ISelectionProvider`/`ISelectionItemProvider` per star (mirroring `role="radiogroup"`/`role="radio"`) since WPF has no built-in radiogroup-of-buttons peer. Fractional half-star fill state (`data-state` full/half/empty) and the pointer half-zone overlay spans need bespoke hit-testing (e.g. splitting each star's `Grid` column in half) since WPF has no equivalent to CSS-positioned overlay elements. RTL arrow mirroring can lean on `FlowDirection`, but the digit-key jump-to-value and Home/End/Backspace-clear keyboard model must be hand-implemented in a `PreviewKeyDown`/`KeyDown` handler exactly as above.

## Open questions

- Whether WPF should model `Value` as `decimal?` (exact parity) or fall back to `double?` for easier XAML/binding ergonomics, given `AllowHalf`'s 0.5-step snapping logic (`Normalize` in `NaviusRating.razor`) depends on decimal rounding.
- How to replicate hover-preview (`HoverValue`) state, since WPF mouse-enter/leave on templated sub-elements (each star, each half-zone) needs careful `MouseEnter`/`MouseLeave` vs `PreviewMouseMove` wiring to avoid flicker that the DOM `pointerenter`/`pointerleave` model handles for free.
- Whether the accessible-name difference between the checked star (announces the real fractional value) and all other stars (announce their whole value) is achievable cleanly via a single `AutomationPeer.GetName()` override or needs per-star peers.

## WPF implementation notes

Delivered: `src/Navius.Wpf.Primitives/Controls/Rating/NaviusRating.cs` (root, Tier B lookless
`Control`), `NaviusRatingItem.cs`, `NaviusRatingMath.cs` (pure step/clamp/select math),
`NaviusRatingAutomationPeer.cs`, `NaviusRatingItemAutomationPeer.cs`, `Themes/Rating.xaml`,
`tests/Navius.Wpf.Tests/RatingTests.cs`, `apps/Navius.Wpf.Gallery/Pages/RatingPage.xaml(.cs)`.

**`Value` type (first open question resolved)**: kept `decimal?`, exact parity with the contract,
not `double?`. `NaviusRatingMath` does all step/clamp/select arithmetic in `decimal` so
`AllowHalf`'s 0.5-step snapping stays exact.

**Hover preview (second open question resolved)**: `NaviusRatingItem` overrides
`OnMouseEnter`/`OnMouseLeave` (not `PreviewMouseMove`) and raises a bubbling `HoverChanged` routed
event that the owning `NaviusRating` handles centrally (`_hoverValue` field), the same
bubble-to-owner shape `NaviusRadioGroup` already uses for `OnItemChecked`. `IsHalf` is `null` only
on leave (clears hover); enter carries `true`/`false` based on which half of the star's bounds the
pointer is in. No debouncing was added beyond WPF's native `MouseEnter`/`MouseLeave` semantics; no
flicker was observed in manual testing since each star is a single hit-test-visible element (the
half zones are geometric X-position checks within one `OnMouseEnter`, not separate overlay elements
receiving their own enter/leave).

**Asymmetric accessible name (third open question resolved)**: a single
`NaviusRatingItemAutomationPeer.GetNameCore()` override, not per-star peer subclasses. It walks up
the visual tree to find the owning `NaviusRating`, compares this item's `Index` against
`NaviusRatingMath.FocusIndex(group.Value, group.Max)` to decide "is this the checked star", and
announces the real `group.Value` (fractional) when checked or the item's own whole `Index`
otherwise, via `group.Label` or `NaviusRating.DefaultLabel`.

**Deviation -- ChildContent dropped**: the contract's `ChildContent` override (a custom star glyph,
replacing the auto-generated `Max` stars) has no WPF equivalent without a bespoke items-template
system and was dropped; the star is a single fixed `PathGeometry` in `Themes/Rating.xaml`
(`Navius.Rating.StarGeometry`), styleable but not swappable per-instance without editing the
template.

**Correction (M6 RTL wave) -- half-zone clip mirroring claim was false**: this section previously
flagged the half-fill visual (`RectangleGeometry Rect="0,0,12,24"` on the "Fill" `Path`) as "not
RTL-mirrored". Pixel-rendered `RenderTargetBitmap` verification (see
`RatingTests.HalfFill_ClipMirrorsUnderRtl_SolidInkOnOppositeSideFromLtr`) shows this was incorrect:
WPF's automatic `FlowDirection` mirroring is applied once, as a whole, at the point where
`FlowDirection` is explicitly set (here, directly on the templated `NaviusRatingItem`, or on
whatever ancestor a consumer sets it on), and nothing in `Themes/Rating.xaml` opts a descendant out
with its own local `FlowDirection`. That single mirror transform reflects everything the item
renders, including the "Fill" `Path`'s local `Clip`, exactly the same as it already does for the
star's outline glyph. No code change was needed; keyboard arrow mirroring under `rtl` (implemented
in `HandleKey`) was already correct as previously documented.

**Keyboard**: `NaviusRating.HandleKey(Key)` is `public` (not the more natural `internal`) so
`RatingTests` can drive the full keyboard table (`Key.Up`/`Down`/`Left`/`Right` with RTL mirroring,
`Home`/`End`, `Backspace`/`Delete`, digit `1`-`9`) without constructing real `KeyEventArgs` --
the same public-for-testability tradeoff `NaviusProgress.FormatValueText()` makes elsewhere in this
codebase. `PreviewKeyDown` on the root calls `HandleKey` and then focuses the resulting star,
mirroring `NaviusRadioGroup`'s roving-tabindex-after-edit behavior.

**Roving tabindex / peer selection**: `GetItem(int)`/`SelectItem(NaviusRatingItem)` are `internal`
(same-assembly access from the automation peers is sufficient, no `InternalsVisibleTo` needed) and
back both `NaviusRatingAutomationPeer.GetSelection()`/`ISelectionProvider` and
`NaviusRatingItemAutomationPeer.Select()`/`ISelectionItemProvider`. `AutomationControlType.Group`
is used for the root (not `List`), matching `NaviusRadioGroupAutomationPeer`'s existing precedent
in this codebase, since WPF has no built-in radiogroup-of-buttons peer.

## M6 audit (2026-07-09)

Confirmed issue found + FIXED (the "Space is dead" bug class):
- The contract keyboard table lists `Space / Enter` as activating (selecting) the focused star. In the web version that falls out of the item being a native `<button>`. In WPF, `NaviusRatingItem` derives from a plain `Control` (NaviusRatingItem.cs:24), has no keyboard handling of its own, and `NaviusRating.HandleKey` (NaviusRating.cs) had no `Space`/`Enter` case, so pressing Space or Enter on a focused star did NOTHING: the key was silently unhandled. This is exactly the dead-activation-key class the audit was told to hunt for.
- Fix: added `Key.Space`/`Key.Enter` cases to `NaviusRating.HandleKey` (NaviusRating.cs:188-196) that select the star at `NaviusRatingMath.FocusIndex(Value, Max)` (always the roving-focused star) via `NaviusRatingMath.Select`, so re-selecting the current value clears it when `AllowClear`, matching native button-click semantics. Guarded by the existing `!IsEnabled || ReadOnly` early return.
- Regression tests added to RatingTests.cs: `HandleKey_Space_SelectsFocusedStar_WhenUnrated`, `HandleKey_Enter_ReselectingFocusedStar_ClearsWhenAllowClear`, `HandleKey_Space_NoOpWhenReadOnly`.

Plausible / residual (left for follow-up, not fixed): the half-fill visual clip is not RTL-mirrored (already flagged in the WPF implementation notes above) - the keyboard arrow mirroring under rtl IS correct, only the geometric half-star fill does not flip sides. Unchanged this wave.

Verified TRUE under adversarial check: every other key in the contract table is genuinely wired in `HandleKey` and covered by a passing test (Up/Down/Left/Right with rtl mirroring, Home, End, Backspace, Delete, digit 1-9). `NaviusRatingMath` is exact-decimal and unit-tested. The automation peers (`NaviusRatingAutomationPeer` : ISelectionProvider = Group, `NaviusRatingItemAutomationPeer` : ISelectionItemProvider = RadioButton, with the asymmetric fractional-vs-whole `GetNameCore`) exist and are returned from `OnCreateAutomationPeer`, matching the doc.
