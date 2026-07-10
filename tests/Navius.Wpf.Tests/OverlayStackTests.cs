using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Navius.Wpf.Primitives.Overlays;

namespace Navius.Wpf.Tests;

public class OverlayStackTests : IDisposable
{
    static OverlayStackTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        // Guarded try/catch (rather than a bare null-check, see ThemeManagerTests) because
        // xunit runs test classes in parallel on separate STA threads: another test class's
        // static ctor can win the same check-then-construct race.
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

    private static OverlayStack NewStack() => OverlayStack.GetFor(new Window());

    [StaFact]
    public void GetFor_ReturnsSameInstanceForSameWindow()
    {
        var window = new Window();

        var first = OverlayStack.GetFor(window);
        var second = OverlayStack.GetFor(window);

        Assert.Same(first, second);
    }

    [StaFact]
    public void GetFor_ReturnsDifferentInstancesForDifferentWindows()
    {
        var a = OverlayStack.GetFor(new Window());
        var b = OverlayStack.GetFor(new Window());

        Assert.NotSame(a, b);
    }

    [StaFact]
    public void Push_AssignsIncreasingStackIndexes()
    {
        var stack = NewStack();

        var s0 = stack.Push(new Grid(), new OverlayOptions());
        var s1 = stack.Push(new Grid(), new OverlayOptions());
        var s2 = stack.Push(new Grid(), new OverlayOptions());

        Assert.Equal(0, s0.StackIndex);
        Assert.Equal(1, s1.StackIndex);
        Assert.Equal(2, s2.StackIndex);
        Assert.Same(s2, stack.Topmost);
        Assert.Equal(new[] { s0, s1, s2 }, stack.Sessions);
    }

    [StaFact]
    public void RequestClose_PopsAndReindexesRemainingSessions()
    {
        var stack = NewStack();

        var s0 = stack.Push(new Grid(), new OverlayOptions());
        var s1 = stack.Push(new Grid(), new OverlayOptions());
        var s2 = stack.Push(new Grid(), new OverlayOptions());

        var closed = s1.RequestClose(OverlayCloseReason.Programmatic);

        Assert.True(closed);
        Assert.True(s1.IsClosed);
        Assert.Equal(new[] { s0, s2 }, stack.Sessions);
        Assert.Equal(0, s0.StackIndex);
        Assert.Equal(1, s2.StackIndex);
        Assert.Same(s2, stack.Topmost);
    }

    [StaFact]
    public void RequestClose_PoppingLastSessionEmptiesStack()
    {
        var stack = NewStack();
        var s0 = stack.Push(new Grid(), new OverlayOptions());

        s0.RequestClose(OverlayCloseReason.Programmatic);

        Assert.Empty(stack.Sessions);
        Assert.Null(stack.Topmost);
    }

    [StaFact]
    public void RequestClose_CancelingClosingBlocksClose()
    {
        var stack = NewStack();
        var session = stack.Push(new Grid(), new OverlayOptions());

        var closedRaised = false;
        session.Closing += (_, args) => args.Cancel = true;
        session.Closed += (_, _) => closedRaised = true;

        var result = session.RequestClose(OverlayCloseReason.EscapeKey);

        Assert.False(result);
        Assert.False(session.IsClosed);
        Assert.False(closedRaised);
        Assert.Single(stack.Sessions);
    }

    [StaFact]
    public void RequestClose_WhenNotCanceled_RaisesClosedAndStaysRemoved()
    {
        var stack = NewStack();
        var session = stack.Push(new Grid(), new OverlayOptions());

        var closedRaised = false;
        session.Closed += (_, _) => closedRaised = true;

        var result = session.RequestClose(OverlayCloseReason.Programmatic);

        Assert.True(result);
        Assert.True(closedRaised);
        Assert.Empty(stack.Sessions);
    }

    [StaFact]
    public void RequestClose_IsIdempotentOnceClosed()
    {
        var stack = NewStack();
        var session = stack.Push(new Grid(), new OverlayOptions());

        var firstClosedCount = 0;
        session.Closed += (_, _) => firstClosedCount++;

        Assert.True(session.RequestClose(OverlayCloseReason.Programmatic));
        Assert.True(session.RequestClose(OverlayCloseReason.Programmatic));

        Assert.Equal(1, firstClosedCount);
    }

    // --- OverlayDismissPolicy: topmost-only Escape policy, tested directly (no real key events) ---

    [StaFact]
    public void FindEscapeTarget_ReturnsTopmostSessionWithCloseOnEscape()
    {
        var stack = NewStack();
        stack.Push(new Grid(), new OverlayOptions { CloseOnEscape = true });
        var top = stack.Push(new Grid(), new OverlayOptions { CloseOnEscape = true });

        var target = OverlayDismissPolicy.FindEscapeTarget(stack.Sessions);

        Assert.Same(top, target);
    }

    [StaFact]
    public void FindEscapeTarget_SkipsTopSessionsThatOptOut()
    {
        var stack = NewStack();
        var eligible = stack.Push(new Grid(), new OverlayOptions { CloseOnEscape = true });
        stack.Push(new Grid(), new OverlayOptions { CloseOnEscape = false });

        var target = OverlayDismissPolicy.FindEscapeTarget(stack.Sessions);

        Assert.Same(eligible, target);
    }

    [StaFact]
    public void FindEscapeTarget_ReturnsNullWhenNoSessionOptsIn()
    {
        var stack = NewStack();
        stack.Push(new Grid(), new OverlayOptions { CloseOnEscape = false });
        stack.Push(new Grid(), new OverlayOptions { CloseOnEscape = false });

        var target = OverlayDismissPolicy.FindEscapeTarget(stack.Sessions);

        Assert.Null(target);
    }

    [StaFact]
    public void FindEscapeTarget_ReturnsNullForEmptyStack()
    {
        var target = OverlayDismissPolicy.FindEscapeTarget(NewStack().Sessions);

        Assert.Null(target);
    }

    // --- OverlayDismissPolicy: outside-press routing ---

    [StaFact]
    public void FindOutsidePressTarget_ClosesTopmostEligibleSessionWhenPressIsOutsideAllRoots()
    {
        var stack = NewStack();
        var target = stack.Push(new Grid(), new OverlayOptions { CloseOnOutsideClick = true });

        var result = OverlayDismissPolicy.FindOutsidePressTarget(stack.Sessions, _ => false);

        Assert.Same(target, result);
    }

    [StaFact]
    public void FindOutsidePressTarget_ReturnsNullWhenPressIsInsideCandidateRoot()
    {
        var stack = NewStack();
        var target = stack.Push(new Grid(), new OverlayOptions { CloseOnOutsideClick = true });

        var result = OverlayDismissPolicy.FindOutsidePressTarget(stack.Sessions, s => s == target);

        Assert.Null(result);
    }

    [StaFact]
    public void FindOutsidePressTarget_ReturnsNullWhenPressIsInsideAHigherStackedRoot()
    {
        var stack = NewStack();
        var lower = stack.Push(new Grid(), new OverlayOptions { CloseOnOutsideClick = true });
        var higher = stack.Push(new Grid(), new OverlayOptions { CloseOnOutsideClick = false });

        // Press lands inside the higher (non-closable) overlay's root; per spec that counts as
        // "inside" for the lower candidate too, so nothing closes.
        var result = OverlayDismissPolicy.FindOutsidePressTarget(stack.Sessions, s => s == higher);

        Assert.Null(result);
        _ = lower;
    }

    [StaFact]
    public void FindOutsidePressTarget_SkipsIneligibleTopSessionsToFindCandidateBelow()
    {
        var stack = NewStack();
        var candidate = stack.Push(new Grid(), new OverlayOptions { CloseOnOutsideClick = true });
        stack.Push(new Grid(), new OverlayOptions { CloseOnOutsideClick = false });

        var result = OverlayDismissPolicy.FindOutsidePressTarget(stack.Sessions, _ => false);

        Assert.Same(candidate, result);
    }

    [StaFact]
    public void FindOutsidePressTarget_ReturnsNullWhenNoSessionOptsIn()
    {
        var stack = NewStack();
        stack.Push(new Grid(), new OverlayOptions { CloseOnOutsideClick = false });

        var result = OverlayDismissPolicy.FindOutsidePressTarget(stack.Sessions, _ => false);

        Assert.Null(result);
    }

    // --- OverlayDismissPolicy: RestoreFocus guard, a pure decision method ---

    [StaFact]
    public void ShouldRestoreFocus_TrueOnlyWhenRequestedAndFocusStillInside()
    {
        Assert.True(OverlayDismissPolicy.ShouldRestoreFocus(restoreFocusOption: true, focusIsWithinOverlaySubtree: true));
        Assert.False(OverlayDismissPolicy.ShouldRestoreFocus(restoreFocusOption: true, focusIsWithinOverlaySubtree: false));
        Assert.False(OverlayDismissPolicy.ShouldRestoreFocus(restoreFocusOption: false, focusIsWithinOverlaySubtree: true));
        Assert.False(OverlayDismissPolicy.ShouldRestoreFocus(restoreFocusOption: false, focusIsWithinOverlaySubtree: false));
    }

    // --- OverlaySession.RegisterInputRoot: Escape/outside-press routing inside a Popup's own HwndSource ---

    // KeyEventArgs requires a non-null PresentationSource; a hidden native window (never shown,
    // style = 0 means no WS_VISIBLE bit) is the lightest real one available headlessly. Lazily
    // created (not a static field initializer) and disposed per test instance -- this dummy 0x0
    // native window must not outlive the STA thread it was created on (same pattern as
    // AutocompleteTests/ComboboxTests/etc.; a static field here would be created once on
    // whichever test's thread runs first and then reused -- and never disposed -- across every
    // other test's own distinct STA thread, exactly the cross-thread teardown this suite guards
    // against).
    private System.Windows.Interop.HwndSource? _testSource;

    private PresentationSource TestSource =>
        _testSource ??= new System.Windows.Interop.HwndSource(0, 0, 0, 0, 0, "NaviusOverlayStackTests", System.IntPtr.Zero);

    public void Dispose()
    {
        _testSource?.Dispose();
        TestCleanup.PumpDispatcher();
    }

    private KeyEventArgs MakeEscapeKeyDown() =>
        new(Keyboard.PrimaryDevice, TestSource, 0, Key.Escape) { RoutedEvent = Keyboard.PreviewKeyDownEvent };

    private static MouseButtonEventArgs MakePreviewMouseDown() =>
        new(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Mouse.PreviewMouseDownEvent };

    [StaFact]
    public void RegisterInputRoot_EscapeRaisedOnRegisteredRoot_ClosesTheSession()
    {
        var stack = NewStack();
        var session = stack.Push(new Grid(), new OverlayOptions { CloseOnEscape = true });
        var popupRoot = new Grid();

        session.RegisterInputRoot(popupRoot);
        popupRoot.RaiseEvent(MakeEscapeKeyDown());

        Assert.True(session.IsClosed);
    }

    [StaFact]
    public void RegisterInputRoot_PressInsideRegisteredRoot_CountsAsInsideForItsOwnOutsidePressCheck()
    {
        var stack = NewStack();
        var session = stack.Push(new Grid(), new OverlayOptions { CloseOnOutsideClick = true });
        var popupRoot = new Grid();

        session.RegisterInputRoot(popupRoot);
        popupRoot.RaiseEvent(MakePreviewMouseDown());

        Assert.False(session.IsClosed);
    }

    [StaFact]
    public void RegisterInputRoot_PressInsideAHigherSessionsRegisteredRoot_BlocksALowerCandidateToo()
    {
        var stack = NewStack();
        var lower = stack.Push(new Grid(), new OverlayOptions { CloseOnOutsideClick = true });
        var higher = stack.Push(new Grid(), new OverlayOptions { CloseOnOutsideClick = false });
        var higherPopupRoot = new Grid();

        higher.RegisterInputRoot(higherPopupRoot);
        // Press lands inside the higher (non-closable) overlay's registered input root; per the
        // existing "at or above" policy this counts as inside for the lower candidate too.
        higherPopupRoot.RaiseEvent(MakePreviewMouseDown());

        Assert.False(lower.IsClosed);
        Assert.False(higher.IsClosed);
    }

    [StaFact]
    public void RegisterInputRoot_WindowLevelOutsidePress_StillClosesUnrelatedRegisteredRootSession()
    {
        var window = new Window();
        var stack = OverlayStack.GetFor(window);
        var session = stack.Push(new Grid(), new OverlayOptions { CloseOnOutsideClick = true });
        session.RegisterInputRoot(new Grid());

        var args = MakePreviewMouseDown();
        window.RaiseEvent(args);

        Assert.True(session.IsClosed);
    }

    [StaFact]
    public void RegisterInputRoot_IsIdempotentForTheSameElement()
    {
        var stack = NewStack();
        var session = stack.Push(new Grid(), new OverlayOptions());
        var popupRoot = new Grid();

        session.RegisterInputRoot(popupRoot);
        session.RegisterInputRoot(popupRoot);

        Assert.Single(session.InputRoots);
    }

    [StaFact]
    public void RegisterInputRoot_NoOpsOnceSessionIsClosed()
    {
        var stack = NewStack();
        var session = stack.Push(new Grid(), new OverlayOptions());
        session.RequestClose(OverlayCloseReason.Programmatic);

        session.RegisterInputRoot(new Grid());

        Assert.Empty(session.InputRoots);
    }

    [StaFact]
    public void RequestClose_UnregistersInputRoots_SoLaterEscapeOnThatRootDoesNothing()
    {
        var stack = NewStack();
        var session = stack.Push(new Grid(), new OverlayOptions { CloseOnEscape = true });
        var popupRoot = new Grid();
        session.RegisterInputRoot(popupRoot);

        session.RequestClose(OverlayCloseReason.Programmatic);
        Assert.Empty(session.InputRoots);

        // Detached: raising Escape on the now-unregistered root must not throw or resurrect anything.
        var exception = Record.Exception(() => popupRoot.RaiseEvent(MakeEscapeKeyDown()));
        Assert.Null(exception);
    }
}
