using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Timeline;

/// <summary>
/// The hairline connecting a NaviusTimelineDot to the next item's dot. Decorative: place one
/// below a NaviusTimelineDot inside every item's Header except the last (which has nothing to
/// connect to).
/// </summary>
public class NaviusTimelineConnector : Control
{
    static NaviusTimelineConnector()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusTimelineConnector), new FrameworkPropertyMetadata(typeof(NaviusTimelineConnector)));
    }
}
