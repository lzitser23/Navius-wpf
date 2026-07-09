using System.Windows.Controls;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Interaction logic for FieldPage.xaml (covers the Fieldset family too)
/// </summary>
public partial class FieldPage : UserControl
{
    public FieldPage()
    {
        InitializeComponent();

        RevealButton.Click += (_, _) => InvalidField.Reveal();
    }
}
