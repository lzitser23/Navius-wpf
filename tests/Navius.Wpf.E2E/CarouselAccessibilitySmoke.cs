using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;

namespace Navius.Wpf.E2E;

public class CarouselAccessibilitySmoke
{
    [Fact]
    public void Inactive_slide_descendants_leave_the_uia_tree_after_transition()
    {
        var exe = Path.Combine(AppContext.BaseDirectory, "Navius.Wpf.Gallery.exe");
        using var app = FlaUI.Core.Application.Launch(exe);
        try
        {
            using var automation = new UIA3Automation();
            var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(15));
            Assert.NotNull(window);

            var nav = Find(window, "Nav").AsListBox();
            nav.Items.First(item => item.Name == "UiComposite").Select();

            var carousel = Find(window, "CompositeCarousel");
            Assert.NotNull(carousel.FindFirstDescendant(cf => cf.ByAutomationId("CarouselSlide1Action")));
            Assert.Null(carousel.FindFirstDescendant(cf => cf.ByAutomationId("CarouselSlide2Action")));

            carousel.Focus();
            Keyboard.Type(VirtualKeyShort.RIGHT);

            var swapped = Retry.WhileFalse(
                () => carousel.FindFirstDescendant(cf => cf.ByAutomationId("CarouselSlide1Action")) is null
                    && carousel.FindFirstDescendant(cf => cf.ByAutomationId("CarouselSlide2Action")) is not null,
                TimeSpan.FromSeconds(5)).Result;
            Assert.True(swapped, "Inactive slide descendants remained in the UIA tree after the transition.");
        }
        finally
        {
            app.Kill();
        }
    }

    private static AutomationElement Find(AutomationElement root, string automationId)
    {
        var element = Retry.WhileNull(
            () => root.FindFirstDescendant(cf => cf.ByAutomationId(automationId)),
            TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(element);
        return element;
    }
}
