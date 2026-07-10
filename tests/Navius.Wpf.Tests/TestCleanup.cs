using System.Windows.Threading;

namespace Navius.Wpf.Tests;

/// <summary>
/// Shared teardown helper for STA WPF tests (Xunit.StaFact gives every [StaFact]/[StaTheory]
/// test its own dedicated STA thread + Dispatcher, torn down right after the test method and its
/// IDisposable.Dispose() return). Closing a WPF <see cref="System.Windows.Controls.Primitives.Popup"/>
/// (IsOpen = false) only hides it synchronously; the underlying native window's actual teardown
/// is queued as a background-priority Dispatcher operation that a test's thread never pumps on
/// its own. Left unpumped, that operation -- and the popup's native HWND -- survives past the
/// owning thread's exit and is later finalized cross-thread (by the GC finalizer thread, on some
/// unrelated later test's turn), which crashes the process with a native NullReferenceException
/// in HwndSubclass.SubclassWndProc. Calling this from a test class's Dispose() flushes any such
/// pending teardown deterministically, on the correct thread, before that thread exits.
/// </summary>
internal static class TestCleanup
{
    public static void PumpDispatcher() =>
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
}
