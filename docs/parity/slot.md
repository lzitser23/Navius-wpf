# Slot

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusSlot | None (renders only `@ChildContent(Merged)`, no wrapper element, no `data-navius-slot` marker) | Blazor approximation of the spec `<Slot/>`/`asChild` engine: merges forwarded attributes onto the caller-supplied single child render fragment instead of injecting its own DOM node |

## Parameters

| Name | Type | Default | Notes |
|---|---|---|---|
| Attributes | `IReadOnlyDictionary<string, object>?` | null | Props to forward onto the single child element (what the spec `Slot` would merge onto its child) |
| UnmatchedAttributes | `IDictionary<string, object>?` (CaptureUnmatchedValues) | null | Attributes captured directly on `<NaviusSlot>`; merged with `Attributes`, wins on key collision |
| ChildContent | `RenderFragment<IReadOnlyDictionary<string, object>>?` | null | Single child; receives the merged dictionary, consumer must splat it via `@attributes` onto exactly one root element |

## Events

None declared directly. Event handler attributes (`on*` keys) passed through `Attributes`/`UnmatchedAttributes` are composed (not overwritten) by `SlotMerge`: the existing (child) handler runs first, then the incoming (parent/slot) handler, for both parameterless (`Action`/`Func<Task>`) and single-argument (`Action<T>`/`Func<T,Task>`) delegate shapes with matching argument types. Incompatible shapes fall back to last-wins.

## State + data attributes

None. `NaviusSlot` renders no element of its own, so there is no `data-*` marker to carry state. `SlotMerge.Combine` performs the merge:
- `class` / `className` normalize to one `class` attribute, values concatenated space-separated.
- `style` merges per CSS property (object-spread semantics): both style strings are parsed into an ordered property map, the incoming value wins per property, re-serialized without duplicate declarations.
- All other keys are last-wins (`UnmatchedAttributes`/overrides win over `Attributes`/forwarded).

## Keyboard

None. Slot carries no interaction of its own; whatever keyboard behavior exists belongs to the consumer's child element and any composed handlers.

## Accessibility

None wired directly; Slot is a pure attribute-merge utility. Any ARIA attributes flow through as regular forwarded attributes and are merged like any other key (last-wins, except `class`/`style`/`on*` special-cased as above).

## WPF strategy

Tier C: reinterpret or retire. WPF has no DOM/attribute-splatting model comparable to Blazor's `@attributes`, so a literal `NaviusSlot` port is not meaningful. Per the project's own `navius-aschild-limit` note this is the one already-acknowledged Radix-parity deviation (ADR-0003): Blazor cannot expose a child element's props for merging, so `NaviusSlot` is already an approximation, not a direct port of the spec. For WPF, the `asChild` need is more naturally met by markup extensions, attached properties, or `Style`/`ControlTemplate` composition (e.g. exposing a `TemplateBinding`-driven attached-property bag) rather than a runtime attribute-merge component. `SlotMerge`'s pure C# merge algorithm (class/style/event composition) has no direct WPF need since WPF has no `class`/inline-`style` attribute string model; its event-handler-composition idea could inform a `RoutedEvent` multicast/composition helper if ever needed, but there is no drop-in mapping.

## Open questions

- Does the WPF port need an `asChild`-equivalent at all, or can consumers just use `ContentPresenter`/`ControlTemplate` styling instead?
- If an equivalent is wanted, is it a markup extension, an attached-property merge helper, or something templated per-control (Tier B custom control per consuming primitive) rather than one generic `Slot`?
- `SlotMerge`'s `class`/`style` merge logic has no natural WPF analog (no string class attribute, no inline style string); confirm no dependents assume string-based style merging survives the port.

## WPF implementation notes

Retired; see docs/adr/0003-web-substrate-utilities-retired.md.

## M6 audit (2026-07-09)

### Retirement CONFIRMED (no code to fix)

- **(a) The retirement is real, not an after-the-fact label on an accidental omission.** There is
  no `Controls/Slot/` folder and no `NaviusSlot.cs` under `src/Navius.Wpf.Primitives`. A repo-wide
  glob for `*Slot*` returns only the web Blazor project's files
  (`src/Navius.Primitives/Components/Slot/NaviusSlot.razor`, `SlotMerge.cs`) plus
  `NaviusPasswordToggleFieldSlot.razor`, none of them in the WPF assembly.
- **(b) ADR-0003's reasoning is internally consistent with the observed composition patterns.**
  Every family audited in this batch composes via `ControlTemplate` + named `TemplatePart`, never
  DOM-style attribute splatting: `NaviusSwitch` declares `PART_Thumb`
  (`Controls/Switch/NaviusSwitch.cs`), `NaviusSlider` declares `PART_Track`/`PART_Range`/`PART_Thumb`
  (`Controls/Slider/NaviusSlider.cs:16-23`), and Tabs/Sortable subclass native `TabControl`/
  `ItemsControl` with template-part styling. This matches the ADR's stated rationale that WPF
  composes via "ControlTemplate + named template parts... not a generic slot component," so there is
  nothing for a runtime attribute-merge primitive to do.
- **(c) The parity doc's WPF implementation notes correctly point to the ADR** (the "Retired; see
  docs/adr/0003..." line above).

Verdict: retirement is correct and well-founded. No dispute.
