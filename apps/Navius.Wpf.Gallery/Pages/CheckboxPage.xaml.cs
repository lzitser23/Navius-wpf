using System.Windows.Controls;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Interaction logic for CheckboxPage.xaml
/// </summary>
public partial class CheckboxPage : UserControl
{
    public CheckboxPage()
    {
        InitializeComponent();

        // AllValues is IReadOnlyList<string> (no XAML type converter); wired here instead.
        GroupRoot.AllValues = new[] { "a", "b", "c" };
    }
}
