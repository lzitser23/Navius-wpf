using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Timeline;

/// <summary>
/// One row of a NaviusTimeline. A HeaderedContentControl rather than a plain ContentControl: the
/// row genuinely needs two independent slots laid out side by side: Header holds the rail (a
/// NaviusTimelineDot, plus a NaviusTimelineConnector on every item except the last), Content
/// holds the NaviusTimelineContent block (title/description). WPF's HeaderedContentControl
/// already models exactly that two-slot shape, so it is reused rather than inventing a bespoke
/// two-content control.
/// </summary>
public class NaviusTimelineItem : HeaderedContentControl
{
    static NaviusTimelineItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusTimelineItem), new FrameworkPropertyMetadata(typeof(NaviusTimelineItem)));
    }
}
