using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using Navius.Wpf.Primitives.Controls;

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
}
