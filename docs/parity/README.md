# Parity contracts

One file per web Navius primitive family, extracted 2026-07-09 from `E:\Lzitser\navius\src\Navius.Primitives\Components\` (the source of truth). Each file has the same 8 sections: Parts, Parameters, Events, State + data attributes, Keyboard, Accessibility, WPF strategy, Open questions.

Two additional files, [calendar](calendar.md) and [date-picker](date-picker.md) (added 2026-07-11), document WPF-side families that have no web extraction of their own: their web contract source is the date-range-picker family composition, as each file's header states. They follow the same section discipline against the shipped WPF source instead of a web extraction.

Tier values are the extractor's proposal per family; they get confirmed or overridden when the family's wave starts. Tier A: derive from a native WPF control. Tier B: custom lookless control. Tier C: reinterpret or retire with an ADR.

Distribution: 22 tier A, 33 tier B, 3 tier C over the 58 web-extracted families. With the two WPF-side docs (calendar: tier A, date-picker: tier B, both previously folded into date-range-picker.md) the directory documents 60 families total.

## Coverage rule

Policy (2026-07-11, decided while splitting Calendar and DatePicker out of date-range-picker.md; see issue #2): **every registry item that ships a user-facing control gets its own generated docs page on wpf.naviusui.dev, and every such `registry:primitive` item additionally gets its own parity document here.** User-facing means a consumer declares the control directly (it has its own Gallery page or documented consumer surface). Dependency-only infrastructure may remain grouped into the parity doc of a family that owns it, even when it is a registry item a consumer could technically `add` directly: today that is `core` (`registry:core`), and `anchored-popup`, `overlay-surface`, and `button-automation-peer` (all `registry:primitive`, reached through the dependency closure: positioning, the dialog/alert-dialog/drawer surface base, and the shared button peer), plus `internal` (`registry:styled`, the styled layer's shared helpers). Calendar and DatePicker are user-facing registry items with distinct APIs, so folding them into the composite's doc violated this rule; calendar.md and date-picker.md fix that. `registry:styled` items get docs pages but not parity documents, since they are not web primitive families and are outside this directory's web-parity scope.

| Family | Parts | Params | Proposed tier |
|---|---|---|---|
| [accessible-icon](accessible-icon.md) | 1 | 3 | B |
| [accordion](accordion.md) | 5 | 25 | A |
| [alert-dialog](alert-dialog.md) | 9 | 28 | B |
| [aspect-ratio](aspect-ratio.md) | 1 | 3 | A |
| [autocomplete](autocomplete.md) | 20 | 59 | B |
| [avatar](avatar.md) | 3 | 8 | B |
| [button](button.md) | 1 | 7 | A |
| [calendar](calendar.md) * | 1 | 0 | A (shipped) |
| [checkbox](checkbox.md) | 3 | 18 | A |
| [collapsible](collapsible.md) | 3 | 8 | A |
| [color-picker](color-picker.md) | 8 | 24 | B |
| [combobox](combobox.md) | 24 | 63 | B |
| [context-menu](context-menu.md) | 17 | 68 | B |
| [csp-provider](csp-provider.md) | 1 | 3 | C |
| [currency-input](currency-input.md) | 1 | 14 | A |
| [data-grid](data-grid.md) | 1 | 14 | A |
| [date-input](date-input.md) | 4 | 27 | B |
| [date-picker](date-picker.md) * | 1 (+ shared base) | 9 | B (shipped) |
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

\* calendar and date-picker have no web extraction; their Parts/Params count the shipped WPF public surface (calendar adds no members over the native Calendar; date-picker's 9 params are `Value` plus the 8 dependency properties on the shared `NaviusDatePickerBase`) and their tier is the shipped tier, not a proposal.

## Cross-cutting port risks flagged during extraction

- `PasswordBox.Password` is not a bindable dependency property (by design); PasswordToggleField needs an explicit strategy.
- NavigationMenu's shared-viewport popup morphing (one viewport re-anchoring across items) has no direct WPF analog; likely a bespoke M2 design.
- The Overlays four-callback cancelable dismiss contract probably collapses to a single cancelable Closing event in WPF; decide once in the M1 overlay host design.
- Web form-participation parameters (`Name`/`Value`/`Form` hidden-input mirroring) have no WPF equivalent and likely drop across all form controls; needs one blanket ADR.
- `Slot`, `CspProvider`, `DirectionProvider`, `VisuallyHidden` retire or reinterpret (templates/ContentPresenter, n/a, `FlowDirection`, `AutomationProperties`); one ADR each.
