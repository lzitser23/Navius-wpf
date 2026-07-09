using System;

namespace Navius.Wpf.Primitives.Controls.Toast;

/// <summary>
/// One toast's auto-dismiss countdown, abstracted away from <see cref="ToastManager"/> so its
/// queueing/priority/pause logic is unit-testable without a live Dispatcher (see
/// tests/Navius.Wpf.Tests/ToastTests.cs's manual-advance test double). <see cref="DispatcherToastTimer"/>
/// is the production implementation.
/// </summary>
public interface IToastTimer : IDisposable
{
    /// <summary>Starts (or restarts) the countdown. A non-positive duration is a no-op (sticky: never elapses).</summary>
    void Start(TimeSpan duration, Action onElapsed);

    /// <summary>Freezes the remaining time; a no-op if not running or already paused.</summary>
    void Pause();

    /// <summary>Resumes counting down the remaining time from a prior <see cref="Pause"/>; a no-op otherwise.</summary>
    void Resume();

    /// <summary>Cancels the countdown; <c>onElapsed</c> will not fire.</summary>
    void Stop();
}
