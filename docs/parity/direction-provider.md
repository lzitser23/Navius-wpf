# DirectionProvider

## Parts

| Part | Element rendered | Purpose |
|---|---|---|
| NaviusDirectionProvider | none (DOM-transparent; renders `@ChildContent` inside a `CascadingValue`) | Cascades a reading-direction string ("ltr" or "rtl") to descendants via a named cascading parameter. Descendants that need a `dir` attribute apply it themselves. |

There is only one component in this family; no Trigger/Popup/Content sub-parts exist.

## Parameters

### NaviusDirectionProvider

| Name | Type | Default | Notes |
|---|---|---|---|
| Dir | string | `"ltr"` | Reading direction cascaded to descendants. Either `"ltr"` or `"rtl"` (no enum, no validation in code; any string is accepted and cascaded as-is). |
| ChildContent | RenderFragment? | null | Content that receives the cascaded direction. |

## Events

None. `NaviusDirectionProvider` declares no `EventCallback` parameters.

## State + data attributes

`NaviusDirectionProvider` itself renders no DOM element and sets no `data-*` attributes or CSS classes. It cascades a single value: `CascadingValue Name="NaviusDirection" Value="@Dir" IsFixed="false"`. Descendants read it via `[CascadingParameter(Name = "NaviusDirection")]` and are individually responsible for reflecting it (e.g. setting their own `dir="..."` attribute). The e2e demo page confirms this pattern: a descendant readout element is asserted to carry `dir="rtl"` (`[data-navius-direction-readout]` in `tests/e2e/specs/wave2.spec.ts`), but that attribute is rendered by the consumer, not by `NaviusDirectionProvider`.

`IsFixed="false"` means the cascaded value can change after initial render (the provider re-cascades if `Dir` changes), unlike `IsFixed="true"` used by e.g. `NaviusDialog`'s context.

## Keyboard

None. `NaviusDirectionProvider` has no interactive element, no `@onkeydown`/`OnKeyDown` handler, and no keyboard-relevant code.

## Accessibility

`NaviusDirectionProvider` renders no markup and wires no ARIA role or attribute itself. It only affects accessibility indirectly: descendants that read the cascaded direction and set their own `dir` attribute get the corresponding native RTL/LTR behavior (bidi text rendering, mirrored focus-order expectations for other primitives like anchored popovers, per the "RTL: an Align=\"start\" anchored popup mirrors..." e2e test in `wave2.spec.ts`). No focus management, no `FocusAsync` calls, no `tabindex` handling in this component.

## WPF strategy

Tier C (reinterpret or retire).

WPF has a native `FlowDirection` property (`System.Windows.FrameworkElement.FlowDirection`, values `LeftToRight`/`RightToLeft`) that already cascades down the visual/logical tree via property-value inheritance, so a dedicated cascading-parameter component is largely redundant. Reinterpret this family as a thin helper (e.g. a `MarkupExtension`, attached property, or simply "set `FlowDirection` on a container") rather than porting a `CascadingValue`-based component 1:1; there is no `AutomationPeer`/ARIA-role mapping needed since this part renders no element and has no accessibility surface of its own. What will NOT translate cleanly: the Blazor cascade is DOM/component-tree-scoped and lets each consumer decide whether/how to reflect `dir` (some consumers ignore it, some read it only to compute RTL-aware layout math like the anchored-popover mirroring), whereas WPF's `FlowDirection` is a rendering-level property that automatically mirrors layout, text, and scrollbars for every descendant: behavior parity needs a decision on whether the WPF port wants automatic visual mirroring (via `FlowDirection`) or the Blazor-style "opt-in per consumer" model (via an attached property consumers read explicitly).

## Open questions

- The Blazor component accepts any string for `Dir` with no validation; the WPF port needs a decision on whether to use the native `FlowDirection` enum (`LeftToRight`/`RightToLeft`) directly, or preserve a string API for lower-level parity.
- Since `NaviusDirectionProvider` doesn't render an element and doesn't itself apply the direction to anything, the WPF equivalent needs a product decision on whether it becomes a real component at all, or is simply documented as "set `FlowDirection` on the root/container" with no dedicated primitive.
- No code was found showing whether/how anchored-popover placement logic (mentioned in the e2e RTL test) reads this cascaded direction from within the Navius.Primitives family folder itself; that consumption logic lives in the Popover/positioning family, so its exact RTL math dependency on this value could not be verified from the DirectionProvider folder alone.
