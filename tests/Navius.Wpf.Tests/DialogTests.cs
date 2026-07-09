using System.Windows;
using Navius.Wpf.Primitives.Controls.Dialog;
using Navius.Wpf.Primitives.Controls.OverlaySurface;

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

    /// <summary>Exposes the protected *_Effective hooks so tests can assert on them without widening the public API.</summary>
    private sealed class ProbeDialog : NaviusDialog
    {
        public bool ModalEffectivePublic => ModalEffective;

        public bool CloseOnOutsideClickEffectivePublic => CloseOnOutsideClickEffective;

        public bool TrapFocusEffectivePublic => TrapFocusEffective;
    }
}
