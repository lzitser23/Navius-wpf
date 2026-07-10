using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.Field;

/// <summary>Groups one item's label + description, e.g. inside a checkbox/radio list.</summary>
public class NaviusFieldItem : ContentControl
{
    static NaviusFieldItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusFieldItem), new FrameworkPropertyMetadata(typeof(NaviusFieldItem)));
    }
}
