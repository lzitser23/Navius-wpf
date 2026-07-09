using System;
using System.Windows;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Overlays;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Demonstrates OverlayStack: an in-window modal card (backdrop + focus-trapped panel) opened
/// from a button, closable via Escape, an outside click, or its own Close button.
/// </summary>
public partial class OverlayPage : UserControl
{
    private OverlaySession? _session;

    public OverlayPage()
    {
        InitializeComponent();
    }

    private void OnOpenClick(object sender, RoutedEventArgs e)
    {
        if (_session is not null)
        {
            return;
        }

        var window = Window.GetWindow(this);
        if (window is null)
        {
            return;
        }

        OverlayLayer.Visibility = Visibility.Visible;

        var stack = OverlayStack.GetFor(window);
        _session = stack.Push(PanelRoot, new OverlayOptions
        {
            Modal = true,
            CloseOnEscape = true,
            CloseOnOutsideClick = true,
            TrapFocus = true,
            RestoreFocus = true,
        });
        _session.Closed += OnSessionClosed;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        _session?.RequestClose(OverlayCloseReason.Programmatic);
    }

    private void OnSessionClosed(object? sender, EventArgs e)
    {
        if (_session is not null)
        {
            _session.Closed -= OnSessionClosed;
            _session = null;
        }

        OverlayLayer.Visibility = Visibility.Collapsed;
    }
}
