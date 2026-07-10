using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Timeline;

/// <summary>
/// A vertical timeline: an ItemsControl hosting NaviusTimelineItem entries in a single column.
/// Vertical-only, per this project's scope (the web contract's Horizontal/Alternate layouts are
/// not ported here).
/// </summary>
public class NaviusTimeline : ItemsControl
{
    static NaviusTimeline()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusTimeline), new FrameworkPropertyMetadata(typeof(NaviusTimeline)));
    }
}
