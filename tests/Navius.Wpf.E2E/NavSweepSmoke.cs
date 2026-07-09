using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using FlaUI.UIA3;

namespace Navius.Wpf.E2E;

public class NavSweepSmoke
{
    /// <summary>
    /// Selects every nav entry once. Catches pages that throw during construction
    /// or whose templates fail to resolve at runtime (window would die or hang).
    /// </summary>
    [Fact]
    public void EveryGalleryPage_Loads()
    {
        var exe = Path.Combine(AppContext.BaseDirectory, "Navius.Wpf.Gallery.exe");
        using var app = FlaUI.Core.Application.Launch(exe);
        try
        {
            using var automation = new UIA3Automation();
            var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(15));
            Assert.NotNull(window);

            var nav = window.FindFirstDescendant(cf => cf.ByAutomationId("Nav")).AsListBox();
            Assert.NotNull(nav);
            Assert.True(nav.Items.Length >= 21, $"Expected 21+ nav entries, found {nav.Items.Length}.");

            foreach (var item in nav.Items)
            {
                item.Select();
                // The window must stay alive and responsive after each page swap.
                var alive = Retry.WhileFalse(
                    () => !app.HasExited && window.FindFirstDescendant(cf => cf.ByAutomationId("Nav")) is not null,
                    TimeSpan.FromSeconds(5)).Result;
                Assert.True(alive, $"Gallery died or hung after selecting page '{item.Name}'.");
            }
        }
        finally
        {
            app.Kill();
        }
    }
}
