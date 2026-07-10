using System.Collections.Generic;
using System.Windows.Automation;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls.Combobox;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Interaction logic for ComboboxPage.xaml. Builds the generic NaviusCombobox&lt;string&gt; in code
/// (a generic control is fiddlier to declare via x:TypeArguments in classic WPF XAML) and hosts it in
/// the XAML-declared DemoHost placeholder. AutomationId "ComboboxDemo" is set on the real control so
/// an external FlaUI e2e harness can find it.
/// </summary>
public partial class ComboboxPage : UserControl
{
    private static readonly IReadOnlyList<string> Fruits = new[]
    {
        "Apple", "Apricot", "Banana", "Blackberry", "Blueberry", "Cherry", "Cranberry",
        "Date", "Fig", "Grape", "Grapefruit", "Kiwi", "Lemon", "Lime", "Mango",
        "Nectarine", "Orange", "Papaya", "Peach", "Pear", "Pineapple", "Plum",
        "Raspberry", "Strawberry", "Tangerine", "Watermelon",
    };

    public ComboboxPage()
    {
        InitializeComponent();

        var combobox = new NaviusCombobox<string>
        {
            Multiple = true,
            Placeholder = "Search fruits...",
            Items = Fruits,
            // Pre-populated chips deliberately in a different order than the filtered list will show,
            // to exercise the chip-remove-by-value behavior (removal must not depend on displayed index).
            Values = new[] { "Cherry", "Apple", "Mango" },
        };

        AutomationProperties.SetAutomationId(combobox, "ComboboxDemo");
        DemoHost.Content = combobox;
    }
}
