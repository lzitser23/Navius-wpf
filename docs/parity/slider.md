# Slider

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusSlider | `<span data-navius-slider>` (+ hidden `<NaviusBubbleInput>` per value when `Name` set) | Root: owns the authoritative value array, cascades `SliderContext`, wires pointer drag via JS interop, renders hidden number inputs for form submission |
| NaviusSliderTrack | `<span data-navius-slider-track>` | Pure layout rail that the range fills and the thumb travels along |
| NaviusSliderRange | `<span data-navius-slider-range>` | The filled band between the lowest and highest thumb values, positioned via inline `style` |
| NaviusSliderThumb | `<span role="slider" data-navius-slider-thumb>` | Draggable handle; owns keyboard interaction and ARIA slider semantics; one entry per value in a range slider |

## Parameters

### NaviusSlider

| Name | Type | Default | Notes |
|---|---|---|---|
| Value | `IReadOnlyList<double>?` | null | Controlled values (one per thumb), paired with `ValueChanged` |
| ValueChanged | `EventCallback<IReadOnlyList<double>>` | | |
| DefaultValue | `IReadOnlyList<double>?` | null | Initial values for uncontrolled use |
| ValueCommitted | `EventCallback<IReadOnlyList<double>>` | | Fires on commit (pointer up / key edit), distinct from per-change `ValueChanged` |
| Min | `double` | 0 | |
| Max | `double` | 100 | |
| Step | `double` | 1 | |
| MinStepsBetweenThumbs | `int` | 0 | Minimum step separation between adjacent thumbs (prevents crossing) |
| LargeStep | `double` | 0 | Navius extension. 0 means the spec heuristic `max(step, 10% of range snapped to step)` is used |
| Orientation | `string` | "horizontal" | `"horizontal"` or `"vertical"` |
| Inverted | `bool` | false | High values sit at the start of the track |
| Dir | `string?` | null | `"ltr"`/`"rtl"`, falls back to cascaded `NaviusDirection`, then `"ltr"` |
| Disabled | `bool` | false | |
| Name | `string?` | null | Form field name; when set, renders a hidden `<input type="number">` per thumb (`Name[]` when >1 thumb) |
| Form | `string?` | null | HTML `form` attribute id for the hidden inputs |
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

### NaviusSliderTrack / NaviusSliderRange

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent (Track only) | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

### NaviusSliderThumb

| Name | Type | Default | Notes |
|---|---|---|---|
| ChildContent | `RenderFragment?` | null | |
| Attributes | `IDictionary<string,object>?` | null | Captured unmatched attributes |

## Events

| Part | Event | Signature |
|---|---|---|
| NaviusSlider | ValueChanged | `EventCallback<IReadOnlyList<double>>`, fired on every value change when controlled |
| NaviusSlider | ValueCommitted | `EventCallback<IReadOnlyList<double>>`, fired on pointer-up or keyboard edit commit |

## State + data attributes

| Element | Attribute | Meaning |
|---|---|---|
| Root | `data-navius-slider` | Marker |
| Root | `data-orientation` | `"horizontal"` / `"vertical"` |
| Root | `data-disabled` | Present when disabled |
| Root | `data-dragging` | Present while any thumb is actively dragged |
| Root | `dir="rtl"` | Present when effective direction is rtl |
| Track | `data-navius-slider-track`, `data-orientation`, `data-disabled` | Mirrors root state |
| Range | `data-navius-slider-range`, `data-orientation`, `data-disabled`, inline `style` (`left`/`width` or `bottom`/`height` percentages) | Positions the filled band |
| Thumb | `data-navius-slider-thumb`, `data-index`, `data-orientation`, `data-disabled`, `data-dragging` (when this thumb is the active drag), inline `style` (`left` or `bottom` percentage) | Positions and marks the thumb |
| SliderContext (C# state) | `Values`, `ActiveThumb`, `Min`, `Max`, `Step`, `LargeStep`, `MinStepsBetweenThumbs`, `Orientation`, `Dir`, `Inverted`, `Disabled` | Shared cascaded state; `Changed` event drives part re-render |

## Keyboard

All keyboard handling lives on `NaviusSliderThumb` (`@onkeydown`), no JS involved.

| Key | Behavior |
|---|---|
| ArrowRight / ArrowUp | `+Step` (direction flips under RTL for horizontal, or `Inverted`) |
| ArrowLeft / ArrowDown | `-Step` (same flip rules) |
| Shift+Arrow (any direction key) | Uses `LargeStep` instead of `Step` |
| PageUp | `+LargeStep` |
| PageDown | `-LargeStep` |
| Home | Jump to `Min` |
| End | Jump to `Max` |

Edits clamp to `[Min, Max]`, to the thumb's neighbours (respecting `MinStepsBetweenThumbs`), and snap to `Step`. Each key edit commits immediately (fires `ValueCommitted`).

Pointer drag is handled by JS (`createDragTracker` from `navius-interop.js`), calling back into `[JSInvokable] OnFraction` / `OnCommit` on the root. If the JS import fails, only pointer drag is lost; keyboard remains fully functional.

## Accessibility

- Each `NaviusSliderThumb` renders `role="slider"`, `aria-valuemin`, `aria-valuemax`, `aria-valuenow`, `aria-orientation`, `aria-disabled`.
- `aria-valuemin`/`aria-valuemax` narrow to neighbouring thumb values for multi-thumb (range) sliders per APG, otherwise the full `Min`/`Max`.
- `tabindex="0"` when enabled; thumb is entirely removed from tab order (`tabindex` omitted) when `Disabled`.
- No explicit `aria-label`/`aria-labelledby` wiring shown in the component; consumers must supply via `Attributes`.

## WPF strategy

Tier A: derive from `System.Windows.Controls.Slider` (or `RangeBase`) as the base for `NaviusSlider`/`NaviusSliderThumb` semantics, since WPF's `Slider` already implements `AutomationPeer` mapping to `role="slider"` (`SliderAutomationPeer` -> UIA `RangeValuePattern`, matching `aria-valuemin/max/now`). Multi-thumb range support (this component's core feature) is not native to `Slider`, so a lookless `Control`/`ItemsControl`-based custom template hosting N `Thumb` drag handles (Tier B ingredients) will likely be needed layered on top of a `RangeBase`-like model. Pointer-drag-to-fraction logic maps directly to WPF `Thumb.DragDelta` plus manual track-relative-position math (replacing the JS `createDragTracker`); RTL/orientation flip math (`OffsetFraction`, `RangeBounds`) ports as-is since it is pure C#. `data-dragging` / `data-orientation` attributes have no WPF equivalent and should become style triggers on a boolean dependency property (e.g. `IsDragging`) and an `Orientation` enum property.

## Open questions

- Multi-thumb range sliders have no first-class WPF control; decide whether to build a custom `ItemsControl`-of-thumbs or a fixed 1-2 thumb template.
- Hidden `<input>` form-submission mirroring (`Name`/`Form`) has no WPF equivalent, confirm whether form-post parity is even a WPF port goal.
- `LargeStep` heuristic and `MinStepsBetweenThumbs` are Navius extensions beyond WPF `Slider`; need new dependency properties.
- Should RTL support use WPF's `FlowDirection` directly instead of a `Dir` string parameter.

## WPF implementation notes

M1 delivered: `src/Navius.Wpf.Primitives/Controls/Slider/NaviusSlider.cs` (derives `Slider`),
`Controls/Slider/NaviusSliderKeyboard.cs` (pure keyboard-to-value logic), `Themes/Slider.xaml`,
`tests/Navius.Wpf.Tests/SliderTests.cs` (18 tests), `apps/Navius.Wpf.Gallery/Pages/SliderPage.xaml(.cs)`.

**Deferred**: multi-thumb range sliders. `NaviusSlider` is single-thumb only; `MinStepsBetweenThumbs`
is exposed as a dependency property for API parity but is a no-op (no adjacent thumb to separate
from). A future M-something would need either a custom `ItemsControl`-of-thumbs or a fixed 1-2 thumb
template, per the first open question above.

**Part mapping**: `PART_Track` is `Slider`'s own required part name (native `Track`, unchanged).
`PART_Range` is `Track.DecreaseRepeatButton` (a `RepeatButton`, not a decorative-only element like
the web `<span>`) restyled to a plain filled bar; it is still clickable/page-steps like a normal
`RepeatButton`, which is a minor behavioral difference from the web `NaviusSliderRange`. `PART_Thumb`
is `Track.Thumb`, unchanged.

**Orientation**: `Track` inside the template is always `Orientation="Horizontal"`; a vertical
`NaviusSlider` rotates the whole template visual tree 270 degrees via `LayoutTransform` on an
`Orientation="Vertical"` trigger, rather than shipping a second full vertical template. Track's
own value-to-position math is unaffected since it never sees an orientation change, only the
rendered visual is rotated.

**Step/LargeStep**: `Step` is a new dependency property kept in sync with native `SmallChange` and
`TickFrequency` (so pointer-drag snapping via `IsSnapToTickEnabled=true` uses the same value).
`LargeStep` implements the contract's exact heuristic (`EffectiveLargeStep`) via
`NaviusSliderKeyboard.ComputeEffectiveLargeStep`, a pure static method, unit tested directly.

**Keyboard**: `OnKeyDown` is fully overridden (not delegated to base `Slider` key handling) and
routes through `NaviusSliderKeyboard.TryGetTargetValue`, a pure static function taking
key/shift/`IsDirectionReversed`/value/bounds/step and returning a clamped target value. This was
split out specifically so the contract's keyboard table (Arrow/Shift+Arrow/PageUp/PageDown/Home/End)
is unit-testable without constructing real WPF `KeyEventArgs` (which require a live
`PresentationSource`); the existing test suite (see `OverlayStackTests`) follows the same
pure-logic-first pattern. Arrow keys flip under `IsDirectionReversed`; Page/Home/End do not, per
the contract's keyboard table (only "(direction flips under RTL for horizontal, or Inverted)" is
called out for Arrow keys).

**ValueCommitted**: a new routed event, distinct from native `ValueChanged`. Raised on
`Thumb.DragCompleted` (pointer-up) and immediately after every keyboard edit, matching the
contract's "each key edit commits immediately."

**Decisions on open questions**: RTL uses native `FlowDirection` directly, no `Dir` string
property was added (open question resolved: yes, use `FlowDirection`). `Inverted` is not
duplicated as a wrapper property; consumers set `IsDirectionReversed` directly (native Slider
property, 1:1 with the contract's `Inverted`). Hidden-input form mirroring (`Name`/`Form`) is
dropped entirely, per the top-level instruction that form mirroring is a web-only parameter.

## M6 audit (2026-07-09)

### Confirmed fixed

None. The keyboard table is genuinely wired.

### Verified TRUE

- Every key in the contract's keyboard table is really wired and tested. `NaviusSlider.OnKeyDown`
  (`NaviusSlider.cs:109-124`) fully overrides base key handling and routes through
  `NaviusSliderKeyboard.TryGetTargetValue` (`NaviusSliderKeyboard.cs:35-78`): ArrowRight/Up `+Step`,
  ArrowLeft/Down `-Step`, Shift+Arrow / PageUp / PageDown use `EffectiveLargeStep`, Home->Min,
  End->Max; arrows flip under `IsDirectionReversed`, Page/Home/End do not; result clamps to
  `[Min,Max]`. All covered directly in `SliderTests.cs:82-217`.
- `LargeStep` heuristic (`max(Step, 10% of range snapped to Step)`) is real and tested
  (`NaviusSliderKeyboard.ComputeEffectiveLargeStep`; `SliderTests.cs:63-78`).
- `Step` syncs `SmallChange`/`TickFrequency` (`NaviusSlider.cs:131-137`; test 54-61); `Maximum`
  defaults 100 (test 46-52); `ValueCommitted` raised on `Thumb.DragCompleted` and immediately after
  each key edit (`NaviusSlider.cs:121-129`).
- `MinStepsBetweenThumbs` is an intentional no-op DP (single-thumb build), matching the notes.

### Plausible / residual (not fixed)

- **RTL via `FlowDirection` is not wired into the keyboard flip.** `OnKeyDown` passes only
  `IsDirectionReversed` (`NaviusSlider.cs:114`). The implementation notes state RTL "uses native
  `FlowDirection` directly," but a horizontal slider set to `FlowDirection.RightToLeft` with
  `IsDirectionReversed=false` will **not** mirror ArrowLeft/Right, unlike the web contract's "flips
  under RTL for horizontal, or Inverted." Left as residual rather than fixed: the notes deliberately
  collapse the flip signal to the single `IsDirectionReversed` property, and the correct combination
  semantics (OR vs XOR of RTL and Inverted) versus the web source were not nailed down here. Note
  the sibling Tabs handler *does* fold `FlowDirection==RightToLeft` into its RTL mirror, so the two
  families are inconsistent on this point.
- The control-level `OnKeyDown` wiring itself has no `KeyEventArgs`-level integration test; only the
  pure `TryGetTargetValue` is exercised. This is the doc's stated pure-logic-first tradeoff, not a
  regression.
