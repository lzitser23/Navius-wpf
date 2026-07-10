using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;

namespace Navius.Wpf.E2E;

public class GatePopupSmoke
{
    [Fact]
    public void GatePopup_OpensAnimated_ClosesOnEscape()
    {
        var exe = Path.Combine(AppContext.BaseDirectory, "Navius.Wpf.Gallery.exe");
        using var app = FlaUI.Core.Application.Launch(exe);
        try
        {
            using var automation = new UIA3Automation();
            var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(15));
            Assert.NotNull(window);

            var state = window.FindFirstDescendant(cf => cf.ByAutomationId("GatePopupState"));
            Assert.NotNull(state);
            Assert.Equal("Closed", state.Name);

            window.FindFirstDescendant(cf => cf.ByAutomationId("GateOpen")).AsButton().Invoke();

            var opened = Retry.WhileFalse(
                () => window.FindFirstDescendant(cf => cf.ByAutomationId("GatePopupState"))?.Name == "Open",
                TimeSpan.FromSeconds(5)).Result;
            Assert.True(opened, "Popup state did not become Open after invoking the trigger.");

            // Focus is trapped inside the popup, so Esc lands on the popup content.
            Keyboard.Type(VirtualKeyShort.ESCAPE);

            var closed = Retry.WhileFalse(
                () => window.FindFirstDescendant(cf => cf.ByAutomationId("GatePopupState"))?.Name == "Closed",
                TimeSpan.FromSeconds(5)).Result;
            Assert.True(closed, "Popup did not close on Escape.");
        }
        finally
        {
            app.Kill();
        }
    }
}
