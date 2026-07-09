using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Navius.Wpf.Primitives.Controls.Toast;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class ToastTests
{
    static ToastTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        // Guarded try/catch (rather than a bare null-check) because xunit runs test classes in
        // parallel on separate STA threads: another test class's static ctor can win the race.
        if (Application.Current is null)
        {
            try
            {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            catch (InvalidOperationException)
            {
                // Another test class's static ctor already created the process-wide Application.
            }
        }
    }

    private static readonly MethodInfo OnClickMethod =
        typeof(ButtonBase).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance)!;

    /// <summary>Same technique as CheckboxTests.SimulateClick: invokes the protected, most-derived
    /// OnClick() without depending on a live visual tree or real input routing.</summary>
    private static void SimulateClick(ButtonBase button) => OnClickMethod.Invoke(button, null);

    private static ResourceDictionary CreateThemedScope()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Toast.xaml"),
        });

        return scope;
    }

    // --- Test double: manual-advance IToastTimer, so ToastManager's queueing/priority/pause
    // logic is testable without a live Dispatcher. ---

    private sealed class ManualToastTimer : IToastTimer
    {
        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }
        public TimeSpan LastStartedDuration { get; private set; }
        public bool StopCalled { get; private set; }
        public bool DisposeCalled { get; private set; }

        private Action? _onElapsed;

        public void Start(TimeSpan duration, Action onElapsed)
        {
            _onElapsed = onElapsed;
            LastStartedDuration = duration;
            IsRunning = duration > TimeSpan.Zero;
            IsPaused = false;
        }

        public void Pause()
        {
            if (IsRunning)
            {
                IsPaused = true;
            }
        }

        public void Resume()
        {
            if (IsRunning)
            {
                IsPaused = false;
            }
        }

        public void Stop()
        {
            StopCalled = true;
            IsRunning = false;
            IsPaused = false;
            _onElapsed = null;
        }

        public void Dispose()
        {
            DisposeCalled = true;
            Stop();
        }

        /// <summary>Simulates the countdown reaching zero; no-ops while paused or not running.</summary>
        public void Elapse()
        {
            if (!IsRunning || IsPaused)
            {
                return;
            }

            var callback = _onElapsed;
            IsRunning = false;
            callback?.Invoke();
        }
    }

    /// <summary>Manager wired to manual timers, in Add() call order (timers[0] is the first
    /// toast's timer, etc).</summary>
    private static (ToastManager Manager, List<ManualToastTimer> Timers) NewManager(
        int limit = 1, TimeSpan? defaultDuration = null)
    {
        var timers = new List<ManualToastTimer>();
        var manager = new ToastManager(limit, defaultDuration, () =>
        {
            var timer = new ManualToastTimer();
            timers.Add(timer);
            return timer;
        });
        return (manager, timers);
    }

    // --- Add / visibility / ordering ---

    [Fact]
    public void Add_ReturnsHandleForNewToast()
    {
        var (manager, _) = NewManager();

        var handle = manager.Add(new ToastOptions { Title = "Saved" });

        Assert.NotEqual(Guid.Empty, handle.Id);
        Assert.Single(manager.Toasts);
        Assert.Equal(handle.Id, manager.Toasts[0].Id);
    }

    [Fact]
    public void Add_WithinLimit_IsVisibleImmediately()
    {
        var (manager, _) = NewManager(limit: 2);

        manager.Add(new ToastOptions { Title = "A" });

        Assert.False(manager.Toasts[0].Limited);
        Assert.Single(manager.VisibleToasts);
    }

    [Fact]
    public void Add_BeyondLimit_IsQueued()
    {
        var (manager, _) = NewManager(limit: 1);

        manager.Add(new ToastOptions { Title = "A" });
        var second = manager.Add(new ToastOptions { Title = "B" });

        var queued = manager.Toasts.Single(t => t.Id == second.Id);
        Assert.True(queued.Limited);
        Assert.Single(manager.VisibleToasts);
        Assert.DoesNotContain(manager.VisibleToasts, t => t.Id == second.Id);
    }

    [Fact]
    public void VisibleToasts_OrderedNewestFirst()
    {
        var (manager, _) = NewManager(limit: 3);

        var a = manager.Add(new ToastOptions { Title = "A" });
        var b = manager.Add(new ToastOptions { Title = "B" });
        var c = manager.Add(new ToastOptions { Title = "C" });

        Assert.Equal(new[] { c.Id, b.Id, a.Id }, manager.VisibleToasts.Select(t => t.Id));
    }

    // --- Regression #1: a queued toast must be promoted (not silently dropped) once a visible
    // slot frees up. The web review history caught exactly this at Limit=1: dismissing the
    // visible toast left the queued one permanently limited instead of taking its place. ---

    [Fact]
    public void Dismiss_AtLimitOne_PromotesQueuedToast_RegressionForHistoricalDropBug()
    {
        var (manager, timers) = NewManager(limit: 1);

        var first = manager.Add(new ToastOptions { Title = "First" });
        var second = manager.Add(new ToastOptions { Title = "Second" });

        Assert.False(manager.Toasts.Single(t => t.Id == first.Id).Limited);
        Assert.True(manager.Toasts.Single(t => t.Id == second.Id).Limited);
        Assert.DoesNotContain(manager.VisibleToasts, t => t.Id == second.Id);

        manager.Dismiss(first);

        var promoted = manager.Toasts.Single(t => t.Id == second.Id);
        Assert.False(promoted.Limited);
        Assert.Contains(manager.VisibleToasts, t => t.Id == second.Id);
        // Promotion arms a fresh timer for the newly-visible toast, same as any other visible one.
        Assert.True(timers[1].IsRunning);
    }

    [Fact]
    public void Limit_IncreasedAtRuntime_PromotesQueuedToasts()
    {
        var (manager, _) = NewManager(limit: 1);

        manager.Add(new ToastOptions { Title = "A" });
        var b = manager.Add(new ToastOptions { Title = "B" });
        Assert.Single(manager.VisibleToasts);

        manager.Limit = 2;

        Assert.Equal(2, manager.VisibleToasts.Count);
        Assert.False(manager.Toasts.Single(t => t.Id == b.Id).Limited);
    }

    [Fact]
    public void Limit_DecreasedAtRuntime_DemotesAndStopsTimer()
    {
        var (manager, timers) = NewManager(limit: 2);

        manager.Add(new ToastOptions { Title = "A" });
        var b = manager.Add(new ToastOptions { Title = "B" });
        Assert.Equal(2, manager.VisibleToasts.Count);

        manager.Limit = 1;

        Assert.True(manager.Toasts.Single(t => t.Id == b.Id).Limited);
        Assert.Single(manager.VisibleToasts);
        Assert.True(timers[1].StopCalled);
    }

    [Fact]
    public void Constructor_LimitBelowOne_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ToastManager(limit: 0));
    }

    [Fact]
    public void LimitSetter_BelowOne_Throws()
    {
        var (manager, _) = NewManager();

        Assert.Throws<ArgumentOutOfRangeException>(() => manager.Limit = 0);
    }

    // --- Regression #2: updating a toast (the promise-style success/error transition) must
    // rearm the auto-dismiss timer. The historical web bug: a promise's terminal success/error
    // toast never auto-dismissed because the prior (sticky, Loading) timer state stuck around. ---

    [Fact]
    public void Update_LoadingToSuccess_RearmsAutoDismissTimer_RegressionForHistoricalStuckBug()
    {
        var (manager, timers) = NewManager(limit: 1, defaultDuration: TimeSpan.FromSeconds(5));

        var handle = manager.Add(new ToastOptions
        {
            Title = "Saving...",
            Type = ToastType.Loading,
            Duration = TimeSpan.Zero, // sticky while the promise is pending
        });

        Assert.False(timers[0].IsRunning);

        manager.Update(handle, new ToastOptions { Title = "Saved", Type = ToastType.Success });

        Assert.True(timers[0].IsRunning);
        Assert.Equal(TimeSpan.FromSeconds(5), timers[0].LastStartedDuration);

        // Prove the auto-dismiss actually fires now, not just that Start() was called.
        timers[0].Elapse();
        Assert.Empty(manager.Toasts);
    }

    [Fact]
    public void Update_ReplacesOptionsAndBumpsUpdateKey()
    {
        var (manager, _) = NewManager();
        var handle = manager.Add(new ToastOptions { Title = "A" });
        var before = manager.Toasts[0].UpdateKey;

        manager.Update(handle, new ToastOptions { Title = "B" });

        Assert.Equal("B", manager.Toasts[0].Options.Title);
        Assert.Equal(before + 1, manager.Toasts[0].UpdateKey);
    }

    [Fact]
    public void Update_UnknownHandle_IsNoOp()
    {
        var (manager, _) = NewManager();
        var other = new ToastManager();
        var foreignHandle = other.Add(new ToastOptions { Title = "Elsewhere" });

        manager.Update(foreignHandle, new ToastOptions { Title = "X" });

        Assert.Empty(manager.Toasts);
    }

    // --- Duration semantics ---

    [Fact]
    public void Duration_Null_UsesManagerDefault()
    {
        var (manager, timers) = NewManager(defaultDuration: TimeSpan.FromMilliseconds(1234));

        manager.Add(new ToastOptions { Title = "A" });

        Assert.Equal(TimeSpan.FromMilliseconds(1234), timers[0].LastStartedDuration);
    }

    [Fact]
    public void Duration_Zero_IsSticky_TimerNeverStarts()
    {
        var (manager, timers) = NewManager();

        manager.Add(new ToastOptions { Title = "A", Duration = TimeSpan.Zero });

        Assert.False(timers[0].IsRunning);
    }

    [Fact]
    public void Timer_Elapsing_DismissesToast()
    {
        var (manager, timers) = NewManager(defaultDuration: TimeSpan.FromMilliseconds(10));

        manager.Add(new ToastOptions { Title = "A" });
        timers[0].Elapse();

        Assert.Empty(manager.Toasts);
    }

    // --- Dismiss / Clear ---

    [Fact]
    public void Dismiss_RemovesToastAndStopsTimer()
    {
        var (manager, timers) = NewManager();
        var handle = manager.Add(new ToastOptions { Title = "A" });

        manager.Dismiss(handle);

        Assert.Empty(manager.Toasts);
        Assert.True(timers[0].StopCalled);
        Assert.True(timers[0].DisposeCalled);
    }

    [Fact]
    public void Dismiss_UnknownHandle_IsNoOp()
    {
        var (manager, _) = NewManager();
        var other = new ToastManager();
        var foreignHandle = other.Add(new ToastOptions { Title = "Elsewhere" });

        manager.Dismiss(foreignHandle);

        Assert.Empty(manager.Toasts); // unaffected, no throw
    }

    [Fact]
    public void Clear_RemovesAllToastsAndStopsAllTimers()
    {
        var (manager, timers) = NewManager(limit: 5);
        manager.Add(new ToastOptions { Title = "A" });
        manager.Add(new ToastOptions { Title = "B" });

        manager.Clear();

        Assert.Empty(manager.Toasts);
        Assert.All(timers, t => Assert.True(t.StopCalled));
    }

    [Fact]
    public void Changed_RaisedOnAddUpdateDismissClear()
    {
        var (manager, _) = NewManager(limit: 5);
        var raised = 0;
        manager.Changed += () => raised++;

        var handle = manager.Add(new ToastOptions { Title = "A" });
        manager.Update(handle, new ToastOptions { Title = "B" });
        manager.Dismiss(handle);
        manager.Add(new ToastOptions { Title = "C" });
        manager.Clear();

        Assert.Equal(5, raised);
    }

    // --- Pause / Resume (hover, focus-within, window-blur all route through these) ---

    [Fact]
    public void Pause_StopsCountdown_Resume_RestartsIt()
    {
        var (manager, timers) = NewManager();
        var handle = manager.Add(new ToastOptions { Title = "A" });

        manager.Pause(handle);
        Assert.True(timers[0].IsPaused);

        manager.Resume(handle);
        Assert.False(timers[0].IsPaused);
    }

    [Fact]
    public void Pause_FromTwoSources_RequiresTwoResumesBeforeCountdownContinues()
    {
        // Models hover + focus-within pausing the same toast simultaneously: mouse leaving
        // while focus is still inside must not prematurely resume the timer.
        var (manager, timers) = NewManager();
        var handle = manager.Add(new ToastOptions { Title = "A" });

        manager.Pause(handle); // hover enter
        manager.Pause(handle); // focus enter
        manager.Resume(handle); // hover leave
        Assert.True(timers[0].IsPaused);

        manager.Resume(handle); // focus leave
        Assert.False(timers[0].IsPaused);
    }

    [Fact]
    public void PauseAll_OnlyPausesVisibleToasts()
    {
        var (manager, timers) = NewManager(limit: 1);
        manager.Add(new ToastOptions { Title = "Visible" });
        manager.Add(new ToastOptions { Title = "Queued" });

        manager.PauseAll();

        Assert.True(timers[0].IsPaused);
        Assert.False(timers[1].IsPaused); // queued toast was never running to begin with
    }

    // --- NaviusToast: template, close/action wiring, automation peer ---

    [StaFact]
    public void NaviusToast_ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var scope = CreateThemedScope();
        var toast = new NaviusToast { Resources = scope, Title = "Hello" };

        Assert.True(toast.ApplyTemplate());
    }

    [StaFact]
    public void NaviusToast_CloseButtonClick_RaisesCloseRequested()
    {
        var scope = CreateThemedScope();
        var toast = new NaviusToast { Resources = scope, Title = "Hello" };
        toast.ApplyTemplate();

        var closeButton = (ButtonBase)toast.GetType()
            .GetMethod("GetTemplateChild", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(toast, new object[] { "PART_Close" })!;

        var raised = false;
        toast.CloseRequested += (_, _) => raised = true;

        SimulateClick(closeButton);

        Assert.True(raised);
    }

    [StaFact]
    public void NaviusToast_ActionButtonClick_RaisesActionRequested()
    {
        var scope = CreateThemedScope();
        var toast = new NaviusToast { Resources = scope, Title = "Hello", ActionLabel = "Undo" };
        toast.ApplyTemplate();

        var actionButton = (ButtonBase)toast.GetType()
            .GetMethod("GetTemplateChild", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(toast, new object[] { "PART_Action" })!;

        var raised = false;
        toast.ActionRequested += (_, _) => raised = true;

        SimulateClick(actionButton);

        Assert.True(raised);
    }

    [StaFact]
    public void NaviusToastAutomationPeer_LiveSetting_MatchesPriority()
    {
        var low = new NaviusToast { Priority = ToastPriority.Low };
        var high = new NaviusToast { Priority = ToastPriority.High };

        var lowPeer = new NaviusToastAutomationPeer(low);
        var highPeer = new NaviusToastAutomationPeer(high);

        Assert.Equal(AutomationLiveSetting.Polite, GetLiveSetting(lowPeer));
        Assert.Equal(AutomationLiveSetting.Assertive, GetLiveSetting(highPeer));
    }

    private static AutomationLiveSetting GetLiveSetting(NaviusToastAutomationPeer peer)
    {
        var method = typeof(NaviusToastAutomationPeer).GetMethod(
            "GetLiveSettingCore", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (AutomationLiveSetting)method.Invoke(peer, null)!;
    }

    // --- NaviusToastViewport: manager-driven visual creation, index/offset publishing ---

    private static NaviusToastViewport NewViewport(ResourceDictionary scope)
    {
        var viewport = new NaviusToastViewport { Resources = scope };
        viewport.ApplyTemplate();
        return viewport;
    }

    [StaFact]
    public void NaviusToastViewport_ApplyTemplate_Succeeds()
    {
        // NewViewport() already applies the template once; ApplyTemplate() only returns true
        // when it actually (re)builds the visual tree, so assert on the template itself here
        // rather than calling ApplyTemplate() a second time (which correctly returns false).
        var viewport = NewViewport(CreateThemedScope());

        Assert.NotNull(viewport.Template);
    }

    [StaFact]
    public void NaviusToastViewport_ManagerAdd_CreatesOneVisualPerVisibleToast()
    {
        var scope = CreateThemedScope();
        var viewport = NewViewport(scope);
        var manager = new ToastManager(limit: 2);

        viewport.Manager = manager;
        manager.Add(new ToastOptions { Title = "A" });
        manager.Add(new ToastOptions { Title = "B" });

        var panel = GetPanel(viewport);
        Assert.Equal(2, panel.Children.Count);
    }

    [StaFact]
    public void NaviusToastViewport_QueuedToast_DoesNotGetAVisual()
    {
        var scope = CreateThemedScope();
        var viewport = NewViewport(scope);
        var manager = new ToastManager(limit: 1);

        viewport.Manager = manager;
        manager.Add(new ToastOptions { Title = "A" });
        manager.Add(new ToastOptions { Title = "B" }); // queued, Limit=1

        var panel = GetPanel(viewport);
        Assert.Single(panel.Children);
    }

    [StaFact]
    public void NaviusToastViewport_Reflow_PublishesIndexAndOffsetYAttachedProperties()
    {
        var scope = CreateThemedScope();
        var viewport = NewViewport(scope);
        var manager = new ToastManager(limit: 2);

        viewport.Manager = manager;
        manager.Add(new ToastOptions { Title = "Older" });
        manager.Add(new ToastOptions { Title = "Newer" });

        var panel = GetPanel(viewport);
        // VisibleToasts is newest-first, so child order in the panel follows Add() order but
        // the *attached* Index must reflect newest-first stacking (index 0 = frontmost = newest).
        var newerElement = panel.Children
            .OfType<NaviusToast>()
            .Single(t => t.Title == "Newer");
        var olderElement = panel.Children
            .OfType<NaviusToast>()
            .Single(t => t.Title == "Older");

        Assert.Equal(0, NaviusToastViewport.GetIndex(newerElement));
        Assert.Equal(1, NaviusToastViewport.GetIndex(olderElement));
        Assert.Equal(0d, NaviusToastViewport.GetOffsetY(newerElement));
    }

    [StaFact]
    public void NaviusToastViewport_Dismiss_RemovesToastFromManagerVisibleSet()
    {
        var scope = CreateThemedScope();
        var viewport = NewViewport(scope);
        var manager = new ToastManager(limit: 2);

        viewport.Manager = manager;
        var handle = manager.Add(new ToastOptions { Title = "A" });

        handle.Dismiss();

        // The visual removal itself is gated on the exit Storyboard completing (a real animation
        // clock, which this headless test has no render loop to pump), so only the manager-level
        // state -- what NaviusToastViewport.Sync() reacts to -- is asserted here.
        Assert.Empty(manager.VisibleToasts);
    }

    private static Canvas GetPanel(NaviusToastViewport viewport)
    {
        var method = typeof(NaviusToastViewport).GetMethod(
            "GetTemplateChild", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (Canvas)method.Invoke(viewport, new object[] { "PART_Panel" })!;
    }
}
