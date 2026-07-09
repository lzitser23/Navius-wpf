# Parity contracts

One file per web Navius primitive family, extracted 2026-07-09 from `E:\Lzitser\navius\src\Navius.Primitives\Components\` (the source of truth). Each file has the same 8 sections: Parts, Parameters, Events, State + data attributes, Keyboard, Accessibility, WPF strategy, Open questions.

Tier values are the extractor's proposal per family; they get confirmed or overridden when the family's wave starts. Tier A: derive from a native WPF control. Tier B: custom lookless control. Tier C: reinterpret or retire with an ADR.

Distribution: 22 tier A, 33 tier B, 3 tier C (58 total).

| Family | Parts | Params | Proposed tier |
|---|---|---|---|
| [accessible-icon](accessible-icon.md) | 1 | 3 | B |
| [accordion](accordion.md) | 5 | 25 | A |
| [alert-dialog](alert-dialog.md) | 9 | 28 | B |
| [aspect-ratio](aspect-ratio.md) | 1 | 3 | A |
| [autocomplete](autocomplete.md) | 20 | 59 | B |
| [avatar](avatar.md) | 3 | 8 | B |
| [button](button.md) | 1 | 7 | A |
| [checkbox](checkbox.md) | 3 | 18 | A |
| [collapsible](collapsible.md) | 3 | 8 | A |
| [color-picker](color-picker.md) | 8 | 24 | B |
| [combobox](combobox.md) | 24 | 63 | B |
| [context-menu](context-menu.md) | 17 | 68 | B |
| [csp-provider](csp-provider.md) | 1 | 3 | C |
| [currency-input](currency-input.md) | 1 | 14 | A |
| [data-grid](data-grid.md) | 1 | 14 | A |
| [date-input](date-input.md) | 4 | 27 | B |
| [date-range-picker](date-range-picker.md) | 6 | 27 | B |
| [dialog](dialog.md) | 8 | 27 | B |
| [direction-provider](direction-provider.md) | 1 | 2 | C |
| [drawer](drawer.md) | 8 | 28 | B |
| [field](field.md) | 8 | 25 | B |
| [fieldset](fieldset.md) | 2 | 5 | A |
| [file-upload](file-upload.md) | 10 | 34 | B |
| [form](form.md) | 2 | 8 | B |
| [label](label.md) | 1 | 3 | A |
| [masked-input](masked-input.md) | 1 | 13 | B |
| [menu](menu.md) | 17 | 84 | A |
| [menubar](menubar.md) | 18 | 95 | A |
| [message-scroller](message-scroller.md) | 6 | 22 | B |
| [meter](meter.md) | 5 | 6 | A |
| [navigation-menu](navigation-menu.md) | 14 | 68 | B |
| [number-field](number-field.md) | 5 | 5 | B |
| [one-time-password-field](one-time-password-field.md) | 3 | 3 | B |
| [overlays](overlays.md) | 5 (base classes) | 12 | B (shared base layer) |
| [password-toggle-field](password-toggle-field.md) | 5 | 4 | B |
| [popover](popover.md) | 11 | 21 | B |
| [preview-card](preview-card.md) | 7 | 20 | B |
| [progress](progress.md) | 5 | 5 | A |
| [radio-group](radio-group.md) | 3 | 3 | B |
| [rating](rating.md) | 2 | 2 | B |
| [scroll-area](scroll-area.md) | 5 | 14 | A |
| [select](select.md) | 17 | 74 | A (B layering for positioner/multi-select) |
| [separator](separator.md) | 1 | 1 | B |
| [slider](slider.md) | 4 | 15 | A |
| [slot](slot.md) | 1 | 3 | C |
| [sortable](sortable.md) | 3 | 12 | B |
| [switch](switch.md) | 2 | 12 | A |
| [tabs](tabs.md) | 4 | 13 | A |
| [tag-input](tag-input.md) | 5 | 17 | B |
| [time-input](time-input.md) | 1 (+2 shared segment sub-parts) | 20 | B |
| [time-picker](time-picker.md) | 6 | 16 | B |
| [toast](toast.md) | 10 (2 stubs) | 40+ | B |
| [toggle](toggle.md) | 1 | 5 | A |
| [toggle-group](toggle-group.md) | 2 | 10 | A |
| [toolbar](toolbar.md) | 6 | 12 | B |
| [tooltip](tooltip.md) | 7 | 15 | A |
| [tree](tree.md) | 6 | 20 | A |
| [visually-hidden](visually-hidden.md) | 1 | 2 | C |

## Cross-cutting port risks flagged during extraction

- `PasswordBox.Password` is not a bindable dependency property (by design); PasswordToggleField needs an explicit strategy.
- NavigationMenu's shared-viewport popup morphing (one viewport re-anchoring across items) has no direct WPF analog; likely a bespoke M2 design.
- The Overlays four-callback cancelable dismiss contract probably collapses to a single cancelable Closing event in WPF; decide once in the M1 overlay host design.
- Web form-participation parameters (`Name`/`Value`/`Form` hidden-input mirroring) have no WPF equivalent and likely drop across all form controls; needs one blanket ADR.
- `Slot`, `CspProvider`, `DirectionProvider`, `VisuallyHidden` retire or reinterpret (templates/ContentPresenter, n/a, `FlowDirection`, `AutomationProperties`); one ADR each.
