using System.Windows;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Gallery;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ThemeManager.Apply(NaviusTheme.Light);
    }
}
