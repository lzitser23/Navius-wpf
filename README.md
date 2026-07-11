<div align="center">

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="assets/navius-mark-dark.svg">
  <img src="assets/navius-mark-light.svg" alt="The navius mark: an N drawn as dots on a 4x4 lattice" width="88" height="88">
</picture>

# navius-wpf

**The Navius component catalog, carried onto native WPF. Lookless controls, real UIA, no WebView.**

[Overview](#overview) ·
[Features](#features) ·
[Installation](#installation) ·
[Quick start](#quick-start) ·
[Documentation](#documentation) ·
[Packages](#packages) ·
[Stack](#stack) ·
[Development](#development) ·
[Architecture](#architecture) ·
[Acknowledgments](#acknowledgments)

![License: MIT](https://img.shields.io/badge/license-MIT-171614)
![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%2010.0-171614)
![Platform](https://img.shields.io/badge/platform-WPF%2C%20no%20WebView-737270)
![Families](https://img.shields.io/badge/families-58-737270)
![Tests](https://img.shields.io/badge/tests-1281-737270)

</div>

---

## Overview

navius-wpf carries the [navius](https://github.com/lzitser23/Navius) component catalog
onto native WPF: lookless custom controls with no fixed visual tree, styled entirely
through `ControlTemplate`, each backed by a real `AutomationPeer` wired to Windows UI
Automation. There is no browser control and no WebView anywhere in the stack.

Parity here means the WPF port matches the web catalog's surface, API semantics,
keyboard matrix, and accessibility outcomes, rebuilt on WPF's own mechanics
(dependency properties, routed events, `ControlTemplate`, UIA peers) rather than
emulating a DOM. Each family's contract, port notes, and an adversarial audit trail
live in [`docs/parity/`](docs/parity/), one document per family.

> Named after **Attus Navius**, the Roman augur who sliced a whetstone with a razor.
> No Razor here; the whetstone this time is WPF itself.

---

## Features

- **58 primitive and utility families** under `src/Navius.Wpf.Primitives/Controls`,
  from Button and Checkbox through Select, Combobox, DataGrid, Tree, DatePicker, and
  a full overlay tier (Dialog, AlertDialog, Drawer, Menu, Toast, Tooltip) hosted in a
  per-window `NaviusOverlayLayer` with a shared overlay stack for Escape routing,
  outside-press dismissal, and focus trapping.
- **Real accessibility mechanics.** Custom `AutomationPeer`s report the intended UIA
  control types and patterns; keyboard maps are implemented per family and pinned by
  routed-key unit tests against real windows.
- **A styled layer, `Navius.Wpf.Ui`**: 20 vendorable styled items (Sidebar,
  CommandPalette, Card, Timeline, and more) built as `ControlTemplate`s over the same
  warm-grayscale token system as the web catalog.
- **Runtime theming.** Light, Dark, and HighContrast token dictionaries swapped live
  via `ThemeManager.Apply`, with `ThemeChanged` for listeners and opt-in OS
  high-contrast sync (`EnableSystemHighContrastSync`), all consumed through
  `DynamicResource`.
- **A native motion engine, `Navius.Wpf.Motion`**: the closed-form spring solver
  shared with the web port, baked to `DoubleAnimationUsingKeyFrames` for
  zero-interruption playback or driven live by a retargetable `SpringTicker`, plus
  named enter/exit and micro-interaction presets.
- **Charts, `Navius.Wpf.Charts`**: a thin themed wrapper over LiveCharts2 that follows
  the token themes at runtime ([ADR-0004](docs/adr/0004-chart-library.md)).
- **Copy-paste vendoring.** The `navius-wpf` dotnet tool copies any registry item's
  source (79 items: 58 primitive, 20 styled, 1 core) into your project with
  transitive dependency closure and namespace rewriting; a CI-style gate builds the
  vendored output to prove the closure compiles.
- **Test-verified.** 1281 tests across five projects: 1212 unit (STA, real windows),
  38 motion, 23 charts, 2 vendoring-closure, and 6 FlaUI-driven UIA end-to-end tests.
  Every family also carries a recorded adversarial audit in its parity doc.
- **RTL and DPI hardening** ([ADR-0006](docs/adr/0006-rtl-dpi-hardening.md)):
  flow-direction-aware keyboarding and layout with pixel-verified segment behavior,
  per-monitor work-area acquisition, PerMonitorV2 in the Gallery.

---

## Installation

The four library packages pack locally at `1.0.0-preview.1` and are not yet published
to NuGet. Until they are, consume the source directly:

```bash
git clone https://github.com/lzitser23/Navius-wpf.git navius-wpf
```

Reference the projects you need:

```bash
dotnet add reference ../navius-wpf/src/Navius.Wpf.Primitives/Navius.Wpf.Primitives.csproj
```

Or vendor individual components (shadcn-style) with the in-repo CLI, which copies the
item plus its full dependency closure into your project under your own namespace:

```bash
dotnet run --project tools/Navius.Wpf.Cli -- list
dotnet run --project tools/Navius.Wpf.Cli -- add select --to ../my-app --namespace MyApp
```

---

## Quick start

Apply a theme once at startup, declare one `NaviusOverlayLayer` per window, then
compose controls:

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    ThemeManager.Apply(NaviusTheme.Light);
}
```

```xml
<Window ...
        xmlns:navius="clr-namespace:Navius.Wpf.Primitives.Controls;assembly=Navius.Wpf.Primitives"
        xmlns:overlay="clr-namespace:Navius.Wpf.Primitives.Controls.OverlaySurface;assembly=Navius.Wpf.Primitives"
        xmlns:dialog="clr-namespace:Navius.Wpf.Primitives.Controls.Dialog;assembly=Navius.Wpf.Primitives"
        Background="{DynamicResource Navius.Background}">
    <Grid>
        <navius:NaviusButton Content="Open" Click="OnOpenDialog" />

        <overlay:NaviusOverlayLayer>
            <dialog:NaviusDialog x:Name="Confirm"
                                 Title="Confirm change"
                                 Description="This updates the shared settings.">
                <StackPanel>
                    <TextBlock Text="Apply the change?" />
                    <navius:NaviusButton Content="Close"
                                         Command="overlay:NaviusOverlaySurfaceBase.CloseCommand" />
                </StackPanel>
            </dialog:NaviusDialog>
        </overlay:NaviusOverlayLayer>
    </Grid>
</Window>
```

Open it with `Confirm.Open()` (or bind `IsOpen`). Escape, outside-click dismissal,
focus cycling, and UIA dialog semantics come built in. The Gallery app
(`apps/Navius.Wpf.Gallery`) demonstrates every family the same way.

---

## Documentation

**[wpf.naviusui.dev](https://wpf.naviusui.dev)** serves one machine-readable manifest
per component: the control type and base, every property with its registered default,
keyboard map, the UIA mechanism it drives, deltas from the web contract, and Gallery
screenshots in all three themes. Every page is available rendered and as raw markdown,
with an [`llms.txt`](https://wpf.naviusui.dev/llms.txt) index, so coding agents can
consume the contracts directly. The same site carries the install, theming, motion,
ADR, and changelog guides.

In-repo, the deeper layer is [`docs/parity/`](docs/parity/): one document per family
with the full web contract, WPF port notes, and the adversarial audit trail the
manifests are derived from.

---

## Packages

| Package | Role |
| --- | --- |
| `Navius.Wpf.Primitives` | The lookless brain: 58 control families, overlay substrate, positioning, theming. No visual opinion. |
| `Navius.Wpf.Ui` | Styled layer: token-themed `ControlTemplate`s and composite components over the primitives. |
| `Navius.Wpf.Motion` | Standalone spring motion engine: solver, keyframe baker, live ticker, presets. No reference to the primitives. |
| `Navius.Wpf.Charts` | Themed LiveCharts2 wrapper following the token system. |
| `navius-wpf` | Dotnet tool that vendors registry items (source + dependency closure) into your app. |

---

## Stack

| Layer | Choice |
| --- | --- |
| Target frameworks | `net8.0-windows`, `net10.0-windows` |
| UI framework | WPF, lookless custom controls + `ControlTemplate` theming |
| Accessibility | UI Automation via per-family `AutomationPeer`s |
| Theming | `ResourceDictionary` tokens (Light / Dark / HighContrast), `DynamicResource` throughout |
| Charts | LiveCharts2 (`LiveChartsCore.SkiaSharpView.WPF`) |
| Tests | xUnit (STA facts against real windows), FlaUI UIA3 for end-to-end |

---

## Development

```bash
dotnet build Navius.Wpf.sln -c Release
dotnet test tests/Navius.Wpf.Tests -c Release --no-build
dotnet test tests/Navius.Wpf.E2E -c Release --no-build
dotnet run --project apps/Navius.Wpf.Gallery
```

CI (`.github/workflows/ci.yml`) builds the solution and runs the unit and e2e test
projects on `windows-latest` for pushes and pull requests to `main`.

To regenerate the theme screenshots used by the docs site:

```bash
dotnet run --project tools/Navius.Wpf.Captures -- --out <dir>
```

---

## Architecture

```
src/
  Navius.Wpf.Primitives/   # the brain: 58 lookless families, overlays, positioning, theming
  Navius.Wpf.Ui/           # styled layer over the token system
  Navius.Wpf.Motion/       # spring solver + WPF execution surface
  Navius.Wpf.Charts/       # themed LiveCharts2 wrapper
apps/
  Navius.Wpf.Gallery/      # every family on a page, three themes
tools/
  Navius.Wpf.Cli/          # registry vendoring tool (list / add / registry-sync)
  Navius.Wpf.Captures/     # theme-sweep screenshot harness for the Gallery
tests/                     # five test projects, 1281 tests
docs/
  parity/                  # one contract + port-notes + audit doc per family
  adr/                     # architecture decision records
registry/                  # generated vendoring registry (registry-sync owns it)
```

Decisions with reasoning live in [`docs/adr/`](docs/adr/); the per-family source of
truth is [`docs/parity/`](docs/parity/). Notable: the web library's `Slot`/`asChild`
approximation dissolves on WPF because templates and `ContentPresenter` are the native
composition model ([ADR-0003](docs/adr/0003-web-substrate-utilities-retired.md)).

---

## Acknowledgments

- [navius](https://github.com/lzitser23/Navius), the web sibling whose catalog, API
  contracts, keyboard matrices, and test matrices this port carries over.
- [Base UI](https://base-ui.com), the contract the web catalog mirrors.
- [LiveCharts2](https://livecharts.dev) under the charts wrapper, and
  [FlaUI](https://github.com/FlaUI/FlaUI) driving the UIA end-to-end tests.

---

## License

MIT, see [LICENSE](LICENSE).
