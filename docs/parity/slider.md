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
