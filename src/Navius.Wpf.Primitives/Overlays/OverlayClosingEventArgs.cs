using System;

namespace Navius.Wpf.Primitives.Overlays;

/// <summary>Cancelable args for <see cref="OverlaySession.Closing"/>.</summary>
public sealed class OverlayClosingEventArgs : EventArgs
{
    public OverlayClosingEventArgs(OverlayCloseReason reason)
    {
        Reason = reason;
    }

    public OverlayCloseReason Reason { get; }

    /// <summary>Set true to keep the overlay open; blocks the subsequent Closed event.</summary>
    public bool Cancel { get; set; }
}
