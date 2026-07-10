using System;

namespace Navius.Wpf.Primitives.Controls.Toast;

/// <summary>
/// Returned by <see cref="ToastManager.Add"/>; a thin id+manager wrapper (cheap to mint again
/// from an id, see NaviusToastViewport) that forwards to the owning manager. This is the
/// "promise-style" handle: hold on to it and call <see cref="Update"/> to flip a loading toast
/// to success/error, or <see cref="Dismiss"/> to close it early.
/// </summary>
public sealed class ToastHandle
{
    internal ToastHandle(ToastManager manager, Guid id)
    {
        Manager = manager;
        Id = id;
    }

    public Guid Id { get; }

    internal ToastManager Manager { get; }

    public void Update(ToastOptions options) => Manager.Update(this, options);

    public void Dismiss() => Manager.Dismiss(this);

    public void Pause() => Manager.Pause(this);

    public void Resume() => Manager.Resume(this);
}
