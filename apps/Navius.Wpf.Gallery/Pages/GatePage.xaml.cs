using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Navius.Wpf.Motion;
using Navius.Wpf.Primitives.Overlays;

namespace Navius.Wpf.Gallery.Pages;

public partial class GatePage : UserControl
{
    private OverlaySession? _session;

    public GatePage()
    {
        InitializeComponent();
    }

    private void OnOpen(object sender, RoutedEventArgs e)
    {
        if (Popup.IsOpen)
        {
            return;
        }

        Popup.IsOpen = true;
        var window = Window.GetWindow(this);
        if (window is not null)
        {
            _session = OverlayStack.GetFor(window).Push(PopupRoot, new OverlayOptions
            {
                CloseOnOutsideClick = true,
                TrapFocus = true,
            });
            _session.Closed += (_, _) =>
            {
                _session = null;
                Popup.IsOpen = false;
                PopupState.Text = "Closed";
            };
        }

        PopupState.Text = "Open";
        AnimateIn();
        PopupRoot.Focus();
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        _session?.RequestClose(OverlayCloseReason.Programmatic);
    }

    private void OnPopupKeyDown(object sender, KeyEventArgs e)
    {
        // The Popup lives in its own HwndSource, so the window-level Esc hook in
        // OverlayStack cannot see keys typed inside it; route Esc to the session here.
        if (e.Key == Key.Escape && _session is not null)
        {
            e.Handled = _session.RequestClose(OverlayCloseReason.EscapeKey);
        }
    }

    private void AnimateIn()
    {
        var translate = new TranslateTransform(0, 10);
        PopupRoot.RenderTransform = translate;
        translate.BeginAnimation(TranslateTransform.YProperty, SpringKeyframeBaker.Bake(Spring.Default, from: 10, to: 0));
        PopupRoot.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150)));
    }
}
