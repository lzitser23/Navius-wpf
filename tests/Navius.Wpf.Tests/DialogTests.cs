using System.Windows;
using System.Windows.Threading;
using Navius.Wpf.Primitives.Controls.Dialog;
using Navius.Wpf.Primitives.Controls.OverlaySurface;
using Navius.Wpf.Primitives.Overlays;

namespace Navius.Wpf.Tests;

public class DialogTests
{
    static DialogTests()
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

    [StaFact]
    public void Defaults_AreClosedAndModal()
    {
        var dialog = new NaviusDialog();

        Assert.False(dialog.IsOpen);
        Assert.True(dialog.Modal);
        Assert.True(dialog.CloseOnOutsideClick);
        Assert.Equal(Visibility.Collapsed, dialog.Visibility);
    }

    [StaFact]
    public void TitleAndDescription_RoundTrip()
    {
        var dialog = new NaviusDialog { Title = "Edit profile", Description = "Update your name and email." };

        Assert.Equal("Edit profile", dialog.Title);
        Assert.Equal("Update your name and email.", dialog.Description);
    }

    [StaFact]
    public void IsOpen_WithoutAHostWindow_RevertsToFalse()
    {
        // No NaviusOverlayLayer (and not even a Window) in the tree: Engage() cannot find a
        // place to render, so it must log and bounce the two-way IsOpen DP back to false rather
        // than claim to be open with nothing shown.
        var dialog = new NaviusDialog();

        dialog.IsOpen = true;

        Assert.False(dialog.IsOpen);
    }

    [StaFact]
    public void Open_WithoutAHostWindow_LeavesDialogClosed()
    {
        var dialog = new NaviusDialog();

        dialog.Open();

        Assert.False(dialog.IsOpen);
    }

    [StaFact]
    public void Open_WithoutAHostWindow_DoesNotRaiseOpened()
    {
        var dialog = new NaviusDialog();
        var raised = false;
        dialog.Opened += (_, _) => raised = true;

        dialog.Open();

        Assert.False(raised);
    }

    [StaFact]
    public void Close_WhenAlreadyClosed_IsANoOp()
    {
        var dialog = new NaviusDialog();

        dialog.Close();

        Assert.False(dialog.IsOpen);
    }

    [StaFact]
    public void ModalEffective_TracksTheModalProperty()
    {
        var probe = new ProbeDialog { Modal = false };

        Assert.False(probe.ModalEffectivePublic);
        Assert.False(probe.TrapFocusEffectivePublic);

        probe.Modal = true;

        Assert.True(probe.ModalEffectivePublic);
        Assert.True(probe.TrapFocusEffectivePublic);
    }

    [StaFact]
    public void CloseOnOutsideClickEffective_TracksTheProperty()
    {
        var probe = new ProbeDialog { CloseOnOutsideClick = false };

        Assert.False(probe.CloseOnOutsideClickEffectivePublic);
    }

    [StaFact]
    public void CloseCommand_IsSharedAcrossOverlaySurfaces()
    {
        Assert.NotNull(NaviusOverlaySurfaceBase.CloseCommand);
        Assert.Equal(nameof(NaviusOverlaySurfaceBase.CloseCommand), NaviusOverlaySurfaceBase.CloseCommand.Name);
    }

    [StaFact]
    public void CancelingClosing_KeepsTheDialogOpenAndIsOpenStaysTrue()
    {
        // Regression (M6): a Closing handler that sets Cancel = true must keep the surface open
        // AND leave IsOpen == true. Previously OnIsOpenChanged ignored RequestClose's return, so a
        // canceled close left the session open but the IsOpen DP stuck at false (a lying property,
        // and any later Open() was swallowed because _session was still non-null).
        var dialog = new NaviusDialog { Title = "Discard changes?" };
        var layer = new NaviusOverlayLayer();
        layer.Children.Add(dialog);
        var window = new Window
        {
            Content = layer,
            Width = 60,
            Height = 60,
            ShowActivated = false,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
        };

        try
        {
            window.Show();
            // Flush the Loaded queue so NaviusOverlayLayer registers itself for this window.
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.Loaded);

            dialog.Open();
            Assert.True(dialog.IsOpen);
            Assert.NotNull(OverlayStack.GetFor(window).Topmost);

            dialog.Closing += (_, e) => e.Cancel = true;

            dialog.Close();

            Assert.True(dialog.IsOpen);
            Assert.Equal(Visibility.Visible, dialog.Visibility);
            Assert.NotNull(OverlayStack.GetFor(window).Topmost);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Reopen_DuringExitAnimation_KeepsTheNewSessionOpen()
    {
        // Regression (DEFECT 1): opening again within the 150ms exit fade must NOT let the OLD
        // exit-completion callback tear down the freshly engaged session. Previously the stale
        // callback ran layer.RemoveSurface / Visibility = Collapsed / IsOpen = false
        // unconditionally once the exit animation elapsed, collapsing the reopened dialog.
        var dialog = new NaviusDialog { Title = "Rapid reopen" };
        var layer = new NaviusOverlayLayer();
        layer.Children.Add(dialog);
        var window = new Window
        {
            Content = layer,
            Width = 60,
            Height = 60,
            ShowActivated = false,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
        };

        try
        {
            window.Show();
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.Loaded);

            dialog.Open();
            Assert.True(dialog.IsOpen);

            // Close then immediately reopen, inside the exit animation window.
            dialog.Close();
            dialog.Open();

            Assert.True(dialog.IsOpen);

            // Pump past the exit (and enter) durations so any pending animation callbacks fire.
            PumpFor(window.Dispatcher, TimeSpan.FromMilliseconds(350));

            Assert.True(dialog.IsOpen);
            Assert.Equal(Visibility.Visible, dialog.Visibility);
            Assert.NotNull(OverlayStack.GetFor(window).Topmost);
        }
        finally
        {
            window.Close();
        }
    }

    private static void PumpFor(Dispatcher dispatcher, TimeSpan duration)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(duration, DispatcherPriority.Background, (_, _) => frame.Continue = false, dispatcher);
        timer.Start();
        Dispatcher.PushFrame(frame);
        timer.Stop();
    }

    /// <summary>Exposes the protected *_Effective hooks so tests can assert on them without widening the public API.</summary>
    private sealed class ProbeDialog : NaviusDialog
    {
        public bool ModalEffectivePublic => ModalEffective;

        public bool CloseOnOutsideClickEffectivePublic => CloseOnOutsideClickEffective;

        public bool TrapFocusEffectivePublic => TrapFocusEffective;
    }
}
