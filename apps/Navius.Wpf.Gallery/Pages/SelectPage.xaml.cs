using System.Windows.Automation;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls.Select;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Interaction logic for SelectPage.xaml. The two demo controls are built here because
/// NaviusSelect&lt;TItem&gt; is generic (code-behind construction is less fragile than
/// x:TypeArguments markup). Their AutomationIds ("SelectDemo" / "SelectMultiDemo") are set on the
/// real instances so an external FlaUI harness can look them up.
/// </summary>
public partial class SelectPage : UserControl
{
    private static readonly (string Value, string Text)[] Fruits =
    {
        ("apple", "Apple"),
        ("banana", "Banana"),
        ("cherry", "Cherry"),
        ("grape", "Grape"),
        ("mango", "Mango"),
        ("orange", "Orange"),
    };

    public SelectPage()
    {
        InitializeComponent();

        var single = new NaviusSelect<string> { Placeholder = "Pick a fruit" };
        AutomationProperties.SetAutomationId(single, "SelectDemo");
        foreach (var (value, text) in Fruits)
        {
            single.Items.Add(new NaviusSelectItem { Value = value, TextValue = text });
        }

        SingleHost.Content = single;

        var multi = new NaviusSelect<string> { Multiple = true, Placeholder = "Pick fruits" };
        AutomationProperties.SetAutomationId(multi, "SelectMultiDemo");
        foreach (var (value, text) in Fruits)
        {
            multi.Items.Add(new NaviusSelectItem { Value = value, TextValue = text });
        }

        MultiHost.Content = multi;
    }
}
