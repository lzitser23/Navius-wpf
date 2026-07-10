using System;
using System.Windows;

namespace Navius.Wpf.Primitives.Positioning;

/// <summary>
/// Pure, deterministic anchored-positioning math: given an anchor rect, a popup size, a
/// work area, and placement options, computes where the popup should be drawn. Mirrors
/// floating-ui's flip + shift + arrow middleware semantics. All inputs must already be in
/// the same coordinate space (e.g. device-independent screen pixels); this class does no
/// visual-tree, DPI, or window/monitor work.
/// </summary>
public static class PlacementMath
{
    public static PlacementResult Place(Rect anchor, Size popup, Rect workArea, AnchoredPlacementOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var side = options.Side;

        if (options.FlipEnabled && !Fits(side, anchor, popup, workArea, options.SideOffset))
        {
            var opposite = Opposite(side);
            if (AvailableSpace(opposite, anchor, workArea) > AvailableSpace(side, anchor, workArea))
            {
                side = opposite;
            }
        }

        var origin = Origin(side, options.Align, anchor, popup, options.SideOffset, options.AlignOffset);

        if (options.ShiftEnabled)
        {
            origin = Shift(side, origin, popup, workArea);
        }

        Point? arrowOffset = options.ArrowSize > 0
            ? ArrowOffset(side, origin, anchor, popup, options.ArrowSize)
            : null;

        return new PlacementResult(origin, side, options.Align, arrowOffset);
    }

    private static bool Fits(PlacementSide side, Rect anchor, Size popup, Rect workArea, double sideOffset)
    {
        var required = MainAxisSize(side, popup) + sideOffset;
        return AvailableSpace(side, anchor, workArea) >= required;
    }

    private static double MainAxisSize(PlacementSide side, Size popup) =>
        side is PlacementSide.Top or PlacementSide.Bottom ? popup.Height : popup.Width;

    private static double AvailableSpace(PlacementSide side, Rect anchor, Rect workArea) => side switch
    {
        PlacementSide.Top => anchor.Top - workArea.Top,
        PlacementSide.Bottom => workArea.Bottom - anchor.Bottom,
        PlacementSide.Left => anchor.Left - workArea.Left,
        PlacementSide.Right => workArea.Right - anchor.Right,
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };

    private static PlacementSide Opposite(PlacementSide side) => side switch
    {
        PlacementSide.Top => PlacementSide.Bottom,
        PlacementSide.Bottom => PlacementSide.Top,
        PlacementSide.Left => PlacementSide.Right,
        PlacementSide.Right => PlacementSide.Left,
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };

    private static Point Origin(PlacementSide side, PlacementAlign align, Rect anchor, Size popup, double sideOffset, double alignOffset)
    {
        double x, y;

        switch (side)
        {
            case PlacementSide.Top:
                y = anchor.Top - popup.Height - sideOffset;
                x = AlignCross(align, anchor.Left, anchor.Width, popup.Width, alignOffset);
                break;
            case PlacementSide.Bottom:
                y = anchor.Bottom + sideOffset;
                x = AlignCross(align, anchor.Left, anchor.Width, popup.Width, alignOffset);
                break;
            case PlacementSide.Left:
                x = anchor.Left - popup.Width - sideOffset;
                y = AlignCross(align, anchor.Top, anchor.Height, popup.Height, alignOffset);
                break;
            case PlacementSide.Right:
                x = anchor.Right + sideOffset;
                y = AlignCross(align, anchor.Top, anchor.Height, popup.Height, alignOffset);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(side));
        }

        return new Point(x, y);
    }

    private static double AlignCross(PlacementAlign align, double anchorStart, double anchorExtent, double popupExtent, double alignOffset) => align switch
    {
        PlacementAlign.Start => anchorStart + alignOffset,
        PlacementAlign.Center => anchorStart + (anchorExtent - popupExtent) / 2 + alignOffset,
        PlacementAlign.End => anchorStart + anchorExtent - popupExtent + alignOffset,
        _ => throw new ArgumentOutOfRangeException(nameof(align)),
    };

    private static Point Shift(PlacementSide side, Point origin, Size popup, Rect workArea)
    {
        var x = origin.X;
        var y = origin.Y;

        if (side is PlacementSide.Top or PlacementSide.Bottom)
        {
            x = ClampAxis(x, popup.Width, workArea.Left, workArea.Right);
        }
        else
        {
            y = ClampAxis(y, popup.Height, workArea.Top, workArea.Bottom);
        }

        return new Point(x, y);
    }

    private static double ClampAxis(double start, double extent, double min, double max)
    {
        if (start + extent > max)
        {
            start = max - extent;
        }

        if (start < min)
        {
            start = min;
        }

        return start;
    }

    private static Point ArrowOffset(PlacementSide side, Point origin, Rect anchor, Size popup, double arrowSize)
    {
        var anchorCenter = new Point(anchor.Left + anchor.Width / 2, anchor.Top + anchor.Height / 2);

        if (side is PlacementSide.Top or PlacementSide.Bottom)
        {
            var x = ClampArrow(anchorCenter.X - origin.X - arrowSize / 2, popup.Width, arrowSize);
            var y = side == PlacementSide.Top ? popup.Height : 0;
            return new Point(x, y);
        }

        var clampedY = ClampArrow(anchorCenter.Y - origin.Y - arrowSize / 2, popup.Height, arrowSize);
        var arrowX = side == PlacementSide.Left ? popup.Width : 0;
        return new Point(arrowX, clampedY);
    }

    private static double ClampArrow(double value, double popupExtent, double arrowSize)
    {
        var max = Math.Max(0, popupExtent - arrowSize);

        if (value < 0)
        {
            return 0;
        }

        return value > max ? max : value;
    }
}
