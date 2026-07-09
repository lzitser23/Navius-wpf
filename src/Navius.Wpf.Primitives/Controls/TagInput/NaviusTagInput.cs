using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;

namespace Navius.Wpf.Primitives.Controls.TagInput;

/// <summary>
/// Tier B (custom lookless control): type-to-create-chips input. The web's 5 parts
/// (Root/List/Field/Tag/TagRemove) fold into one Control with two named template parts:
/// PART_Input (the TextBox field) and PART_Chips (an ItemsControl of <see cref="TagChipVm"/>),
/// the same folding NaviusNumberField applied to its Root/Group wrapper parts. List is layout-only
/// in the contract, Tag/TagRemove carry no state beyond the value they show, so chips are VMs +
/// a DataTemplate (the Combobox chip idiom), not CLR control types.
///
/// The commit pipeline (trim -> transform -> empty/duplicate/max/validate), delimiter splitting
/// and highlight math live in the pure <see cref="TagInputEngine"/>; this class owns only the WPF
/// wiring: the field's key handling, chip roving focus, and the remove command. Unlike Combobox's
/// virtual highlight, the chip highlight here is a REAL focus target (the contract's roving
/// tabindex): chip containers are focusable, the highlighted one is the single tab stop.
///
/// Accessibility: the web source wires no ARIA roles, so this is a fresh minimal design per the
/// APG: the field is a native TextBox (Edit peer for free), each remove button carries
/// "Remove {value}" as its name, and the root's peer exposes the committed tags over a read-only
/// ValuePattern (the Select-peer precedent: value that lives in template text must be readable
/// over UIA).
/// </summary>
[TemplatePart(Name = PartInput, Type = typeof(TextBox))]
[TemplatePart(Name = PartChips, Type = typeof(ItemsControl))]
public class NaviusTagInput : Control
{
    private const string PartInput = "PART_Input";
    private const string PartChips = "PART_Chips";

    /// <summary>Removes one chip; the chip's <see cref="TagChipVm"/> rides as the command parameter.</summary>
    public static readonly RoutedCommand RemoveTagCommand = new(nameof(RemoveTagCommand), typeof(NaviusTagInput));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(IReadOnlyList<string>), typeof(NaviusTagInput),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty DefaultValueProperty = DependencyProperty.Register(
        nameof(DefaultValue), typeof(IReadOnlyList<string>), typeof(NaviusTagInput), new PropertyMetadata(null));

    public static readonly DependencyProperty DelimitersProperty = DependencyProperty.Register(
        nameof(Delimiters), typeof(IReadOnlyList<TagDelimiter>), typeof(NaviusTagInput), new PropertyMetadata(null));

    public static readonly DependencyProperty AllowDuplicatesProperty = DependencyProperty.Register(
        nameof(AllowDuplicates), typeof(bool), typeof(NaviusTagInput), new PropertyMetadata(false));

    public static readonly DependencyProperty MaxTagsProperty = DependencyProperty.Register(
        nameof(MaxTags), typeof(int?), typeof(NaviusTagInput), new PropertyMetadata(null));

    public static readonly DependencyProperty ValidateProperty = DependencyProperty.Register(
        nameof(Validate), typeof(Func<string, bool>), typeof(NaviusTagInput), new PropertyMetadata(null));

    public static readonly DependencyProperty TransformProperty = DependencyProperty.Register(
        nameof(Transform), typeof(Func<string, string>), typeof(NaviusTagInput), new PropertyMetadata(null));

    public static readonly DependencyProperty AddOnBlurProperty = DependencyProperty.Register(
        nameof(AddOnBlur), typeof(bool), typeof(NaviusTagInput), new PropertyMetadata(false));

    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
        nameof(Placeholder), typeof(string), typeof(NaviusTagInput), new PropertyMetadata(null));

    public static readonly DependencyProperty HighlightedIndexProperty = DependencyProperty.Register(
        nameof(HighlightedIndex), typeof(int), typeof(NaviusTagInput),
        new PropertyMetadata(-1, OnHighlightedIndexChanged));

    private static readonly DependencyPropertyKey ChipsPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Chips), typeof(IReadOnlyList<TagChipVm>), typeof(NaviusTagInput), new PropertyMetadata(null));

    public static readonly DependencyProperty ChipsProperty = ChipsPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsTagListEmptyPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsTagListEmpty), typeof(bool), typeof(NaviusTagInput), new PropertyMetadata(true));

    public static readonly DependencyProperty IsTagListEmptyProperty = IsTagListEmptyPropertyKey.DependencyProperty;

    private TextBox? _input;
    private ItemsControl? _chipsList;
    private bool _syncingValue;
    private bool _suppressTextChanged;
    private bool _defaultValueApplied;

    static NaviusTagInput()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusTagInput), new FrameworkPropertyMetadata(typeof(NaviusTagInput)));
    }

    public NaviusTagInput()
    {
        CommandBindings.Add(new CommandBinding(RemoveTagCommand, OnRemoveTagExecuted));
        Loaded += OnLoaded;
    }

    /// <summary>Raised after a chip is successfully committed (the web's OnAdd).</summary>
    public event EventHandler<string>? TagAdded;

    /// <summary>Raised after a chip is removed by any path (the web's OnRemove).</summary>
    public event EventHandler<string>? TagRemoved;

    /// <summary>Raised when a candidate is blocked by duplicate, MaxTags, or Validate (the web's OnInvalid).</summary>
    public event EventHandler<string>? TagRejected;

    /// <summary>Raised on every committed mutation with the new list (the web's ValueChanged).</summary>
    public event EventHandler<IReadOnlyList<string>>? ValueChanged;

    /// <summary>The committed tags, in order (the contract's controlled Value).</summary>
    public IReadOnlyList<string>? Value
    {
        get => (IReadOnlyList<string>?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Uncontrolled initial tags, applied once on Loaded when no <see cref="Value"/> is set.</summary>
    public IReadOnlyList<string>? DefaultValue
    {
        get => (IReadOnlyList<string>?)GetValue(DefaultValueProperty);
        set => SetValue(DefaultValueProperty, value);
    }

    /// <summary>Which keys/chars commit a chip. Null defaults to Enter + Comma (the contract default).</summary>
    public IReadOnlyList<TagDelimiter>? Delimiters
    {
        get => (IReadOnlyList<TagDelimiter>?)GetValue(DelimitersProperty);
        set => SetValue(DelimitersProperty, value);
    }

    public bool AllowDuplicates
    {
        get => (bool)GetValue(AllowDuplicatesProperty);
        set => SetValue(AllowDuplicatesProperty, value);
    }

    public int? MaxTags
    {
        get => (int?)GetValue(MaxTagsProperty);
        set => SetValue(MaxTagsProperty, value);
    }

    /// <summary>Reject a candidate tag (return false blocks it and raises <see cref="TagRejected"/>).</summary>
    public Func<string, bool>? Validate
    {
        get => (Func<string, bool>?)GetValue(ValidateProperty);
        set => SetValue(ValidateProperty, value);
    }

    /// <summary>Normalize a candidate before commit (e.g. lowercase). Applied after a trim.</summary>
    public Func<string, string>? Transform
    {
        get => (Func<string, string>?)GetValue(TransformProperty);
        set => SetValue(TransformProperty, value);
    }

    /// <summary>Commit the field text on blur.</summary>
    public bool AddOnBlur
    {
        get => (bool)GetValue(AddOnBlurProperty);
        set => SetValue(AddOnBlurProperty, value);
    }

    public string? Placeholder
    {
        get => (string?)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>The chip-navigation highlight, or -1 when the field holds focus (the web's HighlightedIndex).</summary>
    public int HighlightedIndex
    {
        get => (int)GetValue(HighlightedIndexProperty);
        set => SetValue(HighlightedIndexProperty, value);
    }

    /// <summary>The chip VMs the template's PART_Chips renders (one per committed tag).</summary>
    public IReadOnlyList<TagChipVm>? Chips => (IReadOnlyList<TagChipVm>?)GetValue(ChipsProperty);

    /// <summary>True when there are no tags (the web's data-empty).</summary>
    public bool IsTagListEmpty => (bool)GetValue(IsTagListEmptyProperty);

    /// <summary>The active delimiters (the contract's default when unset).</summary>
    public IReadOnlyList<TagDelimiter> EffectiveDelimiters =>
        Delimiters ?? new[] { TagDelimiter.Enter, TagDelimiter.Comma };

    private IReadOnlyList<string> Tags => Value ?? Array.Empty<string>();

    public override void OnApplyTemplate()
    {
        if (_input is not null)
        {
            _input.PreviewKeyDown -= OnInputPreviewKeyDown;
            _input.TextChanged -= OnInputTextChanged;
            _input.LostFocus -= OnInputLostFocus;
        }

        if (_chipsList is not null)
        {
            _chipsList.PreviewKeyDown -= OnChipsPreviewKeyDown;
        }

        base.OnApplyTemplate();

        _input = GetTemplateChild(PartInput) as TextBox;
        _chipsList = GetTemplateChild(PartChips) as ItemsControl;

        if (_input is not null)
        {
            _input.PreviewKeyDown += OnInputPreviewKeyDown;
            _input.TextChanged += OnInputTextChanged;
            _input.LostFocus += OnInputLostFocus;
        }

        if (_chipsList is not null)
        {
            _chipsList.PreviewKeyDown += OnChipsPreviewKeyDown;
        }

        RefreshChips();
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusTagInputAutomationPeer(this);

    // ---- Public state machine (directly unit-testable without a template) ----

    /// <summary>
    /// Commits <paramref name="raw"/> through the contract pipeline; fires TagAdded/TagRejected/
    /// ValueChanged. Returns the outcome. No-op (Empty) when disabled.
    /// </summary>
    public TagCommitStatus CommitText(string raw)
    {
        if (!IsEnabled)
        {
            return TagCommitStatus.Empty;
        }

        var (status, text, tags) = TagInputEngine.TryCommit(
            Tags, raw, Transform, Validate, AllowDuplicates, MaxTags);

        switch (status)
        {
            case TagCommitStatus.Committed:
                SetTags(tags);
                TagAdded?.Invoke(this, text);
                break;
            case TagCommitStatus.Duplicate:
            case TagCommitStatus.TooMany:
            case TagCommitStatus.Rejected:
                TagRejected?.Invoke(this, text);
                break;
        }

        return status;
    }

    /// <summary>
    /// Removes the chip at <paramref name="index"/>; highlight (and focus, when a chip had it)
    /// moves to the next adjacent chip, or back to the field when the list empties.
    /// </summary>
    public void RemoveTagAt(int index)
    {
        if (!IsEnabled || index < 0 || index >= Tags.Count)
        {
            return;
        }

        var removed = Tags[index];
        var focusFollows = HighlightedIndex >= 0;
        var next = TagInputEngine.RemoveAt(Tags, index);
        SetTags(next);

        HighlightedIndex = TagInputEngine.HighlightAfterRemove(index, next.Count);
        if (focusFollows)
        {
            MoveFocusToHighlight();
        }

        TagRemoved?.Invoke(this, removed);
    }

    /// <summary>The empty-field Backspace second press: removes the highlighted chip and returns focus to the field.</summary>
    public void RemoveHighlighted()
    {
        var index = HighlightedIndex;
        if (!IsEnabled || index < 0 || index >= Tags.Count)
        {
            return;
        }

        var removed = Tags[index];
        SetTags(TagInputEngine.RemoveAt(Tags, index));
        HighlightedIndex = -1;
        _input?.Focus();
        TagRemoved?.Invoke(this, removed);
    }

    /// <summary>Sets the highlight (-1 = field); when <paramref name="focus"/>, also moves real keyboard focus.</summary>
    public void Highlight(int index, bool focus)
    {
        HighlightedIndex = index;
        if (focus)
        {
            MoveFocusToHighlight();
        }
    }

    // ---- Field wiring ----

    private void OnInputPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!IsEnabled || _input is null)
        {
            return;
        }

        var text = _input.Text;

        if (text.Length > 0 && IsKeyDelimiter(e.Key))
        {
            CommitText(text);
            ClearInput();
            // Enter is consumed; Tab still moves focus (the web does not preventDefault Tab).
            e.Handled = e.Key != Key.Tab;
            return;
        }

        if (text.Length > 0)
        {
            return;
        }

        if (e.Key == Key.Back)
        {
            if (HighlightedIndex < 0 && Tags.Count > 0)
            {
                Highlight(Tags.Count - 1, focus: false);
            }
            else if (HighlightedIndex >= 0)
            {
                RemoveHighlighted();
            }

            e.Handled = true;
        }
        else if (e.Key == Key.Left && Tags.Count > 0)
        {
            Highlight(Tags.Count - 1, focus: true);
            e.Handled = true;
        }
    }

    private void OnInputTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressTextChanged || !IsEnabled || _input is null)
        {
            return;
        }

        var (candidates, remainder) = TagInputEngine.Split(_input.Text, EffectiveDelimiters);
        if (candidates.Count > 0)
        {
            foreach (var candidate in candidates)
            {
                CommitText(candidate);
            }

            SetInputText(remainder);
        }
        else if (HighlightedIndex >= 0)
        {
            // Typing exits chip navigation (the web's Highlight(-1, false) on input).
            HighlightedIndex = -1;
        }
    }

    private void OnInputLostFocus(object sender, RoutedEventArgs e)
    {
        if (AddOnBlur && _input is not null && _input.Text.Length > 0)
        {
            CommitText(_input.Text);
            ClearInput();
        }
    }

    // ---- Chip wiring (roving focus; keys handled on the chips host) ----

    private void OnChipsPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!IsEnabled || HighlightedIndex < 0)
        {
            return;
        }

        var i = HighlightedIndex;
        var last = Tags.Count - 1;

        switch (e.Key)
        {
            case Key.Left:
                if (i > 0)
                {
                    Highlight(i - 1, focus: true);
                }

                e.Handled = true;
                break;
            case Key.Right:
                if (i < last)
                {
                    Highlight(i + 1, focus: true);
                }
                else
                {
                    Highlight(-1, focus: true); // past the last chip -> back to the field.
                }

                e.Handled = true;
                break;
            case Key.Home:
                Highlight(0, focus: true);
                e.Handled = true;
                break;
            case Key.End:
                Highlight(last, focus: true);
                e.Handled = true;
                break;
            case Key.Delete:
            case Key.Back:
                RemoveTagAt(i);
                e.Handled = true;
                break;
        }
    }

    private void OnRemoveTagExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is TagChipVm chip)
        {
            RemoveTagAt(chip.Index);
        }
    }

    // ---- Internals ----

    private bool IsKeyDelimiter(Key key) => key switch
    {
        Key.Enter => EffectiveDelimiters.Contains(TagDelimiter.Enter),
        Key.Tab => EffectiveDelimiters.Contains(TagDelimiter.Tab),
        _ => false,
    };

    private void SetTags(IReadOnlyList<string> tags)
    {
        _syncingValue = true;
        try
        {
            Value = tags;
        }
        finally
        {
            _syncingValue = false;
        }

        RefreshChips();
        ValueChanged?.Invoke(this, tags);
    }

    private void RefreshChips()
    {
        var tags = Tags;
        var chips = new List<TagChipVm>(tags.Count);
        for (var i = 0; i < tags.Count; i++)
        {
            chips.Add(new TagChipVm(tags[i], i) { IsHighlighted = i == HighlightedIndex });
        }

        SetValue(ChipsPropertyKey, (IReadOnlyList<TagChipVm>)chips);
        SetValue(IsTagListEmptyPropertyKey, tags.Count == 0);
    }

    private void RefreshHighlightFlags()
    {
        var chips = Chips;
        if (chips is null)
        {
            return;
        }

        for (var i = 0; i < chips.Count; i++)
        {
            chips[i].IsHighlighted = i == HighlightedIndex;
        }
    }

    private void MoveFocusToHighlight()
    {
        if (HighlightedIndex < 0)
        {
            _input?.Focus();
            return;
        }

        if (_chipsList?.ItemContainerGenerator.ContainerFromIndex(HighlightedIndex) is UIElement container)
        {
            container.Focusable = true;
            container.Focus();
        }
    }

    private void ClearInput() => SetInputText(string.Empty);

    private void SetInputText(string text)
    {
        if (_input is null)
        {
            return;
        }

        _suppressTextChanged = true;
        try
        {
            _input.Text = text;
            _input.CaretIndex = text.Length;
        }
        finally
        {
            _suppressTextChanged = false;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_defaultValueApplied)
        {
            return;
        }

        _defaultValueApplied = true;

        // DefaultValue seeds only when no controlled Value was supplied (web precedence).
        if (Value is null && DefaultValue is { } initial)
        {
            SetTags(new List<string>(initial));
        }
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NaviusTagInput)d;
        if (control._syncingValue)
        {
            return;
        }

        // External (binding-driven) change: clamp the highlight and rebuild chips (web OnParametersSet).
        if (control.HighlightedIndex >= control.Tags.Count)
        {
            control.HighlightedIndex = control.Tags.Count - 1;
        }

        control.RefreshChips();
    }

    private static void OnHighlightedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusTagInput)d).RefreshHighlightFlags();
}

/// <summary>
/// The root's UIA peer. The committed tags live in template text (chips), so they must be exposed
/// over a pattern (the Select-peer / M3-gate precedent): a read-only ValuePattern surfaces the
/// comma-joined tag list. ControlType.Group, since the interactive edit inside is a real TextBox
/// with its own Edit peer.
/// </summary>
internal sealed class NaviusTagInputAutomationPeer : FrameworkElementAutomationPeer, IValueProvider
{
    public NaviusTagInputAutomationPeer(NaviusTagInput owner) : base(owner)
    {
    }

    private NaviusTagInput Root => (NaviusTagInput)Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override string GetClassNameCore() => nameof(NaviusTagInput);

    public override object? GetPattern(PatternInterface patternInterface) =>
        patternInterface == PatternInterface.Value ? this : base.GetPattern(patternInterface);

    public bool IsReadOnly => true;

    public string Value => string.Join(", ", Root.Value ?? Array.Empty<string>());

    public void SetValue(string value) =>
        throw new InvalidOperationException("NaviusTagInput is read-only over ValuePattern; commit tags via the field.");
}
