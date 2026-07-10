using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Empty;

/// <summary>
/// An empty-state placeholder (dashed-border panel). Compositional: nest NaviusEmptyMedia,
/// NaviusEmptyTitle and NaviusEmptyDescription inside a vertical StackPanel as the single
/// Content, matching the web contract's child-content model.
/// </summary>
public class NaviusEmpty : ContentControl
{
    static NaviusEmpty()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusEmpty), new FrameworkPropertyMetadata(typeof(NaviusEmpty)));
    }
}
