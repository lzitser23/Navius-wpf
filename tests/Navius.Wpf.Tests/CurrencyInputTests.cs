using System.Globalization;
using System.Windows;
using Navius.Wpf.Primitives.Controls.CurrencyInput;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

/// <summary>
/// Pure CurrencyEngine tests: plain [Fact]s, no WPF statics. Uses hand-built NumberFormatInfo
/// instances (not real cultures) so the assertions are immune to ICU/NLS cultural-data drift.
/// </summary>
public class CurrencyEngineTests
{
    /// <summary>A deterministic en-US-shaped format: $ prefix, comma groups, dot decimals.</summary>
    private static NumberFormatInfo UsLike() => new()
    {
        CurrencySymbol = "$",
        CurrencyGroupSeparator = ",",
        CurrencyDecimalSeparator = ".",
        CurrencyDecimalDigits = 2,
        CurrencyPositivePattern = 0, // $n
        NegativeSign = "-",
    };

    // --- Parse ---

    [Fact]
    public void Parse_SplitsIntegerAndFraction()
    {
        var (negative, intDigits, fracDigits, hadSep) = CurrencyEngine.Parse("1,234.56", UsLike(), false, 2);

        Assert.False(negative);
        Assert.Equal("1234", intDigits);
        Assert.Equal("56", fracDigits);
        Assert.True(hadSep);
    }

    [Fact]
    public void Parse_StripsLeadingZeros_ButKeepsALoneZero()
    {
        Assert.Equal("7", CurrencyEngine.Parse("007", UsLike(), false, 2).IntDigits);
        Assert.Equal("0", CurrencyEngine.Parse("000", UsLike(), false, 2).IntDigits);
    }

    [Fact]
    public void Parse_NegativeSign_OnlyWhenAllowed()
    {
        Assert.True(CurrencyEngine.Parse("-5", UsLike(), allowNegative: true, 2).Negative);
        Assert.False(CurrencyEngine.Parse("-5", UsLike(), allowNegative: false, 2).Negative);
    }

    [Fact]
    public void Parse_MaxFracZero_IgnoresTheSeparator()
    {
        var (_, intDigits, fracDigits, hadSep) = CurrencyEngine.Parse("12.34", UsLike(), false, 0);

        // With no fraction allowed the '.' is not a separator; all digits fold into the integer.
        Assert.Equal("1234", intDigits);
        Assert.Equal("", fracDigits);
        Assert.False(hadSep);
    }

    [Fact]
    public void Parse_TruncatesFractionBeyondMaxFrac()
    {
        Assert.Equal("99", CurrencyEngine.Parse("1.999", UsLike(), false, 2).FracDigits);
    }

    // --- ToDecimal ---

    [Fact]
    public void ToDecimal_NullWhenNoDigits()
    {
        Assert.Null(CurrencyEngine.ToDecimal(false, "", ""));
    }

    [Fact]
    public void ToDecimal_BuildsSignedValue()
    {
        Assert.Equal(1234.56m, CurrencyEngine.ToDecimal(false, "1234", "56"));
        Assert.Equal(-42m, CurrencyEngine.ToDecimal(true, "42", ""));
        Assert.Equal(0.5m, CurrencyEngine.ToDecimal(false, "", "5"));
    }

    // --- FormatEditing ---

    [Fact]
    public void FormatEditing_GroupsTheInteger()
    {
        Assert.Equal("$1,234,567", CurrencyEngine.FormatEditing(false, "1234567", "", false, UsLike(), true));
    }

    [Fact]
    public void FormatEditing_KeepsALoneTrailingSeparator()
    {
        Assert.Equal("$1,234.", CurrencyEngine.FormatEditing(false, "1234", "", true, UsLike(), true));
    }

    [Fact]
    public void FormatEditing_KeepsFractionAsTyped()
    {
        Assert.Equal("$5.5", CurrencyEngine.FormatEditing(false, "5", "5", true, UsLike(), true));
    }

    [Fact]
    public void FormatEditing_EmptyPartsYieldEmpty()
    {
        Assert.Equal("", CurrencyEngine.FormatEditing(false, "", "", false, UsLike(), true));
    }

    [Fact]
    public void FormatEditing_NegativeIsSignPlusPositive()
    {
        // The documented deviation: NegativeSign + positive, never the parenthesised patterns.
        Assert.Equal("-$5", CurrencyEngine.FormatEditing(true, "5", "", false, UsLike(), true));
    }

    [Fact]
    public void FormatEditing_ShowSymbolFalse_OmitsTheSymbol()
    {
        Assert.Equal("1,234", CurrencyEngine.FormatEditing(false, "1234", "", false, UsLike(), false));
    }

    [Fact]
    public void FormatEditing_HonorsPositivePatternPlacements()
    {
        var suffix = UsLike();
        suffix.CurrencyPositivePattern = 3; // n $

        Assert.Equal("12 $", CurrencyEngine.FormatEditing(false, "12", "", false, suffix, true));
    }

    // --- FormatCommitted ---

    [Fact]
    public void FormatCommitted_PadsFractionToMinFrac()
    {
        Assert.Equal("$1,234.50", CurrencyEngine.FormatCommitted(1234.5m, UsLike(), 2, 2, true));
        Assert.Equal("$5.00", CurrencyEngine.FormatCommitted(5m, UsLike(), 2, 2, true));
    }

    [Fact]
    public void FormatCommitted_TrimsZerosDownToMinFrac()
    {
        Assert.Equal("$1.234", CurrencyEngine.FormatCommitted(1.234m, UsLike(), 2, 4, true));
        Assert.Equal("$1.20", CurrencyEngine.FormatCommitted(1.2m, UsLike(), 2, 4, true));
    }

    [Fact]
    public void FormatCommitted_MinFracZero_DropsAWholeFraction()
    {
        Assert.Equal("$5", CurrencyEngine.FormatCommitted(5m, UsLike(), 0, 2, true));
    }

    [Fact]
    public void FormatCommitted_Negative()
    {
        Assert.Equal("-$5.00", CurrencyEngine.FormatCommitted(-5m, UsLike(), 2, 2, true));
    }

    // --- Caret stability primitives ---

    [Fact]
    public void CountDigitsBefore_CountsOnlyDigits()
    {
        Assert.Equal(2, CurrencyEngine.CountDigitsBefore("$1,234.56", 4));
        Assert.Equal(0, CurrencyEngine.CountDigitsBefore("$1,234", 1));
        Assert.Equal(4, CurrencyEngine.CountDigitsBefore("$1,234", 999)); // caret past the end clamps.
    }

    [Fact]
    public void CaretForDigits_LandsAfterTheNthDigit()
    {
        Assert.Equal(3, CurrencyEngine.CaretForDigits("$12,345", 2)); // after the '2'.
        Assert.Equal(5, CurrencyEngine.CaretForDigits("$12,345", 3)); // after the '3', past the comma.
    }

    [Fact]
    public void CaretForDigits_ZeroTarget_LandsBeforeTheFirstDigit()
    {
        Assert.Equal(1, CurrencyEngine.CaretForDigits("$1,234", 0));
        Assert.Equal(6, CurrencyEngine.CaretForDigits("no dig", 0)); // no digit: end of string.
    }

    [Fact]
    public void CaretForDigits_TargetBeyondDigits_ClampsToEnd()
    {
        Assert.Equal(6, CurrencyEngine.CaretForDigits("$1,234", 99));
    }

    [Fact]
    public void CaretStability_RegroupingRoundTrip()
    {
        // The core guarantee: caret position expressed as a digit count survives a regroup.
        // "$1,234" caret after '2' (index 4, 2 digits before) + typing '9' -> "$12,934"; the caret
        // must land after the '9' (3 digits), not drift over the moved comma.
        var nfi = UsLike();
        var beforeDigits = CurrencyEngine.CountDigitsBefore("$1,29", 5); // proposed text up to caret.
        var (neg, i, f, sep) = CurrencyEngine.Parse("$1,2934", nfi, false, 2);
        var formatted = CurrencyEngine.FormatEditing(neg, i, f, sep, nfi, true);

        Assert.Equal("$12,934", formatted);
        Assert.Equal(5, CurrencyEngine.CaretForDigits(formatted, beforeDigits)); // after the '9'.
    }

    // --- SymbolFor ---

    [Fact]
    public void SymbolFor_MapsCommonCodes_AndFallsBackToTheCode()
    {
        Assert.Equal("$", CurrencyEngine.SymbolFor("usd"));
        Assert.Equal("€", CurrencyEngine.SymbolFor("EUR"));
        Assert.Equal("₪", CurrencyEngine.SymbolFor("ILS"));
        Assert.Equal("CHF", CurrencyEngine.SymbolFor("CHF"));
        Assert.Equal("XYZ", CurrencyEngine.SymbolFor("XYZ"));
    }
}

/// <summary>Control-level wiring: the TextBox-derived live pipeline, blur commit, and clamping.</summary>
public class CurrencyInputTests
{
    static CurrencyInputTests()
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

    /// <summary>Invariant culture + USD symbol: fully deterministic $-prefixed formatting.</summary>
    private static NaviusCurrencyInput CreateUsd() => new()
    {
        Culture = CultureInfo.InvariantCulture,
        Currency = "USD",
    };

    [StaFact]
    public void Defaults_MatchTheContract()
    {
        var input = new NaviusCurrencyInput();

        Assert.Null(input.Value);
        Assert.True(input.ShowSymbol);
        Assert.False(input.AllowNegative);
        Assert.False(input.Invalid);
        Assert.False(input.IsNegative);
        Assert.Null(input.MinFractionDigits);
        Assert.Null(input.MaxFractionDigits);
    }

    [StaFact]
    public void Typing_ParsesAndGroupsLive()
    {
        var input = CreateUsd();

        input.Text = "1234567";

        Assert.Equal("$1,234,567", input.Text);
        Assert.Equal(1234567m, input.Value);
    }

    [StaFact]
    public void Typing_NonDigits_AreFiltered()
    {
        var input = CreateUsd();

        input.Text = "12ab34";

        Assert.Equal("$1,234", input.Text);
        Assert.Equal(1234m, input.Value);
    }

    [StaFact]
    public void MidStringInsertion_KeepsTheCaretStableAcrossRegrouping()
    {
        var input = CreateUsd();
        input.Text = "1234";
        Assert.Equal("$1,234", input.Text);

        input.CaretIndex = 4;     // after the '2'.
        input.SelectedText = "9"; // proposed "$1,2934": regroups to "$12,934".

        Assert.Equal("$12,934", input.Text);
        Assert.Equal(5, input.CaretIndex); // right after the inserted 9; the moved comma is skipped.
        Assert.Equal(12934m, input.Value);
    }

    [StaFact]
    public void ClearingAllText_YieldsNullValue()
    {
        var input = CreateUsd();
        input.Text = "12";

        input.Text = "";

        Assert.Equal("", input.Text);
        Assert.Null(input.Value);
    }

    [StaFact]
    public void CommitValue_PadsFractionToTheCultureDefault()
    {
        var input = CreateUsd();
        input.Text = "5";

        input.CommitValue(); // the LostFocus path, invoked directly (never Show() in unit tests).

        Assert.Equal("$5.00", input.Text);
        Assert.Equal(5m, input.Value);
    }

    [StaFact]
    public void CommitValue_ClampsToMinimumAndMaximum()
    {
        var input = CreateUsd();
        input.Minimum = 10m;
        input.Maximum = 100m;

        input.Text = "150";
        input.CommitValue();

        Assert.Equal(100m, input.Value);
        Assert.Equal("$100.00", input.Text);

        input.Text = "3";
        input.CommitValue();

        Assert.Equal(10m, input.Value);
    }

    [StaFact]
    public void Negative_RequiresAllowNegative()
    {
        var strict = CreateUsd();
        strict.Text = "-42";
        Assert.Equal(42m, strict.Value);
        Assert.False(strict.IsNegative);

        var loose = CreateUsd();
        loose.AllowNegative = true;
        loose.Text = "-42";
        Assert.Equal(-42m, loose.Value);
        Assert.True(loose.IsNegative);
        Assert.Equal("-$42", loose.Text);
    }

    [StaFact]
    public void ShowSymbolFalse_OmitsTheSymbol()
    {
        var input = CreateUsd();
        input.ShowSymbol = false;

        input.Text = "1234";

        Assert.Equal("1,234", input.Text);
    }

    [StaFact]
    public void CurrencyCode_OverridesTheCultureSymbol()
    {
        var input = new NaviusCurrencyInput { Culture = CultureInfo.InvariantCulture, Currency = "EUR" };

        input.Text = "9";

        Assert.Equal("€9", input.Text);
    }

    [StaFact]
    public void MaxFractionDigits_CapsTypedFraction()
    {
        var input = CreateUsd();
        input.MinFractionDigits = 0;
        input.MaxFractionDigits = 1;

        input.Text = "1.99";

        Assert.Equal("$1.9", input.Text);
        Assert.Equal(1.9m, input.Value);
    }

    [StaFact]
    public void SettingValueExternally_RendersCommittedFormat()
    {
        var input = CreateUsd();

        input.Value = 1234.5m;

        Assert.Equal("$1,234.50", input.Text);
    }

    [StaFact]
    public void ValueChanged_FiresOnTypedChange()
    {
        var input = CreateUsd();
        decimal? observed = null;
        input.ValueChanged += (_, e) => observed = e.NewValue;

        input.Text = "7";

        Assert.Equal(7m, observed);
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/CurrencyInput.xaml"),
        });

        var input = CreateUsd();
        _ = new Window { Resources = scope, Content = input };

        Assert.True(input.ApplyTemplate());
    }
}
