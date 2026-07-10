using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using FlaUI.UIA3;

namespace Navius.Wpf.E2E;

public class GalleryThemeSmoke
{
    [Fact]
    public void ThemeToggle_SwitchesTheme()
    {
        var exe = Path.Combine(AppContext.BaseDirectory, "Navius.Wpf.Gallery.exe");
        using var app = FlaUI.Core.Application.Launch(exe);
        try
        {
            using var automation = new UIA3Automation();
            var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(15));
            Assert.NotNull(window);

            var label = window.FindFirstDescendant(cf => cf.ByAutomationId("ThemeLabel"));
            Assert.NotNull(label);
            Assert.Equal("Light", label.Name);

            var toggle = window.FindFirstDescendant(cf => cf.ByAutomationId("ThemeToggle"));
            Assert.NotNull(toggle);
            toggle.AsButton().Invoke();

            var flipped = Retry.WhileFalse(
                () => window.FindFirstDescendant(cf => cf.ByAutomationId("ThemeLabel"))?.Name == "Dark",
                TimeSpan.FromSeconds(5)).Result;
            Assert.True(flipped, "Theme label did not flip to Dark after invoking the toggle.");
        }
        finally
        {
            app.Kill();
        }
    }
}
