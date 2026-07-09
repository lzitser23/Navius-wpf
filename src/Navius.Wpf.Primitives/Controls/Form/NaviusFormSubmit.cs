using System.Windows;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls.Internal;

namespace Navius.Wpf.Primitives.Controls.Form;

/// <summary>
/// Tier A: a native Button wired to the ancestor NaviusForm's SubmitCommand, replacing the
/// web contract's native-submit-plus-preventDefault model with an explicit command. Only
/// auto-wires Command when the consumer hasn't already bound one, so an explicit
/// Command="{Binding SubmitCommand, ElementName=...}" in XAML still wins.
/// </summary>
public class NaviusFormSubmit : Button
{
    static NaviusFormSubmit()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusFormSubmit), new FrameworkPropertyMetadata(typeof(NaviusFormSubmit)));
    }

    public NaviusFormSubmit()
    {
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Command is not null)
        {
            return;
        }

        var form = VisualAncestorWalker.FindAncestor<NaviusForm>(this);
        if (form is not null)
        {
            Command = form.SubmitCommand;
        }
    }
}
