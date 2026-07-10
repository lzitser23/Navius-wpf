using System;

namespace Navius.Wpf.Primitives.Controls.Toast;

/// <summary>
/// One toast tracked by a <see cref="ToastManager"/>. Public read model only: the manager owns
/// all mutation (via <see cref="ToastHandle"/>/internal setters), consumers (NaviusToastViewport)
/// read <see cref="Options"/>/<see cref="Limited"/>/<see cref="UpdateKey"/> to render.
/// </summary>
public sealed class ToastObject
{
    internal ToastObject(Guid id, ToastOptions options)
    {
        Id = id;
        Options = options;
    }

    public Guid Id { get; }

    public ToastOptions Options { get; internal set; }

    /// <summary>True while queued beyond the manager's Limit (not currently visible).</summary>
    public bool Limited { get; internal set; }

    /// <summary>Bumped on every <see cref="ToastManager.Update"/> call; a viewport uses a change
    /// here to replay the enter animation on an already-visible toast.</summary>
    public int UpdateKey { get; internal set; }
}
