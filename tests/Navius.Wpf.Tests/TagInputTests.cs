using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Navius.Wpf.Primitives.Controls.TagInput;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

/// <summary>Pure TagInputEngine tests: plain [Fact]s, no WPF statics.</summary>
public class TagInputEngineTests
{
    private static readonly IReadOnlyList<TagDelimiter> DefaultDelimiters =
        new[] { TagDelimiter.Enter, TagDelimiter.Comma };

    // --- TryCommit: the contract pipeline (trim -> transform -> empty/duplicate/max/validate) ---

    [Fact]
    public void TryCommit_TrimsAndAppends()
    {
        var (status, text, tags) = TagInputEngine.TryCommit(
            new[] { "a" }, "  b  ", null, null, allowDuplicates: false, maxTags: null);

        Assert.Equal(TagCommitStatus.Committed, status);
        Assert.Equal("b", text);
        Assert.Equal(new[] { "a", "b" }, tags);
    }

    [Fact]
    public void TryCommit_Transform_RunsAfterTrim()
    {
        var (status, text, _) = TagInputEngine.TryCommit(
            Array.Empty<string>(), " ReD ", s => s.ToLowerInvariant(), null, false, null);

        Assert.Equal(TagCommitStatus.Committed, status);
        Assert.Equal("red", text);
    }

    [Fact]
    public void TryCommit_EmptyAfterTrim_IsASilentNoOp()
    {
        var (status, _, tags) = TagInputEngine.TryCommit(
            new[] { "a" }, "   ", null, null, false, null);

        Assert.Equal(TagCommitStatus.Empty, status);
        Assert.Equal(new[] { "a" }, tags);
    }

    [Fact]
    public void TryCommit_DuplicateBlocked_UnlessAllowed()
    {
        var (status, _, _) = TagInputEngine.TryCommit(new[] { "a" }, "a", null, null, false, null);
        Assert.Equal(TagCommitStatus.Duplicate, status);

        var (allowed, _, tags) = TagInputEngine.TryCommit(new[] { "a" }, "a", null, null, true, null);
        Assert.Equal(TagCommitStatus.Committed, allowed);
        Assert.Equal(new[] { "a", "a" }, tags);
    }

    [Fact]
    public void TryCommit_DuplicateChecked_AgainstTheTransformedText()
    {
        var (status, _, _) = TagInputEngine.TryCommit(
            new[] { "red" }, "RED", s => s.ToLowerInvariant(), null, false, null);

        Assert.Equal(TagCommitStatus.Duplicate, status);
    }

    [Fact]
    public void TryCommit_MaxTags_Blocks()
    {
        var (status, _, tags) = TagInputEngine.TryCommit(new[] { "a", "b" }, "c", null, null, false, maxTags: 2);

        Assert.Equal(TagCommitStatus.TooMany, status);
        Assert.Equal(new[] { "a", "b" }, tags);
    }

    [Fact]
    public void TryCommit_DuplicateWins_OverMaxTags()
    {
        // The web checks duplicate before max: a full list still reports Duplicate for a repeat.
        var (status, _, _) = TagInputEngine.TryCommit(new[] { "a" }, "a", null, null, false, maxTags: 1);

        Assert.Equal(TagCommitStatus.Duplicate, status);
    }

    [Fact]
    public void TryCommit_ValidateBlocks()
    {
        var (status, _, _) = TagInputEngine.TryCommit(
            Array.Empty<string>(), "nope", null, s => s.Length > 4, false, null);

        Assert.Equal(TagCommitStatus.Rejected, status);
    }

    // --- Delimiter splitting ---

    [Fact]
    public void FindCharDelimiter_CommaBeforeSpace()
    {
        var both = new[] { TagDelimiter.Comma, TagDelimiter.Space };

        Assert.Equal(',', TagInputEngine.FindCharDelimiter("a b,c", both));
        Assert.Equal(' ', TagInputEngine.FindCharDelimiter("a b", both));
        Assert.Null(TagInputEngine.FindCharDelimiter("abc", both));
    }

    [Fact]
    public void FindCharDelimiter_OnlyActiveDelimitersApply()
    {
        Assert.Null(TagInputEngine.FindCharDelimiter("a b", DefaultDelimiters)); // Space not active.
        Assert.Equal(',', TagInputEngine.FindCharDelimiter("a,b", DefaultDelimiters));
    }

    [Fact]
    public void Split_CommitsCompletedSegments_KeepsTheRemainder()
    {
        var (candidates, remainder) = TagInputEngine.Split("a,b,c", DefaultDelimiters);

        Assert.Equal(new[] { "a", "b" }, candidates);
        Assert.Equal("c", remainder);
    }

    [Fact]
    public void Split_TrailingDelimiter_LeavesAnEmptyRemainder()
    {
        var (candidates, remainder) = TagInputEngine.Split("a,", DefaultDelimiters);

        Assert.Equal(new[] { "a" }, candidates);
        Assert.Equal("", remainder);
    }

    [Fact]
    public void Split_NoDelimiter_EverythingIsRemainder()
    {
        var (candidates, remainder) = TagInputEngine.Split("abc", DefaultDelimiters);

        Assert.Empty(candidates);
        Assert.Equal("abc", remainder);
    }

    // --- Remove + highlight ---

    [Fact]
    public void RemoveAt_RemovesByIndex_OutOfRangeIsANoOp()
    {
        Assert.Equal(new[] { "a", "c" }, TagInputEngine.RemoveAt(new[] { "a", "b", "c" }, 1));
        Assert.Equal(new[] { "a" }, TagInputEngine.RemoveAt(new[] { "a" }, 5));
        Assert.Equal(new[] { "a" }, TagInputEngine.RemoveAt(new[] { "a" }, -1));
    }

    [Fact]
    public void HighlightAfterRemove_MovesToAdjacent_OrBackToField()
    {
        Assert.Equal(1, TagInputEngine.HighlightAfterRemove(1, 3));  // next adjacent chip.
        Assert.Equal(1, TagInputEngine.HighlightAfterRemove(2, 2));  // removed the last: clamp.
        Assert.Equal(-1, TagInputEngine.HighlightAfterRemove(0, 0)); // list emptied: field.
    }
}

/// <summary>Control-level wiring: commit paths, chip navigation state, events, and the UIA peer.</summary>
public class TagInputTests : IDisposable
{
    static TagInputTests()
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

    private static ResourceDictionary CreateThemedScope()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/TagInput.xaml"),
        });

        return scope;
    }

    /// <summary>Builds a templated control in a themed (never-shown) Window and returns its field part.</summary>
    private static (NaviusTagInput Control, TextBox Input) CreateApplied()
    {
        var control = new NaviusTagInput();
        _ = new Window { Resources = CreateThemedScope(), Content = control };
        Assert.True(control.ApplyTemplate());
        var input = (TextBox)control.Template.FindName("PART_Input", control);
        return (control, input);
    }

    [StaFact]
    public void Defaults_MatchTheContract()
    {
        var control = new NaviusTagInput();

        Assert.Null(control.Value);
        Assert.Equal(-1, control.HighlightedIndex);
        Assert.False(control.AllowDuplicates);
        Assert.Null(control.MaxTags);
        Assert.False(control.AddOnBlur);
        Assert.True(control.IsTagListEmpty);
        Assert.Equal(new[] { TagDelimiter.Enter, TagDelimiter.Comma }, control.EffectiveDelimiters);
    }

    [StaFact]
    public void CommitText_AddsATag_AndFiresEvents()
    {
        var control = new NaviusTagInput();
        string? added = null;
        IReadOnlyList<string>? changed = null;
        control.TagAdded += (_, t) => added = t;
        control.ValueChanged += (_, v) => changed = v;

        var status = control.CommitText(" alpha ");

        Assert.Equal(TagCommitStatus.Committed, status);
        Assert.Equal(new[] { "alpha" }, control.Value);
        Assert.Equal("alpha", added);
        Assert.Equal(new[] { "alpha" }, changed);
        Assert.False(control.IsTagListEmpty);
        Assert.Single(control.Chips!);
        Assert.Equal("alpha", control.Chips![0].Text);
        Assert.Equal("Remove alpha", control.Chips![0].RemoveName);
    }

    [StaFact]
    public void CommitText_Duplicate_FiresTagRejected()
    {
        var control = new NaviusTagInput();
        control.CommitText("a");
        string? rejected = null;
        control.TagRejected += (_, t) => rejected = t;

        var status = control.CommitText("a");

        Assert.Equal(TagCommitStatus.Duplicate, status);
        Assert.Equal("a", rejected);
        Assert.Equal(new[] { "a" }, control.Value);
    }

    [StaFact]
    public void CommitText_MaxTags_FiresTagRejected()
    {
        var control = new NaviusTagInput { MaxTags = 1 };
        control.CommitText("a");

        Assert.Equal(TagCommitStatus.TooMany, control.CommitText("b"));
        Assert.Equal(new[] { "a" }, control.Value);
    }

    [StaFact]
    public void CommitText_TransformAndValidate_AreApplied()
    {
        var control = new NaviusTagInput
        {
            Transform = s => s.ToLowerInvariant(),
            Validate = s => s.StartsWith("a", StringComparison.Ordinal),
        };

        Assert.Equal(TagCommitStatus.Committed, control.CommitText("ALPHA"));
        Assert.Equal(TagCommitStatus.Rejected, control.CommitText("beta"));
        Assert.Equal(new[] { "alpha" }, control.Value);
    }

    [StaFact]
    public void CommitText_NoOpWhenDisabled()
    {
        var control = new NaviusTagInput { IsEnabled = false };

        control.CommitText("a");

        Assert.Null(control.Value);
    }

    [StaFact]
    public void RemoveTagAt_FiresTagRemoved_AndMovesHighlightToAdjacent()
    {
        var control = new NaviusTagInput();
        control.CommitText("a");
        control.CommitText("b");
        control.CommitText("c");
        control.Highlight(1, focus: false);
        string? removed = null;
        control.TagRemoved += (_, t) => removed = t;

        control.RemoveTagAt(1);

        Assert.Equal("b", removed);
        Assert.Equal(new[] { "a", "c" }, control.Value);
        Assert.Equal(1, control.HighlightedIndex); // the next adjacent chip ("c").
        Assert.True(control.Chips![1].IsHighlighted);
    }

    [StaFact]
    public void RemoveTagAt_LastChip_ReturnsHighlightToField()
    {
        var control = new NaviusTagInput();
        control.CommitText("only");
        control.Highlight(0, focus: false);

        control.RemoveTagAt(0);

        Assert.Equal(-1, control.HighlightedIndex);
        Assert.True(control.IsTagListEmpty);
    }

    [StaFact]
    public void RemoveHighlighted_TheEmptyBackspaceSecondPress()
    {
        var control = new NaviusTagInput();
        control.CommitText("a");
        control.CommitText("b");
        control.Highlight(1, focus: false);

        control.RemoveHighlighted();

        Assert.Equal(new[] { "a" }, control.Value);
        Assert.Equal(-1, control.HighlightedIndex); // focus returns to the field.
    }

    [StaFact]
    public void Highlight_TogglesChipFlags()
    {
        var control = new NaviusTagInput();
        control.CommitText("a");
        control.CommitText("b");

        control.Highlight(0, focus: false);
        Assert.True(control.Chips![0].IsHighlighted);
        Assert.False(control.Chips![1].IsHighlighted);

        control.Highlight(-1, focus: false);
        Assert.False(control.Chips![0].IsHighlighted);
    }

    [StaFact]
    public void TypingACharDelimiter_CommitsSegments_AndKeepsTheRemainder()
    {
        var (control, input) = CreateApplied();

        input.Text = "red,green,bl";

        Assert.Equal(new[] { "red", "green" }, control.Value);
        Assert.Equal("bl", input.Text);
    }

    [StaFact]
    public void EnterKey_CommitsTheFieldText()
    {
        var (control, input) = CreateApplied();
        input.Text = "alpha";

        RaiseKey(input, Key.Enter);

        Assert.Equal(new[] { "alpha" }, control.Value);
        Assert.Equal("", input.Text);
    }

    [StaFact]
    public void BackspaceOnEmptyField_HighlightsThenRemoves()
    {
        var (control, input) = CreateApplied();
        control.CommitText("a");
        control.CommitText("b");

        RaiseKey(input, Key.Back); // first press: highlight the last chip.
        Assert.Equal(1, control.HighlightedIndex);
        Assert.Equal(new[] { "a", "b" }, control.Value);

        RaiseKey(input, Key.Back); // second press: remove it.
        Assert.Equal(new[] { "a" }, control.Value);
        Assert.Equal(-1, control.HighlightedIndex);
    }

    [StaFact]
    public void TabDelimiter_OnlyWhenConfigured()
    {
        var (control, input) = CreateApplied();
        control.Delimiters = new[] { TagDelimiter.Enter, TagDelimiter.Tab };
        input.Text = "alpha";

        RaiseKey(input, Key.Tab);

        Assert.Equal(new[] { "alpha" }, control.Value);
    }

    [StaFact]
    public void FieldArrowLeft_OnEmptyField_EntersChipNavigation()
    {
        var (control, input) = CreateApplied();
        control.CommitText("a");
        control.CommitText("b");

        RaiseKey(input, Key.Left); // empty field + tags present -> highlight the last chip.

        Assert.Equal(1, control.HighlightedIndex);
    }

    [StaFact]
    public void ChipArrowKeys_MoveTheHighlight()
    {
        var (control, _) = CreateApplied();
        control.CommitText("a");
        control.CommitText("b");
        control.CommitText("c");
        var chips = (ItemsControl)control.Template.FindName("PART_Chips", control);
        control.Highlight(2, focus: false); // start on the last chip.

        RaiseKey(chips, Key.Left);
        Assert.Equal(1, control.HighlightedIndex);

        RaiseKey(chips, Key.Home);
        Assert.Equal(0, control.HighlightedIndex);

        RaiseKey(chips, Key.End);
        Assert.Equal(2, control.HighlightedIndex);

        RaiseKey(chips, Key.Right); // past the last chip -> back to the field (-1).
        Assert.Equal(-1, control.HighlightedIndex);
    }

    [StaFact]
    public void ChipDeleteKey_RemovesTheHighlightedChip()
    {
        var (control, _) = CreateApplied();
        control.CommitText("a");
        control.CommitText("b");
        var chips = (ItemsControl)control.Template.FindName("PART_Chips", control);
        control.Highlight(0, focus: false);

        RaiseKey(chips, Key.Delete);

        Assert.Equal(new[] { "b" }, control.Value);
    }

    [StaFact]
    public void UiaPeer_SurfacesTagsOverReadOnlyValuePattern()
    {
        var control = new NaviusTagInput();
        control.CommitText("a");
        control.CommitText("b");

        var peer = System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(control);
        var provider = (System.Windows.Automation.Provider.IValueProvider)peer.GetPattern(
            System.Windows.Automation.Peers.PatternInterface.Value)!;

        Assert.True(provider.IsReadOnly);
        Assert.Equal("a, b", provider.Value);
        Assert.Throws<InvalidOperationException>(() => provider.SetValue("x"));
    }

    // Lazily created (not a static field initializer) and disposed per test instance -- this
    // dummy 0x0 native window must not outlive the STA thread it was created on.
    private HwndSource? _testSource;

    private PresentationSource TestSource =>
        _testSource ??= new HwndSource(0, 0, 0, 0, 0, "NaviusTagInputTests", IntPtr.Zero);

    public void Dispose() => _testSource?.Dispose();

    private void RaiseKey(UIElement target, Key key) =>
        target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, TestSource, 0, key)
        {
            RoutedEvent = Keyboard.PreviewKeyDownEvent,
        });
}
