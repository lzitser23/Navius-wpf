using System;
using System.Windows;
using System.Windows.Input;

namespace Navius.Wpf.Primitives.Overlays;

/// <summary>
/// One open overlay tracked by an <see cref="OverlayStack"/>. Created per call to
/// <see cref="OverlayStack.Push"/>; carries the overlay's root element, its options,
/// its current z-order index, and the cancelable close lifecycle.
/// </summary>
public sealed class OverlaySession
{
    private readonly OverlayStack _owner;

    internal OverlaySession(OverlayStack owner, FrameworkElement root, OverlayOptions options, int stackIndex)
    {
        _owner = owner;
        Root = root;
        Options = options;
        StackIndex = stackIndex;
    }

    /// <summary>The overlay's root element, as passed to <see cref="OverlayStack.Push"/>.</summary>
    public FrameworkElement Root { get; }

    public OverlayOptions Options { get; }

    /// <summary>Position within the owning stack; 0 is bottommost. Updated as sessions above this one are popped.</summary>
    public int StackIndex { get; internal set; }

    public bool IsModal => Options.Modal;

    public bool IsClosed { get; private set; }

    /// <summary>The element focused immediately before this session engaged its focus trap, or null if TrapFocus was false.</summary>
    internal IInputElement? RestoreFocusTarget { get; set; }

    /// <summary>Cancelable: set <see cref="OverlayClosingEventArgs.Cancel"/> to keep the overlay open.</summary>
    public event EventHandler<OverlayClosingEventArgs>? Closing;

    /// <summary>Raised after a close request succeeds (was not canceled).</summary>
    public event EventHandler? Closed;

    /// <summary>
    /// Asks the session to close for the given reason. Fires Closing first; if canceled,
    /// the session stays open and this returns false. Otherwise pops the session from its
    /// owning stack and fires Closed. Returns true if the session is now closed (or already was).
    /// </summary>
    public bool RequestClose(OverlayCloseReason reason)
    {
        if (IsClosed)
        {
            return true;
        }

        var args = new OverlayClosingEventArgs(reason);
        Closing?.Invoke(this, args);
        if (args.Cancel)
        {
            return false;
        }

        IsClosed = true;
        _owner.Remove(this);
        Closed?.Invoke(this, EventArgs.Empty);
        return true;
    }
}
