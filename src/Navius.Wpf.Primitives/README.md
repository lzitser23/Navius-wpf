# Navius.Wpf.Primitives

The lookless brain of the [navius-wpf](https://github.com/lzitser23/Navius-wpf) suite:
58 control families as WPF custom controls with no fixed visual tree, styled entirely
through `ControlTemplate`, each backed by a real `AutomationPeer` wired to UI
Automation. Includes the per-window overlay substrate (Dialog, AlertDialog, Drawer,
Menu, Toast, Tooltip and friends), anchored positioning, and the runtime theming
system (Light, Dark, HighContrast token dictionaries via `ThemeManager`).

Machine-readable contracts for every component: https://wpf.naviusui.dev
