namespace Navius.Wpf.Primitives.Overlays;

/// <summary>
/// Why an <see cref="OverlaySession"/> is being asked to close. Collapses the web's four
/// separate cancelable dismiss callbacks (OnEscapeKeyDown, OnPointerDownOutside,
/// OnFocusOutside, OnInteractOutside) into one reason enum carried by a single
/// cancelable Closing event; see OverlaySession.Closing.
/// </summary>
public enum OverlayCloseReason
{
    EscapeKey,
    OutsidePress,
    Programmatic,
}
