using System.Windows;
using Navius.Wpf.Primitives.Positioning;

namespace Navius.Wpf.Tests;

public class PlacementMathTests
{
    private static readonly Rect WorkArea = new(0, 0, 1000, 800);
    private static readonly Rect Anchor = new(400, 300, 100, 40);
    private static readonly Size Popup = new(120, 60);

    [Fact]
    public void Bottom_Center_PositionsBelowAnchor_Centered()
    {
        var options = new AnchoredPlacementOptions { Side = PlacementSide.Bottom, Align = PlacementAlign.Center };

        var result = PlacementMath.Place(Anchor, Popup, WorkArea, options);

        Assert.Equal(new Point(390, 340), result.Origin);
        Assert.Equal(PlacementSide.Bottom, result.EffectiveSide);
        Assert.Equal(PlacementAlign.Center, result.Align);
    }

    [Fact]
    public void Top_Center_PositionsAboveAnchor()
    {
        var options = new AnchoredPlacementOptions { Side = PlacementSide.Top, Align = PlacementAlign.Center };

        var result = PlacementMath.Place(Anchor, Popup, WorkArea, options);

        Assert.Equal(new Point(390, 240), result.Origin);
        Assert.Equal(PlacementSide.Top, result.EffectiveSide);
    }

    [Fact]
    public void Left_Center_PositionsLeftOfAnchor()
    {
        var options = new AnchoredPlacementOptions { Side = PlacementSide.Left, Align = PlacementAlign.Center };

        var result = PlacementMath.Place(Anchor, Popup, WorkArea, options);

        Assert.Equal(new Point(280, 290), result.Origin);
        Assert.Equal(PlacementSide.Left, result.EffectiveSide);
    }

    [Fact]
    public void Right_Center_PositionsRightOfAnchor()
    {
        var options = new AnchoredPlacementOptions { Side = PlacementSide.Right, Align = PlacementAlign.Center };

        var result = PlacementMath.Place(Anchor, Popup, WorkArea, options);

        Assert.Equal(new Point(500, 290), result.Origin);
        Assert.Equal(PlacementSide.Right, result.EffectiveSide);
    }

    [Fact]
    public void Bottom_Start_AlignsToAnchorLeftEdge()
    {
        var options = new AnchoredPlacementOptions { Side = PlacementSide.Bottom, Align = PlacementAlign.Start };

        var result = PlacementMath.Place(Anchor, Popup, WorkArea, options);

        Assert.Equal(new Point(400, 340), result.Origin);
    }

    [Fact]
    public void Bottom_End_AlignsToAnchorRightEdge()
    {
        var options = new AnchoredPlacementOptions { Side = PlacementSide.Bottom, Align = PlacementAlign.End };

        var result = PlacementMath.Place(Anchor, Popup, WorkArea, options);

        Assert.Equal(new Point(380, 340), result.Origin);
    }

    [Fact]
    public void SideOffset_AddsGapOnMainAxis()
    {
        var options = new AnchoredPlacementOptions { Side = PlacementSide.Bottom, Align = PlacementAlign.Center, SideOffset = 10 };

        var result = PlacementMath.Place(Anchor, Popup, WorkArea, options);

        Assert.Equal(new Point(390, 350), result.Origin);
    }

    [Fact]
    public void AlignOffset_ShiftsOnCrossAxis()
    {
        var options = new AnchoredPlacementOptions { Side = PlacementSide.Bottom, Align = PlacementAlign.Center, AlignOffset = 15 };

        var result = PlacementMath.Place(Anchor, Popup, WorkArea, options);

        Assert.Equal(new Point(405, 340), result.Origin);
    }

    [Fact]
    public void Flip_WhenCrampedOnBottom_FlipsToTop()
    {
        var anchor = new Rect(400, 760, 100, 20);
        var options = new AnchoredPlacementOptions { Side = PlacementSide.Bottom, Align = PlacementAlign.Center };

        var result = PlacementMath.Place(anchor, Popup, WorkArea, options);

        Assert.Equal(PlacementSide.Top, result.EffectiveSide);
        Assert.Equal(new Point(390, 700), result.Origin);
    }

    [Fact]
    public void NoFlip_WhenOppositeSideIsAlsoWorse()
    {
        var shortWorkArea = new Rect(0, 0, 1000, 50);
        var anchor = new Rect(400, 15, 100, 30);
        var options = new AnchoredPlacementOptions { Side = PlacementSide.Top, Align = PlacementAlign.Center };

        var result = PlacementMath.Place(anchor, Popup, shortWorkArea, options);

        // Space above (15) beats space below (5), so Top stays even though it doesn't fully fit.
        Assert.Equal(PlacementSide.Top, result.EffectiveSide);
        Assert.Equal(new Point(390, -45), result.Origin);
    }

    [Fact]
    public void FlipDisabled_KeepsRequestedSide_EvenWhenCramped()
    {
        var anchor = new Rect(400, 760, 100, 20);
        var options = new AnchoredPlacementOptions { Side = PlacementSide.Bottom, Align = PlacementAlign.Center, FlipEnabled = false };

        var result = PlacementMath.Place(anchor, Popup, WorkArea, options);

        Assert.Equal(PlacementSide.Bottom, result.EffectiveSide);
        Assert.Equal(new Point(390, 780), result.Origin);
    }

    [Fact]
    public void Shift_ClampsAtRightEdge_WhenPopupWouldOverflowRight()
    {
        var narrowWorkArea = new Rect(0, 0, 500, 800);
        var anchor = new Rect(450, 300, 40, 20);
        var options = new AnchoredPlacementOptions { Side = PlacementSide.Bottom, Align = PlacementAlign.Center };

        var result = PlacementMath.Place(anchor, Popup, narrowWorkArea, options);

        Assert.Equal(new Point(380, 320), result.Origin);
    }

    [Fact]
    public void Shift_ClampsAtLeftEdge_WhenPopupWouldOverflowLeft()
    {
        var anchor = new Rect(10, 300, 40, 20);
        var options = new AnchoredPlacementOptions { Side = PlacementSide.Bottom, Align = PlacementAlign.Center };

        var result = PlacementMath.Place(anchor, Popup, WorkArea, options);

        Assert.Equal(new Point(0, 320), result.Origin);
    }

    [Fact]
    public void ShiftDisabled_AllowsPopupToOverflowWorkArea()
    {
        var narrowWorkArea = new Rect(0, 0, 500, 800);
        var anchor = new Rect(450, 300, 40, 20);
        var options = new AnchoredPlacementOptions { Side = PlacementSide.Bottom, Align = PlacementAlign.Center, ShiftEnabled = false };

        var result = PlacementMath.Place(anchor, Popup, narrowWorkArea, options);

        Assert.Equal(new Point(410, 320), result.Origin);
    }

    [Fact]
    public void ArrowOffset_CentersOnAnchor_WhenRoomAvailable()
    {
        var options = new AnchoredPlacementOptions { Side = PlacementSide.Bottom, Align = PlacementAlign.Center, ArrowSize = 16 };

        var result = PlacementMath.Place(Anchor, Popup, WorkArea, options);

        Assert.Equal(new Point(52, 0), result.ArrowOffset);
    }

    [Fact]
    public void ArrowOffset_ClampsAtPopupEdge_WhenAnchorCenterIsFarFromPopup()
    {
        var wideWorkArea = new Rect(0, 0, 2000, 800);
        var wideAnchor = new Rect(400, 300, 300, 40);
        var options = new AnchoredPlacementOptions { Side = PlacementSide.Bottom, Align = PlacementAlign.Start, ArrowSize = 16 };

        var result = PlacementMath.Place(wideAnchor, Popup, wideWorkArea, options);

        Assert.Equal(new Point(400, 340), result.Origin);
        Assert.Equal(new Point(104, 0), result.ArrowOffset);
    }

    [Fact]
    public void ArrowOffset_IsNull_WhenArrowSizeIsZero()
    {
        var options = new AnchoredPlacementOptions { Side = PlacementSide.Bottom, Align = PlacementAlign.Center };

        var result = PlacementMath.Place(Anchor, Popup, WorkArea, options);

        Assert.Null(result.ArrowOffset);
    }
}
