using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls.Autocomplete;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Interaction logic for AutocompletePage.xaml. The demo control is generic
/// (<see cref="NaviusAutocomplete{TItem}"/>), so it is built in code-behind and dropped into the
/// XAML-declared Host container rather than declared with x:TypeArguments.
/// </summary>
public partial class AutocompletePage : UserControl
{
    private static readonly string[] Cities =
    {
        "Amsterdam", "Athens", "Barcelona", "Berlin", "Bratislava", "Brussels", "Budapest",
        "Copenhagen", "Dublin", "Helsinki", "Lisbon", "London", "Madrid", "Oslo", "Paris",
        "Prague", "Reykjavik", "Rome", "Stockholm", "Tallinn", "Vienna", "Warsaw", "Zurich",
    };

    public AutocompletePage()
    {
        InitializeComponent();

        var autocomplete = new NaviusAutocomplete<string>
        {
            Width = 280,
            Items = Cities,
            Placeholder = "Search a city...",
        };
        AutomationProperties.SetAutomationId(autocomplete, "AutocompleteDemo");

        Host.Content = autocomplete;
    }
}
