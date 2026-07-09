using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using Navius.Wpf.Primitives.Controls.OneTimePasswordField;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class OneTimePasswordFieldTests
{
    static OneTimePasswordFieldTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        if (Application.Current is null)
        {
            _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        }
    }

    // ---- Pure buffer logic (no WPF/STA dependency; the source of truth the control drives from) ----

    [Fact]
    public void SetChar_WritesSlotAndAdvancesFocus()
    {
        var buffer = new char?[6];
        var (next, focus) = OneTimePasswordBuffer.SetChar(buffer, 0, '1');

        Assert.Equal('1', next[0]);
        Assert.Equal(1, focus);
    }

    [Fact]
    public void SetChar_AtLastSlot_ClampsFocus()
    {
        var buffer = new char?[6];
        var (_, focus) = OneTimePasswordBuffer.SetChar(buffer, 5, '9');

        Assert.Equal(5, focus);
    }

    [Fact]
    public void Backspace_NonEmptyCell_ClearsShiftsRemainderBack_AndRetreatsFocus()
    {
        var buffer = OneTimePasswordBuffer.FromValue("123", 6);

        var (next, focus) = OneTimePasswordBuffer.Backspace(buffer, 1);

        Assert.Equal('1', next[0]);
        Assert.Equal('3', next[1]);
        Assert.Null(next[2]);
        Assert.Equal(0, focus);
    }

    [Fact]
    public void Backspace_EmptyCell_ClearsPreviousInstead_AndRetreatsFocus()
    {
        var buffer = OneTimePasswordBuffer.FromValue("12", 6);

        var (next, focus) = OneTimePasswordBuffer.Backspace(buffer, 2);

        Assert.Equal('1', next[0]);
        Assert.Null(next[1]);
        Assert.Equal(1, focus);
    }

    [Fact]
    public void Backspace_AtFirstEmptyCell_IsNoOp()
    {
        var buffer = new char?[6];

        var (next, focus) = OneTimePasswordBuffer.Backspace(buffer, 0);

        Assert.All(next, c => Assert.Null(c));
        Assert.Equal(0, focus);
    }

    [Fact]
    public void Delete_ClearsAndShiftsRemainderBack_FocusStaysPut()
    {
        var buffer = OneTimePasswordBuffer.FromValue("123", 6);

        var (next, focus) = OneTimePasswordBuffer.Delete(buffer, 0);

        Assert.Equal('2', next[0]);
        Assert.Equal('3', next[1]);
        Assert.Null(next[2]);
        Assert.Equal(0, focus);
    }

    [Fact]
    public void ClearAll_ClearsEveryCell_AndFocusesFirst()
    {
        var (next, focus) = OneTimePasswordBuffer.ClearAll(6);

        Assert.All(next, c => Assert.Null(c));
        Assert.Equal(0, focus);
    }

    [Fact]
    public void Paste_ReplacesFromSlotZero_RegardlessOfPriorContent()
    {
        var buffer = OneTimePasswordBuffer.FromValue("999999", 6);

        var (next, focus) = OneTimePasswordBuffer.Paste("1234", 6);

        Assert.Equal("1234  ", OneTimePasswordBuffer.ToValue(next));
        Assert.Equal(3, focus);
    }

    [Fact]
    public void Paste_TruncatesToLength_AndFocusesLastSlot()
    {
        var (next, focus) = OneTimePasswordBuffer.Paste("12345678", 6);

        Assert.Equal("123456", OneTimePasswordBuffer.ToValue(next));
        Assert.Equal(5, focus);
    }

    [Fact]
    public void FromValueToValue_RoundTripsInteriorGapsAsSpaces()
    {
        var buffer = OneTimePasswordBuffer.FromValue("1 3", 4);

        Assert.Equal('1', buffer[0]);
        Assert.Null(buffer[1]);
        Assert.Equal('3', buffer[2]);
        Assert.Equal("1 3 ", OneTimePasswordBuffer.ToValue(buffer));
    }

    [Fact]
    public void IsComplete_TrueOnlyWhenEveryCellFilled()
    {
        Assert.False(OneTimePasswordBuffer.IsComplete(OneTimePasswordBuffer.FromValue("12", 3)));
        Assert.True(OneTimePasswordBuffer.IsComplete(OneTimePasswordBuffer.FromValue("123", 3)));
    }

    [Theory]
    [InlineData('5', "numeric", true)]
    [InlineData('a', "numeric", false)]
    [InlineData('a', "alpha", true)]
    [InlineData('5', "alpha", false)]
    [InlineData('a', "alphanumeric", true)]
    [InlineData('5', "alphanumeric", true)]
    public void IsAllowedChar_ClassifiesPerValidationType(char ch, string validationType, bool expected) =>
        Assert.Equal(expected, OneTimePasswordBuffer.IsAllowedChar(ch, validationType));

    // ---- Control integration ----

    private static readonly MethodInfo PreviewKeyDownMethod = typeof(NaviusOneTimePasswordField)
        .GetMethod("OnCellPreviewKeyDown", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo PreviewTextInputMethod = typeof(NaviusOneTimePasswordField)
        .GetMethod("OnCellPreviewTextInput", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly ConstructorInfo KeyEventArgsCtor = typeof(KeyEventArgs).GetConstructor(
        new[] { typeof(KeyboardDevice), typeof(PresentationSource), typeof(int), typeof(Key) })!;

    private static readonly System.Windows.Interop.HwndSource TestSource =
        new(0, 0, 0, 0, 0, "NaviusOtpTests", IntPtr.Zero);

    private static ResourceDictionary CreateThemedScope()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/OneTimePasswordField.xaml"),
        });

        return scope;
    }

    private static NaviusOneTimePasswordField CreateApplied(int length = 4)
    {
        var otp = new NaviusOneTimePasswordField { Length = length, Resources = CreateThemedScope() };
        Assert.True(otp.ApplyTemplate());
        return otp;
    }

    private static void SimulateType(NaviusOneTimePasswordField otp, NaviusOneTimePasswordFieldInput cell, string text)
    {
        var composition = new TextComposition(System.Windows.Input.InputManager.Current, cell, text);
        var args = new TextCompositionEventArgs(Keyboard.PrimaryDevice, composition)
        {
            RoutedEvent = TextCompositionManager.PreviewTextInputEvent,
        };
        PreviewTextInputMethod.Invoke(otp, new object[] { cell, args });
    }

    private static void SimulateKey(NaviusOneTimePasswordField otp, NaviusOneTimePasswordFieldInput cell, Key key)
    {
        var args = (KeyEventArgs)KeyEventArgsCtor.Invoke(new object?[] { Keyboard.PrimaryDevice, TestSource, 0, key });
        args.RoutedEvent = Keyboard.PreviewKeyDownEvent;
        PreviewKeyDownMethod.Invoke(otp, new object[] { cell, args });
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_BuildsLengthCells()
    {
        var otp = CreateApplied(6);

        var cells = otp.GetType()
            .GetField("_cells", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(otp) as System.Collections.IList;

        Assert.Equal(6, cells!.Count);
    }

    [StaFact]
    public void Typing_AdvancesFocusToNextCell_AndUpdatesValue()
    {
        var otp = CreateApplied(4);
        var cells = GetCells(otp);

        SimulateType(otp, cells[0], "1");

        Assert.Equal('1', cells[0].Char);
        Assert.True(cells[1].IsFocused);
        Assert.Equal("1   ", otp.Value);
    }

    [StaFact]
    public void Typing_RejectsCharacterFailingValidationType_LeavesBufferUntouched()
    {
        var otp = CreateApplied(4);
        otp.ValidationType = "numeric";
        var cells = GetCells(otp);

        SimulateType(otp, cells[0], "a");

        Assert.Null(cells[0].Char);
    }

    [StaFact]
    public void Backspace_OnFilledCell_ClearsAndRetreatsFocus()
    {
        var otp = CreateApplied(4);
        var cells = GetCells(otp);
        SimulateType(otp, cells[0], "1");
        SimulateType(otp, cells[1], "2");

        SimulateKey(otp, cells[2], Key.Back);

        Assert.Null(cells[1].Char);
        Assert.True(cells[1].IsFocused);
    }

    [StaFact]
    public void Complete_FiresOnceEveryCellIsFilled()
    {
        var otp = CreateApplied(2);
        var cells = GetCells(otp);
        var completed = 0;
        otp.Complete += (_, _) => completed++;

        SimulateType(otp, cells[0], "1");
        Assert.Equal(0, completed);

        SimulateType(otp, cells[1], "2");
        Assert.Equal(1, completed);
    }

    [StaFact]
    public void AutoSubmit_FiresAfterComplete_WhenEnabled()
    {
        var otp = CreateApplied(1);
        otp.AutoSubmit = true;
        var cells = GetCells(otp);
        var autoSubmitted = 0;
        otp.AutoSubmitted += (_, _) => autoSubmitted++;

        SimulateType(otp, cells[0], "9");

        Assert.Equal(1, autoSubmitted);
    }

    [StaFact]
    public void ArrowDown_MovesFocusToNextCell_InDefaultVerticalOrientation()
    {
        var otp = CreateApplied(3);
        var cells = GetCells(otp);

        SimulateKey(otp, cells[0], Key.Down);

        Assert.True(cells[1].IsFocused);
    }

    [StaFact]
    public void ReadOnly_BlocksTyping()
    {
        var otp = CreateApplied(3);
        otp.ReadOnly = true;
        var cells = GetCells(otp);

        SimulateType(otp, cells[0], "1");

        Assert.Null(cells[0].Char);
    }

    private static List<NaviusOneTimePasswordFieldInput> GetCells(NaviusOneTimePasswordField otp) =>
        (List<NaviusOneTimePasswordFieldInput>)otp.GetType()
            .GetField("_cells", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(otp)!;
}
