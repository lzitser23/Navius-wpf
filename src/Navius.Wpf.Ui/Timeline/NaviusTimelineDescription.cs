using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Timeline;

/// <summary>The muted description line of a NaviusTimelineItem's NaviusTimelineContent.</summary>
public class NaviusTimelineDescription : ContentControl
{
    static NaviusTimelineDescription()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusTimelineDescription), new FrameworkPropertyMetadata(typeof(NaviusTimelineDescription)));
    }
}
