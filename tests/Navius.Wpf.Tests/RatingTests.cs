using System;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Navius.Wpf.Primitives.Controls;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class RatingTests
{
    static RatingTests()
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

    // --- NaviusRatingMath: pure step/clamp/select math ---

    [StaFact]
    public void Math_Step_IsOneWholeOrHalfStep()
    {
        Assert.Equal(1m, NaviusRatingMath.Step(allowHalf: false));
        Assert.Equal(0.5m, NaviusRatingMath.Step(allowHalf: true));
    }

    [StaFact]
    public void Math_StepUp_FromNullStartsAtStep()
    {
        Assert.Equal(1m, NaviusRatingMath.StepUp(null, allowHalf: false, max: 5));
    }

    [StaFact]
    public void Math_StepUp_ClampsToMax()
    {
        Assert.Equal(5m, NaviusRatingMath.StepUp(5m, allowHalf: false, max: 5));
    }

    [StaFact]
    public void Math_StepDown_ClearsBelowStepWhenAllowClear()
    {
        Assert.Null(NaviusRatingMath.StepDown(1m, allowHalf: false, allowClear: true));
    }

    [StaFact]
    public void Math_StepDown_FloorsAtStepWhenClearDisallowed()
    {
        Assert.Equal(1m, NaviusRatingMath.StepDown(1m, allowHalf: false, allowClear: false));
    }

    [StaFact]
    public void Math_StepDown_DecreasesByStep()
    {
        Assert.Equal(2m, NaviusRatingMath.StepDown(3m, allowHalf: false, allowClear: true));
    }

    [StaFact]
    public void Math_Digit_ClampsToMax()
    {
        Assert.Equal(5m, NaviusRatingMath.Digit(9, 5));
        Assert.Equal(3m, NaviusRatingMath.Digit(3, 5));
    }

    [StaFact]
    public void Math_FocusIndex_DefaultsToOneWhenUnrated()
    {
        Assert.Equal(1, NaviusRatingMath.FocusIndex(null, 5));
        Assert.Equal(1, NaviusRatingMath.FocusIndex(0m, 5));
    }

    [StaFact]
    public void Math_FocusIndex_CeilingsFractionalValue()
    {
        Assert.Equal(4, NaviusRatingMath.FocusIndex(3.5m, 5));
    }

    [StaFact]
    public void Math_Select_ReselectingClearsWhenAllowClear()
    {
        Assert.Null(NaviusRatingMath.Select(3m, current: 3m, allowClear: true));
    }

    [StaFact]
    public void Math_Select_ReselectingKeepsValueWhenClearDisallowed()
    {
        Assert.Equal(3m, NaviusRatingMath.Select(3m, current: 3m, allowClear: false));
    }

    [StaFact]
    public void Math_Select_DifferentValueAlwaysSelects()
    {
        Assert.Equal(4m, NaviusRatingMath.Select(4m, current: 3m, allowClear: true));
    }

    // --- NaviusRating: control-level defaults and keyboard model ---

    [StaFact]
    public void DefaultState_IsUnratedWithContractDefaults()
    {
        var rating = new NaviusRating();

        Assert.Null(rating.Value);
        Assert.Equal(5, rating.Max);
        Assert.False(rating.AllowHalf);
        Assert.True(rating.AllowClear);
        Assert.False(rating.ReadOnly);
    }

    [StaFact]
    public void Max_CoercedToAtLeastOne()
    {
        var rating = new NaviusRating { Max = 0 };

        Assert.Equal(1, rating.Max);
    }

    [StaFact]
    public void HandleKey_ArrowUp_IncreasesByStep()
    {
        var rating = new NaviusRating { Value = 2m };

        var handled = rating.HandleKey(Key.Up);

        Assert.True(handled);
        Assert.Equal(3m, rating.Value);
    }

    [StaFact]
    public void HandleKey_ArrowUp_HalfStepWhenAllowHalf()
    {
        var rating = new NaviusRating { AllowHalf = true, Value = 2m };

        rating.HandleKey(Key.Up);

        Assert.Equal(2.5m, rating.Value);
    }

    [StaFact]
    public void HandleKey_ArrowDown_ClearsBelowStepWhenAllowClear()
    {
        var rating = new NaviusRating { Value = 1m };

        rating.HandleKey(Key.Down);

        Assert.Null(rating.Value);
    }

    [StaFact]
    public void HandleKey_ArrowRight_MirroredUnderRtl()
    {
        var rating = new NaviusRating { Value = 2m, FlowDirection = FlowDirection.RightToLeft };

        rating.HandleKey(Key.Right);

        // Under rtl, ArrowRight behaves like the ltr ArrowLeft (decrease).
        Assert.Equal(1m, rating.Value);
    }

    [StaFact]
    public void HandleKey_Home_JumpsToOne()
    {
        var rating = new NaviusRating { Value = 4m };

        rating.HandleKey(Key.Home);

        Assert.Equal(1m, rating.Value);
    }

    [StaFact]
    public void HandleKey_End_JumpsToMax()
    {
        var rating = new NaviusRating { Max = 5, Value = 1m };

        rating.HandleKey(Key.End);

        Assert.Equal(5m, rating.Value);
    }

    [StaFact]
    public void HandleKey_Backspace_ClearsWhenAllowClear()
    {
        var rating = new NaviusRating { Value = 3m };

        rating.HandleKey(Key.Back);

        Assert.Null(rating.Value);
    }

    [StaFact]
    public void HandleKey_Backspace_NoOpWhenClearDisallowed()
    {
        var rating = new NaviusRating { Value = 3m, AllowClear = false };

        rating.HandleKey(Key.Back);

        Assert.Equal(3m, rating.Value);
    }

    [StaFact]
    public void HandleKey_Digit_JumpsToClampedValue()
    {
        var rating = new NaviusRating { Max = 5 };

        rating.HandleKey(Key.D3);

        Assert.Equal(3m, rating.Value);
    }

    [StaFact]
    public void HandleKey_Space_SelectsFocusedStar_WhenUnrated()
    {
        // Roving focus lands on star 1 when unrated; Space activates it (native button-click parity).
        var rating = new NaviusRating { Max = 5 };

        var handled = rating.HandleKey(Key.Space);

        Assert.True(handled);
        Assert.Equal(1m, rating.Value);
    }

    [StaFact]
    public void HandleKey_Enter_ReselectingFocusedStar_ClearsWhenAllowClear()
    {
        // Focus is on the star holding the current value (3); Enter re-selects it, which clears.
        var rating = new NaviusRating { Value = 3m };

        var handled = rating.HandleKey(Key.Enter);

        Assert.True(handled);
        Assert.Null(rating.Value);
    }

    [StaFact]
    public void HandleKey_Space_NoOpWhenReadOnly()
    {
        var rating = new NaviusRating { ReadOnly = true, Value = 2m };

        var handled = rating.HandleKey(Key.Space);

        Assert.False(handled);
        Assert.Equal(2m, rating.Value);
    }

    [StaFact]
    public void HandleKey_NoOpWhenReadOnly()
    {
        var rating = new NaviusRating { ReadOnly = true, Value = 2m };

        var handled = rating.HandleKey(Key.Up);

        Assert.False(handled);
        Assert.Equal(2m, rating.Value);
    }

    [StaFact]
    public void HandleKey_NoOpWhenDisabled()
    {
        var rating = new NaviusRating { IsEnabled = false, Value = 2m };

        var handled = rating.HandleKey(Key.Up);

        Assert.False(handled);
    }

    [StaFact]
    public void HandleKey_UnhandledKey_ReturnsFalse()
    {
        var rating = new NaviusRating();

        Assert.False(rating.HandleKey(Key.Tab));
    }

    [StaFact]
    public void ValueChanged_FiresOnValueChange()
    {
        var rating = new NaviusRating();
        decimal? observed = null;
        rating.ValueChanged += (_, e) => observed = e.NewValue;

        rating.Value = 3m;

        Assert.Equal(3m, observed);
    }

    [StaFact]
    public void DefaultLabel_SingularForOne()
    {
        Assert.Equal("1 star", NaviusRating.DefaultLabel(1m));
        Assert.Equal("3.5 stars", NaviusRating.DefaultLabel(3.5m));
    }

    // --- NaviusRatingItem ---

    [StaFact]
    public void RatingItem_DefaultsToEmptyUnhighlighted()
    {
        var item = new NaviusRatingItem { Index = 2 };

        Assert.Equal(2, item.Index);
        Assert.Equal(NaviusRatingFillState.Empty, item.FillState);
        Assert.False(item.IsHighlighted);
    }

    // --- Automation peers ---

    [StaFact]
    public void RatingAutomationPeer_ReportsGroupControlType()
    {
        var rating = new NaviusRating();
        var peer = new NaviusRatingAutomationPeer(rating);

        Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
        Assert.False(peer.CanSelectMultiple);
    }

    [StaFact]
    public void RatingAutomationPeer_IsSelectionRequired_MatchesRequired()
    {
        var rating = new NaviusRating { Required = true };
        var peer = new NaviusRatingAutomationPeer(rating);

        Assert.True(peer.IsSelectionRequired);
    }

    [StaFact]
    public void RatingItemAutomationPeer_ReportsRadioButtonControlType()
    {
        var item = new NaviusRatingItem { Index = 1 };
        var peer = new NaviusRatingItemAutomationPeer(item);

        Assert.Equal(AutomationControlType.RadioButton, peer.GetAutomationControlType());
    }

    [StaFact]
    public void RatingItemAutomationPeer_IsSelected_FalseWithoutGroup()
    {
        // An item outside any NaviusRating's visual tree has no group to check against.
        var item = new NaviusRatingItem { Index = 1 };
        var peer = new NaviusRatingItemAutomationPeer(item);

        Assert.False(peer.IsSelected);
    }

    // ---- RTL: half-fill clip mirroring (docs/parity/rating.md correction) -------------------

    // docs/parity/rating.md previously flagged the half-fill Clip (Themes/Rating.xaml's
    // RectangleGeometry Rect="0,0,12,24" on the "Fill" Path) as "not RTL-mirrored". Pixel-rendered
    // RenderTargetBitmap verification during the M6 RTL wave showed this claim was false: WPF's
    // automatic FlowDirection mirroring (applied once, as a whole, wherever FlowDirection is
    // explicitly set -- here, directly on the templated NaviusRatingItem) already reflects the
    // Path's local Clip along with everything else the item renders, since no element in the
    // template opts out with its own local FlowDirection. This test locks in that (correct,
    // no-code-change-needed) behavior as a regression guard: the solid ink mass of a half-filled
    // star must sit on the side matching FlowDirection's "first half of reading order".
    [StaFact]
    public void HalfFill_ClipMirrorsUnderRtl_SolidInkOnOppositeSideFromLtr()
    {
        var ltrLeftHeavy = SolidInkIsLeftHeavy(FlowDirection.LeftToRight);
        var rtlLeftHeavy = SolidInkIsLeftHeavy(FlowDirection.RightToLeft);

        Assert.True(ltrLeftHeavy, "LTR half-fill should show its solid ink mass on the left.");
        Assert.False(rtlLeftHeavy, "RTL half-fill should show its solid ink mass on the right (mirrored).");
    }

    private static bool SolidInkIsLeftHeavy(FlowDirection dir)
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Rating.xaml"),
        });

        var item = new NaviusRatingItem { Width = 24, Height = 24, FlowDirection = dir, Resources = scope };
        item.SetResourceReference(FrameworkElement.StyleProperty, typeof(NaviusRatingItem));
        item.ApplyTemplate();
        item.FillState = NaviusRatingFillState.Half;
        item.Measure(new Size(24, 24));
        item.Arrange(new Rect(0, 0, 24, 24));
        item.UpdateLayout();

        var rtb = new RenderTargetBitmap(24, 24, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(item);
        var pixels = new byte[24 * 24 * 4];
        rtb.CopyPixels(pixels, 24 * 4, 0);

        // Row 12 (vertical middle) is wide enough on this star glyph to distinguish a left- from a
        // right-heavy solid fill; count opaque pixels in each half, ignoring the thin (~1px) always
        // -visible outline stroke by requiring a comfortable majority rather than any-opacity.
        const int y = 12;
        var leftOpaque = 0;
        var rightOpaque = 0;
        for (var x = 0; x < 24; x++)
        {
            var idx = (y * 24 + x) * 4;
            if (pixels[idx + 3] <= 10)
            {
                continue;
            }

            if (x < 12)
            {
                leftOpaque++;
            }
            else
            {
                rightOpaque++;
            }
        }

        return leftOpaque > rightOpaque;
    }
}
