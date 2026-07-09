using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;

namespace Navius.Wpf.E2E;

/// <summary>
/// The M3 gate: the web e2e keyboard matrix for Select/Combobox, driven over real UIA.
/// </summary>
public class KeyboardMatrixSmoke
{
    private static readonly string[] Fruits = ["Apple", "Banana", "Cherry", "Grape", "Mango", "Orange"];

    [Fact]
    public void Select_Single_KeyboardMatrix()
    {
        RunOnPage("Select", (window, automation) =>
        {
            var select = Find(window, "SelectDemo");
            select.Focus();

            // Closed + ArrowDown opens; navigate and commit with Enter.
            Keyboard.Type(VirtualKeyShort.DOWN);
            Keyboard.Type(VirtualKeyShort.DOWN);
            Keyboard.Type(VirtualKeyShort.RETURN);

            var selected = WaitForDisplayText(window, "SelectDemo",
                text => Fruits.Contains(text));
            Assert.True(selected, "Enter did not commit a fruit selection.");

            // Reopen, End then Home then Escape: selection must not change on Escape.
            var before = DisplayText(window, "SelectDemo");
            Keyboard.Type(VirtualKeyShort.DOWN);
            Keyboard.Type(VirtualKeyShort.END);
            Keyboard.Type(VirtualKeyShort.HOME);
            Keyboard.Type(VirtualKeyShort.ESCAPE);
            Assert.Equal(before, DisplayText(window, "SelectDemo"));
        });
    }

    [Fact]
    public void Select_Multiple_TogglesAndStaysOpen()
    {
        RunOnPage("Select", (window, automation) =>
        {
            var multi = Find(window, "SelectMultiDemo");
            multi.Focus();

            // Open, toggle first item, move, toggle second item (popup stays open in
            // Multiple mode), close with Escape: summary joins both selections.
            Keyboard.Type(VirtualKeyShort.DOWN);
            Keyboard.Type(VirtualKeyShort.RETURN);
            Keyboard.Type(VirtualKeyShort.DOWN);
            Keyboard.Type(VirtualKeyShort.RETURN);
            Keyboard.Type(VirtualKeyShort.ESCAPE);

            var joined = WaitForDisplayText(window, "SelectMultiDemo",
                text => text.Contains(", "));
            Assert.True(joined, "Multiple-select summary did not join two selections.");
        });
    }

    [Fact]
    public void Combobox_TypeToOpen_EscapeReverts()
    {
        RunOnPage("Combobox", (window, automation) =>
        {
            var combobox = Find(window, "ComboboxDemo");
            var input = combobox.FindFirstDescendant(cf => cf.ByControlType(ControlType.Edit));
            Assert.NotNull(input);
            input.Focus();

            Keyboard.Type("ap");
            var opened = Retry.WhileFalse(
                () => Find(window, "ComboboxDemo").FindFirstDescendant(
                    cf => cf.ByControlType(ControlType.Edit))?.AsTextBox().Text == "ap",
                TimeSpan.FromSeconds(5)).Result;
            Assert.True(opened, "Typing did not land in the combobox input.");

            // Escape closes AND reverts the query text.
            Keyboard.Type(VirtualKeyShort.ESCAPE);
            var reverted = Retry.WhileFalse(
                () => Find(window, "ComboboxDemo").FindFirstDescendant(
                    cf => cf.ByControlType(ControlType.Edit))?.AsTextBox().Text?.Length == 0,
                TimeSpan.FromSeconds(5)).Result;
            Assert.True(reverted, "Escape did not revert the combobox query.");
        });
    }

    private static void RunOnPage(string navEntry, Action<Window, UIA3Automation> body)
    {
        var exe = Path.Combine(AppContext.BaseDirectory, "Navius.Wpf.Gallery.exe");
        using var app = FlaUI.Core.Application.Launch(exe);
        try
        {
            using var automation = new UIA3Automation();
            var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(15));
            Assert.NotNull(window);

            var nav = window.FindFirstDescendant(cf => cf.ByAutomationId("Nav")).AsListBox();
            nav.Items.First(i => i.Name == navEntry).Select();

            body(window, automation);
        }
        finally
        {
            app.Kill();
        }
    }

    private static AutomationElement Find(Window window, string automationId)
    {
        var element = Retry.WhileNull(
            () => window.FindFirstDescendant(cf => cf.ByAutomationId(automationId)),
            TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(element);
        return element;
    }

    // Reads the control's UIA ValuePattern: the same thing Narrator announces.
    private static string DisplayText(Window window, string automationId)
        => Find(window, automationId).Patterns.Value.Pattern.Value.Value ?? string.Empty;

    private static bool WaitForDisplayText(Window window, string automationId, Func<string, bool> predicate)
        => Retry.WhileFalse(
            () => predicate(DisplayText(window, automationId)),
            TimeSpan.FromSeconds(5)).Result;
}
