using System;
using System.Windows;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls;
using Navius.Wpf.Primitives.Positioning;

namespace Navius.Wpf.Tests;

/// <summary>
/// Covers the ArrowOffset surface added to <see cref="NaviusAnchoredPopup"/> (see
/// docs/parity/popover.md, "ArrowOffset surface (M3)"): DP defaults, round-trip, and the
/// attached mirror onto <see cref="NaviusAnchoredPopup.Child"/>.
///
/// A full live-placement assertion (setting Anchor/Child/IsOpen and checking that
/// ArrowOffsetX/Y are populated from a real <see cref="PlacementMath.Place"/> pass) is not
/// covered here: like EffectiveSide elsewhere in this control, UpdatePlacement() only runs once
/// Anchor has a real PresentationSource (a shown/hosted HwndSource with the anchor as
/// RootVisual), which no existing test in this suite establishes. PlacementMath's own
/// arrow-offset math is covered directly in PlacementMathTests.
/// </summary>
public class AnchoredPopupTests
{
    static AnchoredPopupTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        // Guarded try/catch (rather than a bare null-check) because xunit runs test classes in
        // parallel on separate STA threads: another test class's static ctor can win the race.
        if (Application.Current is null)
        {
            try
            {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            catch (InvalidOperationException)
            {
                // Another test class's static ctor already created the process-wide Application.
            }
        }
    }

    [StaFact]
    public void Defaults_MatchTheContract()
    {
        var popup = new NaviusAnchoredPopup();

        Assert.Equal(0d, popup.ArrowSize);
        Assert.True(double.IsNaN(popup.ArrowOffsetX));
        Assert.True(double.IsNaN(popup.ArrowOffsetY));
        Assert.Equal(PlacementSide.Bottom, popup.EffectiveSide);
    }

    [StaFact]
    public void ArrowSize_RoundTrips()
    {
        var popup = new NaviusAnchoredPopup { ArrowSize = 12 };

        Assert.Equal(12d, popup.ArrowSize);
    }

    [StaFact]
    public void ArrowOffsetX_ArrowOffsetY_AreReadOnly()
    {
        Assert.True(NaviusAnchoredPopup.ArrowOffsetXProperty.ReadOnly);
        Assert.True(NaviusAnchoredPopup.ArrowOffsetYProperty.ReadOnly);
    }

    [StaFact]
    public void GetArrowOffsetXText_DefaultsToNaN_OnAnyElement()
    {
        var element = new TextBlock();

        Assert.True(double.IsNaN(NaviusAnchoredPopup.GetArrowOffsetXText(element)));
    }

    [StaFact]
    public void GetArrowOffsetYText_DefaultsToNaN_OnAnyElement()
    {
        var element = new TextBlock();

        Assert.True(double.IsNaN(NaviusAnchoredPopup.GetArrowOffsetYText(element)));
    }

    [StaFact]
    public void GetArrowOffsetXText_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => NaviusAnchoredPopup.GetArrowOffsetXText(null!));
    }

    [StaFact]
    public void GetArrowOffsetYText_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => NaviusAnchoredPopup.GetArrowOffsetYText(null!));
    }

    [StaFact]
    public void SettingArrowSize_WithNoAnchor_DoesNotThrow()
    {
        // ArrowSize is wired through OnPlacementInputChanged (same as Side/Align/SideOffset/
        // AlignOffset), which calls UpdatePlacement(); UpdatePlacement() must no-op safely when
        // Anchor/Child/IsOpen aren't all set yet, same as every other placement-input DP.
        var popup = new NaviusAnchoredPopup { Child = new TextBlock() };

        popup.ArrowSize = 16;

        Assert.Equal(16d, popup.ArrowSize);
        Assert.True(double.IsNaN(popup.ArrowOffsetX));
    }
}
