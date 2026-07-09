using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Timeline;

/// <summary>
/// The text block beside a NaviusTimeline rail. A StackPanel subclass (fixed to Vertical) so
/// NaviusTimelineTitle and NaviusTimelineDescription can be nested directly as siblings, mirroring
/// NaviusItemContent.
/// </summary>
public class NaviusTimelineContent : StackPanel
{
    static NaviusTimelineContent()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusTimelineContent), new FrameworkPropertyMetadata(typeof(NaviusTimelineContent)));
    }

    public NaviusTimelineContent()
    {
        Orientation = Orientation.Vertical;
    }
}
