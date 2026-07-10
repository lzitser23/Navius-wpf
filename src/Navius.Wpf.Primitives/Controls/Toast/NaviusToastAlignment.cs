namespace Navius.Wpf.Primitives.Controls.Toast;

/// <summary>
/// Which corner (or top/bottom-center edge) of the host window NaviusToastViewport stacks its
/// toasts against. No web equivalent by name (the web contract has a single fixed-position
/// viewport styled by the consumer's own CSS); this is the WPF port's explicit knob for the
/// same "pick a screen corner" decision. Default is BottomRight.
/// </summary>
public enum NaviusToastAlignment
{
    TopLeft,
    TopCenter,
    TopRight,
    BottomLeft,
    BottomCenter,
    BottomRight,
}
