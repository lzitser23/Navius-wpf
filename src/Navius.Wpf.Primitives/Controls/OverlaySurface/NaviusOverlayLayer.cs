using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Navius.Wpf.Primitives.Controls.OverlaySurface;

/// <summary>
/// In-window host for Dialog/AlertDialog/Drawer popup surfaces (the WPF analogue of the web
/// family's NaviusPortal, which teleports Backdrop/Popup to document.body).
///
/// WPF's <see cref="System.Windows.Controls.Primitives.Popup"/> renders through a separate
/// top-level surface (its own hwnd once open), which the existing
/// <see cref="Overlays.OverlayStack"/>'s window-level PreviewKeyDown/PreviewMouseDown hooks and
/// visual/logical-descendant outside-press checks cannot see into: Escape and outside-click
/// routing would silently stop working for Popup-hosted content. NaviusOverlayLayer instead is a
/// plain <see cref="Grid"/> a consumer places once, normally as the last (topmost) child of
/// their window's root Grid, stretched to fill it, so it stays inside the window's own tree and
/// the existing OverlayStack hooks keep working.
///
/// Contract: declare each NaviusDialog / NaviusAlertDialog / NaviusDrawer instance as a direct
/// XAML child of the NaviusOverlayLayer itself (see any of the Dialog/AlertDialog/DrawerPage
/// gallery samples), the same way OverlayPage.xaml's manual Backdrop+Panel demo already lives
/// inside its own local overlay Grid. The family control defaults to Visibility.Collapsed and
/// only toggles that (plus a fade/slide) when it opens/closes; it is deliberately never
/// re-parented at runtime (a XAML-declared child already has a logical parent, and moving a
/// UIElement to a second parent without detaching it first throws), so a control declared outside
/// a NaviusOverlayLayer will fail to find one and log a dev-time warning instead of throwing. See
/// each family's "## WPF implementation notes" section in docs/parity for the diagnostic.
/// </summary>
public class NaviusOverlayLayer : Grid
{
    private static readonly ConditionalWeakTable<Window, NaviusOverlayLayer> Instances = new();

    private int _activeSurfaceCount;

    public NaviusOverlayLayer()
    {
        Focusable = false;
        Background = Brushes.Transparent;
        IsHitTestVisible = false;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>The layer registered for <paramref name="window"/>, or null if none has loaded yet.</summary>
    public static NaviusOverlayLayer? GetFor(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        return Instances.TryGetValue(window, out var layer) ? layer : null;
    }

    /// <summary>
    /// Marks a surface as active, making the layer hit-test visible so backdrop/outside-press
    /// detection works. The surface must already be declared as this layer's XAML child (see
    /// class remarks); this does not add it to <see cref="Panel.Children"/>, only tracks the
    /// open count.
    /// </summary>
    internal void AddSurface(UIElement surface)
    {
        if (!Children.Contains(surface))
        {
            // Not pre-declared as our child (surface was constructed and opened purely from
            // code, or the documented contract wasn't followed): fall back to adding it so it
            // still renders, on a best-effort basis.
            Children.Add(surface);
        }

        _activeSurfaceCount++;
        IsHitTestVisible = true;
    }

    /// <summary>Marks a surface as inactive; the layer stops absorbing hit-tests once nothing is open. Never removes the surface from Children (see class remarks).</summary>
    internal void RemoveSurface(UIElement surface)
    {
        _ = surface;
        _activeSurfaceCount = Math.Max(0, _activeSurfaceCount - 1);
        IsHitTestVisible = _activeSurfaceCount > 0;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window is null)
        {
            return;
        }

        Instances.Remove(window);
        Instances.Add(window, this);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window is null)
        {
            return;
        }

        Instances.Remove(window);
    }
}
