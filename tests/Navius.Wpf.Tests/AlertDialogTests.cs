using System.Windows;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls.AlertDialog;

namespace Navius.Wpf.Tests;

public class AlertDialogTests
{
    static AlertDialogTests()
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
    public void Defaults_AreClosed()
    {
        var alertDialog = new NaviusAlertDialog();

        Assert.False(alertDialog.IsOpen);
        Assert.Equal(Visibility.Collapsed, alertDialog.Visibility);
    }

    [StaFact]
    public void IsOpen_WithoutAHostWindow_RevertsToFalse()
    {
        var alertDialog = new NaviusAlertDialog();

        alertDialog.IsOpen = true;

        Assert.False(alertDialog.IsOpen);
    }

    [StaFact]
    public void ModalEffective_IsAlwaysTrue_ThereIsNoModalProperty()
    {
        var probe = new ProbeAlertDialog();

        Assert.True(probe.ModalEffectivePublic);
        Assert.True(probe.TrapFocusEffectivePublic);
    }

    [StaFact]
    public void CloseOnOutsideClickEffective_IsAlwaysFalse()
    {
        var probe = new ProbeAlertDialog();

        Assert.False(probe.CloseOnOutsideClickEffectivePublic);
    }

    // --- AlertDialogFocus: pure logical-tree search, tested directly ---

    [StaFact]
    public void FindCancelElement_ReturnsNull_WhenNothingIsMarked()
    {
        var content = new StackPanel { Children = { new Button(), new Button() } };
        var alertDialog = new NaviusAlertDialog { Content = content };

        var found = AlertDialogFocus.FindCancelElement(alertDialog);

        Assert.Null(found);
    }

    [StaFact]
    public void FindCancelElement_ReturnsTheMarkedDescendant()
    {
        var cancel = new Button { Content = "Cancel" };
        NaviusAlertDialog.SetIsCancelButton(cancel, true);
        var content = new StackPanel { Children = { new Button { Content = "Delete" }, cancel } };
        var alertDialog = new NaviusAlertDialog { Content = content };

        var found = AlertDialogFocus.FindCancelElement(alertDialog);

        Assert.Same(cancel, found);
    }

    [StaFact]
    public void FindCancelElement_SkipsADisabledCandidate()
    {
        var cancel = new Button { Content = "Cancel", IsEnabled = false };
        NaviusAlertDialog.SetIsCancelButton(cancel, true);
        var alertDialog = new NaviusAlertDialog { Content = cancel };

        var found = AlertDialogFocus.FindCancelElement(alertDialog);

        Assert.Null(found);
    }

    [StaFact]
    public void IsCancelButton_DefaultsToFalse()
    {
        var button = new Button();

        Assert.False(NaviusAlertDialog.GetIsCancelButton(button));
    }

    /// <summary>Exposes the protected *_Effective hooks so tests can assert on them without widening the public API.</summary>
    private sealed class ProbeAlertDialog : NaviusAlertDialog
    {
        public bool ModalEffectivePublic => ModalEffective;

        public bool CloseOnOutsideClickEffectivePublic => CloseOnOutsideClickEffective;

        public bool TrapFocusEffectivePublic => TrapFocusEffective;
    }
}
