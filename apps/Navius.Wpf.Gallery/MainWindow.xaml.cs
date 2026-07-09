using System.Windows;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Gallery;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnToggleTheme(object sender, RoutedEventArgs e)
    {
        var next = ThemeManager.Current == NaviusTheme.Light ? NaviusTheme.Dark : NaviusTheme.Light;
        ThemeManager.Apply(next);
        ThemeLabel.Text = next.ToString();
    }
}
