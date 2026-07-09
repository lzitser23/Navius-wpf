using System.Windows;
using Navius.Wpf.Primitives.Controls.MaskedInput;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

/// <summary>
/// Pure MaskEngine tests: plain [Fact]s, no WPF statics, runnable on the thread pool. The bulk is
/// caret-stability coverage: the engine's contract is that typing, deleting, and pasting NEVER
/// land the caret anywhere but "after the same count of editable characters".
/// </summary>
public class MaskEngineTests
{
    private const string PhoneMask = "(000) 000-0000";

    private static IReadOnlyList<MaskToken> Phone => MaskEngine.Parse(PhoneMask);

    // --- Parse ---

    [Fact]
    public void Parse_MapsTokenKinds()
    {
        var tokens = MaskEngine.Parse("0A*-");

        Assert.Equal(MaskTokenKind.Digit, tokens[0].Kind);
        Assert.Equal(MaskTokenKind.Letter, tokens[1].Kind);
        Assert.Equal(MaskTokenKind.Alnum, tokens[2].Kind);
        Assert.Equal(MaskTokenKind.Fixed, tokens[3].Kind);
        Assert.Equal('-', tokens[3].Literal);
    }

    [Fact]
    public void Parse_EmptyMask_YieldsNoTokens()
    {
        Assert.Empty(MaskEngine.Parse(string.Empty));
    }

    [Fact]
    public void Token_Accepts_MatchesItsClass()
    {
        Assert.True(new MaskToken(MaskTokenKind.Digit, '0').Accepts('7'));
        Assert.False(new MaskToken(MaskTokenKind.Digit, '0').Accepts('a'));
        Assert.True(new MaskToken(MaskTokenKind.Letter, 'A').Accepts('x'));
        Assert.False(new MaskToken(MaskTokenKind.Letter, 'A').Accepts('7'));
        Assert.True(new MaskToken(MaskTokenKind.Alnum, '*').Accepts('7'));
        Assert.True(new MaskToken(MaskTokenKind.Alnum, '*').Accepts('x'));
        Assert.False(new MaskToken(MaskTokenKind.Alnum, '*').Accepts('-'));
        Assert.False(new MaskToken(MaskTokenKind.Fixed, '-').Accepts('-'));
    }

    // --- Format: masking behavior ---

    [Fact]
    public void Format_FullDigits_ProducesMaskedPhone()
    {
        var (value, _, _, unmasked) = MaskEngine.Format(Phone, "1234567890", 10, 10, lazy: true, placeholder: null);

        Assert.Equal("(123) 456-7890", value);
        Assert.Equal("1234567890", unmasked);
    }

    [Fact]
    public void Format_RejectsCharactersTheTokenRefuses()
    {
        // The e2e spec's exact scenario: letters typed into digit slots are dropped.
        var (value, _, _, unmasked) = MaskEngine.Format(Phone, "1234567890abc", 13, 13, lazy: true, placeholder: null);

        Assert.Equal("(123) 456-7890", value);
        Assert.Equal("1234567890", unmasked);
    }

    [Fact]
    public void Format_OverflowDigits_AreDropped()
    {
        var (value, _, _, _) = MaskEngine.Format(Phone, "123456789012345", 15, 15, lazy: true, placeholder: null);

        Assert.Equal("(123) 456-7890", value);
    }

    [Fact]
    public void Format_EmptyRaw_YieldsEmptyLazyValue()
    {
        var (value, caretStart, caretEnd, unmasked) = MaskEngine.Format(Phone, "", 0, 0, lazy: true, placeholder: null);

        Assert.Equal("", value);
        Assert.Equal(0, caretStart);
        Assert.Equal(0, caretEnd);
        Assert.Equal("", unmasked);
    }

    [Fact]
    public void Format_Lazy_StopsAtTrailingFixedLiteral()
    {
        var tokens = MaskEngine.Parse("00/00");

        var (value, _, _, _) = MaskEngine.Format(tokens, "12", 2, 2, lazy: true, placeholder: null);

        Assert.Equal("12", value); // the '/' is not emitted until a third char arrives.
    }

    [Fact]
    public void Format_Eager_EmitsLeadingFixedLiterals()
    {
        var tokens = MaskEngine.Parse("+1 000");

        var (value, _, _, _) = MaskEngine.Format(tokens, "", 0, 0, lazy: false, placeholder: null);

        Assert.Equal("+1 ", value); // eager: fixed prefix appears before any typing.
    }

    [Fact]
    public void Format_ReachingAFixedLiteral_AutoInsertsIt()
    {
        var tokens = MaskEngine.Parse("00/00");

        var (value, caretStart, _, _) = MaskEngine.Format(tokens, "123", 3, 3, lazy: true, placeholder: null);

        Assert.Equal("12/3", value);
        Assert.Equal(4, caretStart); // caret rides past the auto-inserted '/'.
    }

    [Fact]
    public void Format_UserTypedLiteral_IsConsumedNotDoubled()
    {
        var tokens = MaskEngine.Parse("00/00");

        var (value, _, _, _) = MaskEngine.Format(tokens, "12/34", 5, 5, lazy: true, placeholder: null);

        Assert.Equal("12/34", value);
    }

    [Fact]
    public void Format_Placeholder_FillsEmptySlots()
    {
        var tokens = MaskEngine.Parse("00/00");

        var (value, caretStart, _, unmasked) = MaskEngine.Format(tokens, "1", 1, 1, lazy: true, placeholder: '_');

        Assert.Equal("1_/__", value);
        Assert.Equal(1, caretStart);
        Assert.Equal("1", unmasked);
    }

    [Fact]
    public void Format_LetterAndAlnumMasks_FilterPerClass()
    {
        var tokens = MaskEngine.Parse("AA-00");

        var (value, _, _, _) = MaskEngine.Format(tokens, "ab12", 4, 4, lazy: true, placeholder: null);

        Assert.Equal("ab-12", value);

        var (rejected, _, _, _) = MaskEngine.Format(tokens, "1a2b", 4, 4, lazy: true, placeholder: null);

        // The walk is strictly ordered: each letter slot skips rejected digits to find its letter,
        // so the skipped '2' is consumed (dropped) before the digit slots are reached.
        Assert.Equal("ab", rejected);
    }

    // --- Caret stability: the family's core guarantee ---

    [Fact]
    public void Caret_MidStringInsertion_LandsAfterTheInsertedDigit()
    {
        // The e2e spec's scenario: "(123) 456-7890", caret placed at index 2 (between 1 and 2),
        // digit 9 typed -> proposed raw "(1923) 456-7890" with caret 3. The caret must land at
        // index 3 (right after the inserted 9), not jump to the end.
        var (value, caretStart, caretEnd, _) = MaskEngine.Format(
            Phone, "(1923) 456-7890", 3, 3, lazy: true, placeholder: null);

        Assert.Equal("(192) 345-6789", value); // overflow digit dropped at the tail.
        Assert.Equal(3, caretStart);
        Assert.Equal(caretStart, caretEnd);
    }

    [Fact]
    public void Caret_TypingFirstChar_LandsAfterIt()
    {
        var (value, caretStart, _, _) = MaskEngine.Format(Phone, "1", 1, 1, lazy: true, placeholder: null);

        Assert.Equal("(1", value);
        Assert.Equal(2, caretStart); // after the auto-inserted '(' and the digit.
    }

    [Fact]
    public void Caret_BackspaceOverFixedLiteral_StaysPutWhileTheLiteralReappears()
    {
        // Backspace at index 5 of "(123) 456-7890" deletes ')' -> raw "(123 456-7890", caret 4.
        // The mask re-inserts ')'; the caret must stay right after the 3 (index 4), not shift.
        var (value, caretStart, _, _) = MaskEngine.Format(
            Phone, "(123 456-7890", 4, 4, lazy: true, placeholder: null);

        Assert.Equal("(123) 456-7890", value);
        Assert.Equal(4, caretStart);
    }

    [Fact]
    public void Caret_DeletingAMidStringDigit_AnchorsByDigitCount()
    {
        // Deleting '4' from "(123) 456-7890" -> raw "(123) 56-7890", caret 6 (3 digits before it).
        // Everything reflows; the caret must land after the 3rd editable char of the new value.
        var (value, caretStart, _, _) = MaskEngine.Format(
            Phone, "(123) 56-7890", 6, 6, lazy: true, placeholder: null);

        Assert.Equal("(123) 567-890", value);
        Assert.Equal(4, caretStart); // right after the 3rd digit ('3').
    }

    [Fact]
    public void Caret_PasteReplacingEverything_LandsAtEndOfMasked()
    {
        var (value, caretStart, _, _) = MaskEngine.Format(
            Phone, "5551234567", 10, 10, lazy: true, placeholder: null);

        Assert.Equal("(555) 123-4567", value);
        Assert.Equal(value.Length, caretStart);
    }

    [Fact]
    public void Caret_PasteWithGarbage_CountsOnlyAcceptedCharsBeforeIt()
    {
        // Pasting "12ab3" with caret at the end: only 1, 2, 3 are consumed.
        var tokens = MaskEngine.Parse("00/00");

        var (value, caretStart, _, _) = MaskEngine.Format(tokens, "12ab3", 5, 5, lazy: true, placeholder: null);

        Assert.Equal("12/3", value);
        Assert.Equal(4, caretStart);
    }

    [Fact]
    public void Caret_AtPositionZero_StaysAtZero()
    {
        var (_, caretStart, _, _) = MaskEngine.Format(Phone, "(123) 456-7890", 0, 0, lazy: true, placeholder: null);

        Assert.Equal(0, caretStart);
    }

    [Fact]
    public void Caret_NonCollapsedSelection_MapsBothEndsIndependently()
    {
        // Selection over "23) 4" of "(123) 456-7890" (start 2, end 7 = 1 digit before start,
        // 4 digits before end). Both ends must re-land by their own digit counts.
        var (value, caretStart, caretEnd, _) = MaskEngine.Format(
            Phone, "(123) 456-7890", 2, 7, lazy: true, placeholder: null);

        Assert.Equal("(123) 456-7890", value);
        Assert.Equal(2, caretStart); // after digit 1.
        Assert.Equal(7, caretEnd);   // after digit 4.
    }

    [Fact]
    public void Caret_WithPlaceholder_NeverLandsInsidePlaceholderSlots()
    {
        var tokens = MaskEngine.Parse("(000) 000-0000");

        var (value, caretStart, _, _) = MaskEngine.Format(tokens, "12", 2, 2, lazy: true, placeholder: '_');

        Assert.Equal("(12_) ___-____", value);
        Assert.Equal(3, caretStart); // after the 2, before the first placeholder.
    }

    [Fact]
    public void Caret_InsertionBeforeExistingDigits_ShiftsFollowingDigitsRight()
    {
        // "12/34" with caret at 0, typing 9 -> raw "912/34" caret 1: digits reflow to 91/23 (4 dropped).
        var tokens = MaskEngine.Parse("00/00");

        var (value, caretStart, _, _) = MaskEngine.Format(tokens, "912/34", 1, 1, lazy: true, placeholder: null);

        Assert.Equal("91/23", value);
        Assert.Equal(1, caretStart);
    }

    [Fact]
    public void Caret_ConsumedCountBeyondEditableChars_ClampsToEnd()
    {
        // Caret claims more editable chars than survive the walk: clamp to the value's end.
        var tokens = MaskEngine.Parse("00");

        var (value, caretStart, _, _) = MaskEngine.Format(tokens, "12345", 5, 5, lazy: true, placeholder: null);

        Assert.Equal("12", value);
        Assert.Equal(2, caretStart);
    }
}

/// <summary>Control-level wiring: the TextBox-derived pipeline, unmasked surfacing, and Value sync.</summary>
public class MaskedInputTests
{
    static MaskedInputTests()
    {
        // pack://application URIs only resolve once an Application exists in the process. Guarded
        // try/catch because xunit runs test classes in parallel on separate STA threads: another
        // class's static ctor can win the race to create the process-wide Application.
        if (Application.Current is null)
        {
            try
            {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            catch (InvalidOperationException)
            {
                // Another test class already created the process-wide Application.
            }
        }
    }

    private static NaviusMaskedInput CreatePhone() => new() { Mask = "(000) 000-0000" };

    [StaFact]
    public void Defaults_MatchTheContract()
    {
        var input = new NaviusMaskedInput();

        Assert.Equal(string.Empty, input.Mask);
        Assert.Null(input.Value);
        Assert.Null(input.PlaceholderChar);
        Assert.True(input.Lazy);
        Assert.False(input.Overwrite);
        Assert.False(input.Invalid);
        Assert.Equal(string.Empty, input.UnmaskedValue);
    }

    [StaFact]
    public void Typing_RunsTheMaskPipeline()
    {
        var input = CreatePhone();

        input.Text = "1234567890abc";

        Assert.Equal("(123) 456-7890", input.Text);
        Assert.Equal("(123) 456-7890", input.Value);
        Assert.Equal("1234567890", input.UnmaskedValue);
    }

    [StaFact]
    public void MidStringInsertion_KeepsTheCaretStable()
    {
        // The e2e spec driven through the real TextBox editing path: SelectedText insertion
        // triggers OnTextChanged with the post-edit caret, exactly like a keystroke.
        var input = CreatePhone();
        input.Text = "1234567890";
        Assert.Equal("(123) 456-7890", input.Text);

        input.CaretIndex = 2; // between 1 and 2.
        input.SelectedText = "9"; // types a digit mid-string.

        Assert.Equal("(192) 345-6789", input.Text);
        Assert.Equal(3, input.CaretIndex); // right after the inserted 9; no jump to the end.
    }

    [StaFact]
    public void BackspaceOverALiteral_ReinsertsIt_AndHoldsTheCaret()
    {
        var input = CreatePhone();
        input.Text = "1234567890";

        input.Select(4, 1);       // the ')' literal.
        input.SelectedText = ""; // Backspace/Delete over it.

        Assert.Equal("(123) 456-7890", input.Text);
        Assert.Equal(4, input.CaretIndex);
    }

    [StaFact]
    public void UnmaskedValueChanged_FiresWithEditableCharsOnly()
    {
        var input = CreatePhone();
        string? observed = null;
        input.UnmaskedValueChanged += (_, unmasked) => observed = unmasked;

        input.Text = "12";

        Assert.Equal("12", observed);
    }

    [StaFact]
    public void SettingValue_RemasksThroughThePipeline()
    {
        var input = CreatePhone();

        input.Value = "1234567890";

        Assert.Equal("(123) 456-7890", input.Text);
        Assert.Equal("(123) 456-7890", input.Value);
    }

    [StaFact]
    public void ChangingMask_ReformatsTheCurrentText()
    {
        var input = new NaviusMaskedInput { Mask = "0000" };
        input.Text = "1234";

        input.Mask = "00-00";

        Assert.Equal("12-34", input.Text);
    }

    [StaFact]
    public void PlaceholderChar_RendersTheFullSkeleton()
    {
        var input = new NaviusMaskedInput { Mask = "00/00", PlaceholderChar = '_' };

        input.Text = "1";

        Assert.Equal("1_/__", input.Text);
        Assert.Equal("1", input.UnmaskedValue);
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/MaskedInput.xaml"),
        });

        var input = CreatePhone();
        _ = new Window { Resources = scope, Content = input };

        Assert.True(input.ApplyTemplate());
    }
}
