using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Navius.Wpf.Primitives.Controls.NavigationMenu;

/// <summary>
/// Tier B: a small downward-triangle marker, styled via Fill/Stroke like any other Shape, meant
/// to sit inside a Content panel's chrome pointing at the active Trigger. Width/Height reuse
/// FrameworkElement's own properties (default overridden to 10x5, matching the contract) rather
/// than adding redundant new DPs.
/// </summary>
public class NaviusNavigationMenuArrow : Shape
{
    static NaviusNavigationMenuArrow()
    {
        WidthProperty.OverrideMetadata(typeof(NaviusNavigationMenuArrow), new FrameworkPropertyMetadata(10.0));
        HeightProperty.OverrideMetadata(typeof(NaviusNavigationMenuArrow), new FrameworkPropertyMetadata(5.0));
    }

    protected override Geometry DefiningGeometry
    {
        get
        {
            var figure = new PathFigure { StartPoint = new Point(0, 0), IsClosed = true };
            figure.Segments.Add(new LineSegment(new Point(Width, 0), true));
            figure.Segments.Add(new LineSegment(new Point(Width / 2, Height), true));

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            return geometry;
        }
    }
}
