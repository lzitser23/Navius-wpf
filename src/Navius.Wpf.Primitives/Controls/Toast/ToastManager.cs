using System;
using System.Collections.Generic;
using System.Linq;

namespace Navius.Wpf.Primitives.Controls.Toast;

/// <summary>
/// Plain C# port of the web contract's manager-driven toast tree (NaviusToastProvider's owned
/// ToastManager + ToastProviderContext, collapsed into one class -- no DI framework, no Blazor
/// cascading context to mirror). Owns the toast list, the Limit-based visible/queued split, and
/// each toast's auto-dismiss timer; <see cref="NaviusToastViewport"/> is the only consumer that
/// renders it.
///
/// Queueing model: toasts stay visible in the order they were added (insertion order); the
/// first <see cref="Limit"/> active toasts are visible, anything added beyond that queues
/// (<see cref="ToastObject.Limited"/> = true, contract: "Limited (queued beyond Limit)") until
/// an earlier toast is dismissed and frees a slot. This is a promotion, not just a removal: see
/// <see cref="RecomputeVisibility"/> and the ToastTests regression test for the historical web
/// bug where a queued toast was never promoted (silently dropped) once its slot freed.
/// </summary>
public sealed class ToastManager
{
    private readonly List<ToastObject> _toasts = new();
    private readonly Dictionary<Guid, ToastRuntime> _runtimes = new();
    private readonly Func<IToastTimer> _timerFactory;
    private int _limit;

    /// <summary>Contract default Limit is 1; DefaultDuration mirrors the contract's Timeout default of 5000ms.</summary>
    public ToastManager(int limit = 1, TimeSpan? defaultDuration = null, Func<IToastTimer>? timerFactory = null)
    {
        if (limit < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "Limit must be at least 1.");
        }

        _limit = limit;
        DefaultDuration = defaultDuration ?? TimeSpan.FromMilliseconds(5000);
        _timerFactory = timerFactory ?? (() => new DispatcherToastTimer());
    }

    /// <summary>Max simultaneously-visible toasts; changing this re-evaluates the queue immediately.</summary>
    public int Limit
    {
        get => _limit;
        set
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Limit must be at least 1.");
            }

            if (_limit == value)
            {
                return;
            }

            _limit = value;
            RecomputeVisibility();
            RaiseChanged();
        }
    }

    public TimeSpan DefaultDuration { get; }

    /// <summary>All tracked toasts, oldest first (insertion order).</summary>
    public IReadOnlyList<ToastObject> Toasts => _toasts;

    /// <summary>Visible (non-queued) toasts, newest first -- the order NaviusToastViewport stacks them in.</summary>
    public IReadOnlyList<ToastObject> VisibleToasts =>
        _toasts.Where(t => !t.Limited).Reverse().ToList();

    /// <summary>Raised after Add/Update/Dismiss/Clear/Limit change, mirroring the web contract's
    /// ToastManager.Changed.</summary>
    public event Action? Changed;

    public ToastHandle Add(ToastOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var id = Guid.NewGuid();
        var toast = new ToastObject(id, options);
        _toasts.Add(toast);
        _runtimes[id] = new ToastRuntime(_timerFactory());

        RecomputeVisibility();
        ArmTimerIfVisible(toast);

        RaiseChanged();
        return new ToastHandle(this, id);
    }

    /// <summary>
    /// Replaces a toast's options wholesale (e.g. flip a loading toast to success/error) and
    /// rearms its auto-dismiss timer against the new effective duration. Rearming unconditionally
    /// here is what fixes the historical bug where a promise's terminal success/error toast never
    /// auto-dismissed: the prior (often sticky, Loading) timer state does not carry over.
    /// </summary>
    public void Update(ToastHandle handle, ToastOptions options)
    {
        ArgumentNullException.ThrowIfNull(handle);
        ArgumentNullException.ThrowIfNull(options);

        var toast = Find(handle.Id);
        if (toast is null)
        {
            return;
        }

        toast.Options = options;
        toast.UpdateKey++;
        ArmTimerIfVisible(toast);

        RaiseChanged();
    }

    public void Dismiss(ToastHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);
        DismissById(handle.Id);
    }

    public void Clear()
    {
        if (_toasts.Count == 0)
        {
            return;
        }

        foreach (var runtime in _runtimes.Values)
        {
            runtime.Timer.Stop();
            runtime.Timer.Dispose();
        }

        _runtimes.Clear();
        _toasts.Clear();
        RaiseChanged();
    }

    public void Pause(ToastHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);
        PauseById(handle.Id);
    }

    public void Resume(ToastHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);
        ResumeById(handle.Id);
    }

    /// <summary>Pauses every visible toast's timer (e.g. on window deactivation).</summary>
    public void PauseAll()
    {
        foreach (var toast in _toasts)
        {
            if (!toast.Limited)
            {
                PauseById(toast.Id);
            }
        }
    }

    /// <summary>Resumes every visible toast's timer (e.g. on window activation).</summary>
    public void ResumeAll()
    {
        foreach (var toast in _toasts)
        {
            if (!toast.Limited)
            {
                ResumeById(toast.Id);
            }
        }
    }

    private void DismissById(Guid id)
    {
        var index = _toasts.FindIndex(t => t.Id == id);
        if (index < 0)
        {
            return;
        }

        _toasts.RemoveAt(index);
        if (_runtimes.Remove(id, out var runtime))
        {
            runtime.Timer.Stop();
            runtime.Timer.Dispose();
        }

        RecomputeVisibility();
        RaiseChanged();
    }

    private void PauseById(Guid id)
    {
        if (!_runtimes.TryGetValue(id, out var runtime))
        {
            return;
        }

        if (runtime.PauseRefCount++ == 0)
        {
            runtime.Timer.Pause();
        }
    }

    private void ResumeById(Guid id)
    {
        if (!_runtimes.TryGetValue(id, out var runtime) || runtime.PauseRefCount == 0)
        {
            return;
        }

        if (--runtime.PauseRefCount == 0)
        {
            runtime.Timer.Resume();
        }
    }

    private ToastObject? Find(Guid id) => _toasts.FirstOrDefault(t => t.Id == id);

    /// <summary>
    /// Recomputes Limited for every toast from its current index (insertion order): the first
    /// Limit entries are visible, the rest are queued. Only toasts whose Limited flag actually
    /// flips are touched, arming a newly-promoted toast's timer or stopping a newly-demoted one's.
    /// </summary>
    private void RecomputeVisibility()
    {
        for (var i = 0; i < _toasts.Count; i++)
        {
            var toast = _toasts[i];
            var shouldBeLimited = i >= _limit;
            if (toast.Limited == shouldBeLimited)
            {
                continue;
            }

            toast.Limited = shouldBeLimited;
            if (shouldBeLimited)
            {
                if (_runtimes.TryGetValue(toast.Id, out var runtime))
                {
                    runtime.Timer.Stop();
                }
            }
            else
            {
                // Promotion out of the queue: arm as if freshly added, using whatever
                // Options/Duration it currently holds.
                ArmTimerIfVisible(toast);
            }
        }
    }

    private void ArmTimerIfVisible(ToastObject toast)
    {
        if (toast.Limited || !_runtimes.TryGetValue(toast.Id, out var runtime))
        {
            return;
        }

        runtime.PauseRefCount = 0;

        var duration = toast.Options.Duration ?? DefaultDuration;
        if (duration <= TimeSpan.Zero)
        {
            runtime.Timer.Stop();
            return;
        }

        runtime.Timer.Start(duration, () => DismissById(toast.Id));
    }

    private void RaiseChanged() => Changed?.Invoke();

    private sealed class ToastRuntime(IToastTimer timer)
    {
        public IToastTimer Timer { get; } = timer;
        public int PauseRefCount;
    }
}
