using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Timeline;

/// <summary>The title line of a NaviusTimelineItem's NaviusTimelineContent.</summary>
public class NaviusTimelineTitle : ContentControl
{
    static NaviusTimelineTitle()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusTimelineTitle), new FrameworkPropertyMetadata(typeof(NaviusTimelineTitle)));
    }
}
