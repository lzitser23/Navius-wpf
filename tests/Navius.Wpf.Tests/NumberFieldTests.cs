using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using Navius.Wpf.Primitives.Controls;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class NumberFieldTests
{
    static NumberFieldTests()
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

    // --- NaviusNumberFieldMath: pure step/clamp/format/parse math ---

    [StaFact]
    public void Math_CoerceStep_NonPositiveBecomesOne()
    {
        Assert.Equal(1, NaviusNumberFieldMath.CoerceStep(0));
        Assert.Equal(1, NaviusNumberFieldMath.CoerceStep(-5));
        Assert.Equal(2.5, NaviusNumberFieldMath.CoerceStep(2.5));
    }

    [StaFact]
    public void Math_Clamp_RespectsMinAndMax()
    {
        Assert.Equal(0, NaviusNumberFieldMath.Clamp(-5, 0, 10));
        Assert.Equal(10, NaviusNumberFieldMath.Clamp(15, 0, 10));
        Assert.Equal(5, NaviusNumberFieldMath.Clamp(5, 0, 10));
    }

    [StaFact]
    public void Math_Clamp_UnboundedWhenMinMaxNull()
    {
        Assert.Equal(-100, NaviusNumberFieldMath.Clamp(-100, null, null));
    }

    [StaFact]
    public void Math_Step_TreatsNullCurrentAsZero()
    {
        Assert.Equal(1, NaviusNumberFieldMath.Step(null, 1, null, null));
    }

    [StaFact]
    public void Math_Step_ClampsResult()
    {
        Assert.Equal(10, NaviusNumberFieldMath.Step(9, 5, 0, 10));
    }

    [StaFact]
    public void Math_CanIncrement_FalseAtMax()
    {
        Assert.False(NaviusNumberFieldMath.CanIncrement(10, 10, disabled: false, readOnly: false));
        Assert.True(NaviusNumberFieldMath.CanIncrement(9, 10, disabled: false, readOnly: false));
    }

    [StaFact]
    public void Math_CanIncrement_FalseWhenDisabledOrReadOnly()
    {
        Assert.False(NaviusNumberFieldMath.CanIncrement(0, null, disabled: true, readOnly: false));
        Assert.False(NaviusNumberFieldMath.CanIncrement(0, null, disabled: false, readOnly: true));
    }

    [StaFact]
    public void Math_CanDecrement_FalseAtMin()
    {
        Assert.False(NaviusNumberFieldMath.CanDecrement(0, 0, disabled: false, readOnly: false));
        Assert.True(NaviusNumberFieldMath.CanDecrement(1, 0, disabled: false, readOnly: false));
    }

    [StaFact]
    public void Math_Format_EmptyWhenNull()
    {
        Assert.Equal(string.Empty, NaviusNumberFieldMath.Format(null, null));
    }

    [StaFact]
    public void Math_Format_UsesInvariantCultureAndFormatString()
    {
        Assert.Equal("3.5", NaviusNumberFieldMath.Format(3.5, "0.##"));
    }

    [StaFact]
    public void Math_TryParse_ValidInvariantNumber()
    {
        Assert.True(NaviusNumberFieldMath.TryParse("42.5", out var value));
        Assert.Equal(42.5, value);
    }

    [StaFact]
    public void Math_TryParse_RejectsGarbage()
    {
        Assert.False(NaviusNumberFieldMath.TryParse("abc", out _));
    }

    // --- NaviusNumberField: control-level defaults and behavior ---

    [StaFact]
    public void DefaultState_IsEmptyWithContractDefaults()
    {
        var field = new NaviusNumberField();

        Assert.Null(field.Value);
        Assert.Equal(1, field.Step);
        Assert.Equal(10, field.LargeStep);
        Assert.Equal(0.1, field.SmallStep);
        Assert.False(field.ReadOnly);
        Assert.False(field.Required);
    }

    [StaFact]
    public void Step_NonPositive_CoercesToOne()
    {
        var field = new NaviusNumberField { Step = -3 };

        Assert.Equal(1, field.Step);
    }

    [StaFact]
    public void Value_ClampedToMinMaxOnSet()
    {
        var field = new NaviusNumberField { Minimum = 0, Maximum = 10, Value = 999 };

        Assert.Equal(10, field.Value);
    }

    [StaFact]
    public void Value_ReclampedWhenBoundsShrink()
    {
        var field = new NaviusNumberField { Value = 50 };

        field.Maximum = 10;

        Assert.Equal(10, field.Value);
    }

    [StaFact]
    public void StepBy_IncrementsFromNullAsZero()
    {
        var field = new NaviusNumberField { Step = 2 };

        field.StepBy(field.Step);

        Assert.Equal(2, field.Value);
    }

    [StaFact]
    public void StepBy_NoOpWhenReadOnly()
    {
        var field = new NaviusNumberField { ReadOnly = true, Value = 5 };

        field.StepBy(1);

        Assert.Equal(5, field.Value);
    }

    [StaFact]
    public void StepBy_NoOpWhenDisabled()
    {
        var field = new NaviusNumberField { IsEnabled = false, Value = 5 };

        field.StepBy(1);

        Assert.Equal(5, field.Value);
    }

    [StaFact]
    public void SetToBound_JumpsToMinimum()
    {
        var field = new NaviusNumberField { Minimum = -5, Maximum = 5, Value = 2 };

        field.SetToBound(field.Minimum);

        Assert.Equal(-5, field.Value);
    }

    [StaFact]
    public void SetToBound_NoOpWhenBoundUnset()
    {
        var field = new NaviusNumberField { Value = 2 };

        field.SetToBound(field.Minimum);

        Assert.Equal(2, field.Value);
    }

    [StaFact]
    public void CommitText_ValidText_ParsesAndClamps()
    {
        var field = new NaviusNumberField { Minimum = 0, Maximum = 10 };

        field.CommitText("999");

        Assert.Equal(10, field.Value);
    }

    [StaFact]
    public void CommitText_InvalidText_RevertsToCurrentValue()
    {
        var field = new NaviusNumberField { Value = 7 };

        field.CommitText("not-a-number");

        Assert.Equal(7, field.Value);
    }

    [StaFact]
    public void CommitText_EmptyText_ClearsToNull()
    {
        var field = new NaviusNumberField { Value = 7 };

        field.CommitText("   ");

        Assert.Null(field.Value);
    }

    [StaFact]
    public void CommitText_NoOpWhenReadOnly()
    {
        var field = new NaviusNumberField { ReadOnly = true, Value = 7 };

        field.CommitText("42");

        Assert.Equal(7, field.Value);
    }

    [StaFact]
    public void Display_FormatsWithInvariantCulture()
    {
        var field = new NaviusNumberField { Value = 3.5, Format = "0.##" };

        Assert.Equal("3.5", field.Display);
    }

    [StaFact]
    public void CanIncrement_FalseAtMaximum()
    {
        var field = new NaviusNumberField { Minimum = 0, Maximum = 10, Value = 10 };

        Assert.False(field.CanIncrement);
        Assert.True(field.CanDecrement);
    }

    [StaFact]
    public void CanDecrement_FalseAtMinimum()
    {
        var field = new NaviusNumberField { Minimum = 0, Maximum = 10, Value = 0 };

        Assert.False(field.CanDecrement);
        Assert.True(field.CanIncrement);
    }

    [StaFact]
    public void ValueChanged_FiresOnValueChange()
    {
        var field = new NaviusNumberField();
        double? observed = null;
        field.ValueChanged += (_, e) => observed = e.NewValue;

        field.Value = 5;

        Assert.Equal(5, observed);
    }

    // --- Automation peer: role=spinbutton + IRangeValueProvider ---

    [StaFact]
    public void AutomationPeer_ReportsSpinnerControlType()
    {
        var field = new NaviusNumberField();
        var peer = new NaviusNumberFieldAutomationPeer(field);

        Assert.Equal(AutomationControlType.Spinner, peer.GetAutomationControlType());
    }

    [StaFact]
    public void AutomationPeer_RangeValues_MirrorField()
    {
        var field = new NaviusNumberField { Minimum = 0, Maximum = 10, Step = 2, LargeStep = 5, Value = 4 };
        var peer = new NaviusNumberFieldAutomationPeer(field);

        Assert.Equal(4, peer.Value);
        Assert.Equal(0, peer.Minimum);
        Assert.Equal(10, peer.Maximum);
        Assert.Equal(2, peer.SmallChange);
        Assert.Equal(5, peer.LargeChange);
    }

    [StaFact]
    public void AutomationPeer_IsReadOnly_TrueWhenReadOnlyOrDisabled()
    {
        var field = new NaviusNumberField { ReadOnly = true };
        var peer = new NaviusNumberFieldAutomationPeer(field);

        Assert.True(peer.IsReadOnly);
    }

    [StaFact]
    public void AutomationPeer_SetValue_UpdatesField()
    {
        var field = new NaviusNumberField { Minimum = 0, Maximum = 10 };
        var peer = new NaviusNumberFieldAutomationPeer(field);

        peer.SetValue(7);

        Assert.Equal(7, field.Value);
    }

    [StaFact]
    public void AutomationPeer_SetValue_ThrowsWhenReadOnly()
    {
        var field = new NaviusNumberField { ReadOnly = true };
        var peer = new NaviusNumberFieldAutomationPeer(field);

        Assert.Throws<System.Windows.Automation.ElementNotEnabledException>(() => peer.SetValue(7));
    }

    // M6 regression: the peer implements IRangeValueProvider, but a UIA client only reaches it
    // through GetPattern(PatternInterface.RangeValue). Before the fix GetPattern was not overridden,
    // so FrameworkElementAutomationPeer returned null and the RangeValue pattern (aria-valuenow/
    // min/max) was invisible to assistive tech despite the peer implementing the interface.
    [StaFact]
    public void AutomationPeer_ExposesRangeValuePattern_ViaGetPattern()
    {
        var field = new NaviusNumberField { Minimum = 0, Maximum = 10, Value = 4 };
        var peer = new NaviusNumberFieldAutomationPeer(field);

        var provider = peer.GetPattern(PatternInterface.RangeValue);

        Assert.NotNull(provider);
        Assert.Same(peer, provider);
        Assert.Equal(4, ((IRangeValueProvider)provider!).Value);
    }

    // --- M6 regression: keyboard stepping through the REAL hosted-TextBox key routing ---
    // These raise genuine tunneling key events on the inner input. With the original
    // `_input.KeyDown += ...` wiring the TextBox's own class handlers marked Home/End/PageUp/PageDown/
    // Arrow as Handled during the bubbling phase, so the stepping handler never ran and Value never
    // moved. Handling PreviewKeyDown (tunnel) fixes it; these tests fail against the old wiring.

    private static readonly System.Windows.Interop.HwndSource KeyTestSource =
        new(0, 0, 0, 0, 0, "NaviusNumberFieldKeyTests", IntPtr.Zero);

    private static readonly ConstructorInfo KeyEventArgsCtor = typeof(KeyEventArgs).GetConstructor(
        new[] { typeof(KeyboardDevice), typeof(PresentationSource), typeof(int), typeof(Key) })!;

    private static NaviusNumberField CreateAppliedField(double? min, double? max, double? value)
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/NumberField.xaml"),
        });

        var field = new NaviusNumberField { Minimum = min, Maximum = max, Value = value, Resources = scope };
        _ = new Window { Content = field };
        Assert.True(field.ApplyTemplate());
        return field;
    }

    private static TextBox InnerInput(NaviusNumberField field) =>
        (TextBox)typeof(NaviusNumberField)
            .GetField("_input", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(field)!;

    private static void PressKey(TextBox input, Key key)
    {
        var args = (KeyEventArgs)KeyEventArgsCtor.Invoke(
            new object?[] { Keyboard.PrimaryDevice, KeyTestSource, 0, key });
        args.RoutedEvent = Keyboard.PreviewKeyDownEvent;
        input.RaiseEvent(args);
    }

    [StaFact]
    public void Keyboard_Home_JumpsToMinimum_ThroughRealRouting()
    {
        var field = CreateAppliedField(0, 100, 50);

        PressKey(InnerInput(field), Key.Home);

        Assert.Equal(0, field.Value);
    }

    [StaFact]
    public void Keyboard_End_JumpsToMaximum_ThroughRealRouting()
    {
        var field = CreateAppliedField(0, 100, 50);

        PressKey(InnerInput(field), Key.End);

        Assert.Equal(100, field.Value);
    }

    [StaFact]
    public void Keyboard_ArrowUp_StepsByStep_ThroughRealRouting()
    {
        var field = CreateAppliedField(0, 100, 50);

        PressKey(InnerInput(field), Key.Up);

        Assert.Equal(51, field.Value);
    }

    [StaFact]
    public void Keyboard_PageDown_StepsByLargeStep_ThroughRealRouting()
    {
        var field = CreateAppliedField(0, 100, 50);

        PressKey(InnerInput(field), Key.PageDown);

        Assert.Equal(40, field.Value);
    }
}
