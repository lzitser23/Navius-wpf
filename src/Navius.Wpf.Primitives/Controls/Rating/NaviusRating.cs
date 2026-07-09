using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Tier B (custom lookless control), APG radio-set model: NaviusRating owns the authoritative
/// decimal? value plus a transient hover-preview value, builds Max NaviusRatingItem stars into a
/// PART_ItemsHost panel, and drives selection/hover/roving-tabindex centrally -- mirroring how
/// NaviusRadioGroup owns its NaviusRadioGroupItem children rather than each item owning its own
/// selection state (see docs/parity/rating.md "WPF strategy"). Step/clamp/select math is factored
/// into the pure, unit-testable NaviusRatingMath.
///
/// Deviation: the contract's ChildContent override (custom star glyph content, replacing the
/// auto-generated stars) is dropped -- WPF has no equivalent to a Blazor RenderFragment slot
/// without a bespoke items-template system, so the star glyph is fixed (styleable via
/// Themes/Rating.xaml) rather than consumer-replaceable per-instance. See
/// docs/parity/rating.md "WPF implementation notes".
/// </summary>
[TemplatePart(Name = PartItemsHost, Type = typeof(Panel))]
public class NaviusRating : Control
{
    private const string PartItemsHost = "PART_ItemsHost";

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(decimal?), typeof(NaviusRating), new PropertyMetadata(null, OnValueChanged));

    public static readonly DependencyProperty MaxProperty = DependencyProperty.Register(
        nameof(Max), typeof(int), typeof(NaviusRating), new PropertyMetadata(5, OnMaxChanged, CoerceMax));

    public static readonly DependencyProperty AllowHalfProperty = DependencyProperty.Register(
        nameof(AllowHalf), typeof(bool), typeof(NaviusRating), new PropertyMetadata(false));

    public static readonly DependencyProperty AllowClearProperty = DependencyProperty.Register(
        nameof(AllowClear), typeof(bool), typeof(NaviusRating), new PropertyMetadata(true));

    public static readonly DependencyProperty ReadOnlyProperty = DependencyProperty.Register(
        nameof(ReadOnly), typeof(bool), typeof(NaviusRating), new PropertyMetadata(false));

    public static readonly DependencyProperty RequiredProperty = DependencyProperty.Register(
        nameof(Required), typeof(bool), typeof(NaviusRating), new PropertyMetadata(false));

    public static readonly DependencyProperty InvalidProperty = DependencyProperty.Register(
        nameof(Invalid), typeof(bool), typeof(NaviusRating), new PropertyMetadata(false));

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(Func<decimal, string>), typeof(NaviusRating), new PropertyMetadata(null));

    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueChanged), RoutingStrategy.Bubble,
        typeof(RoutedPropertyChangedEventHandler<decimal?>), typeof(NaviusRating));

    private Panel? _itemsHost;
    private decimal? _hoverValue;

    static NaviusRating()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusRating), new FrameworkPropertyMetadata(typeof(NaviusRating)));
    }

    public NaviusRating()
    {
        // Roving tabindex lives on the items (see RefreshItemStates), matching NaviusRadioGroup.
        Focusable = false;
        PreviewKeyDown += OnPreviewKeyDown;
        AddHandler(NaviusRatingItem.SelectRequestedEvent, new EventHandler<NaviusRatingSelectEventArgs>(OnItemSelectRequested));
        AddHandler(NaviusRatingItem.HoverChangedEvent, new EventHandler<NaviusRatingSelectEventArgs>(OnItemHoverChanged));
    }

    /// <summary>Controlled value; use a Binding for @bind-Value parity.</summary>
    public decimal? Value
    {
        get => (decimal?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Number of visual stars; coerced to &gt;= 1.</summary>
    public int Max
    {
        get => (int)GetValue(MaxProperty);
        set => SetValue(MaxProperty, value);
    }

    public bool AllowHalf
    {
        get => (bool)GetValue(AllowHalfProperty);
        set => SetValue(AllowHalfProperty, value);
    }

    public bool AllowClear
    {
        get => (bool)GetValue(AllowClearProperty);
        set => SetValue(AllowClearProperty, value);
    }

    /// <summary>Focusable but the value cannot be changed (contract's ReadOnly).</summary>
    public bool ReadOnly
    {
        get => (bool)GetValue(ReadOnlyProperty);
        set => SetValue(ReadOnlyProperty, value);
    }

    public bool Required
    {
        get => (bool)GetValue(RequiredProperty);
        set => SetValue(RequiredProperty, value);
    }

    public bool Invalid
    {
        get => (bool)GetValue(InvalidProperty);
        set => SetValue(InvalidProperty, value);
    }

    /// <summary>Accessible-name factory per star value; defaults to NaviusRating.DefaultLabel when unset.</summary>
    public Func<decimal, string>? Label
    {
        get => (Func<decimal, string>?)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public event RoutedPropertyChangedEventHandler<decimal?> ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    /// <summary>"1 star" (singular) or "N stars", the contract's default accessible-name text.</summary>
    public static string DefaultLabel(decimal value) => value == 1m ? "1 star" : $"{value} stars";

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _itemsHost = GetTemplateChild(PartItemsHost) as Panel;
        RebuildItems();
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusRatingAutomationPeer(this);

    /// <summary>
    /// Handles one key exactly per the contract's keyboard table (see docs/parity/rating.md
    /// "Keyboard"); returns whether the key was consumed. Public (rather than the more natural
    /// `internal`) so it is directly unit-testable without constructing real KeyEventArgs, the
    /// same tradeoff NaviusProgress.FormatValueText() makes elsewhere in this codebase.
    /// </summary>
    public bool HandleKey(Key key)
    {
        if (!IsEnabled || ReadOnly)
        {
            return false;
        }

        var isRtl = FlowDirection == FlowDirection.RightToLeft;

        switch (key)
        {
            case Key.Up:
                Value = NaviusRatingMath.StepUp(Value, AllowHalf, Max);
                return true;
            case Key.Right when !isRtl:
            case Key.Left when isRtl:
                Value = NaviusRatingMath.StepUp(Value, AllowHalf, Max);
                return true;
            case Key.Down:
                Value = NaviusRatingMath.StepDown(Value, AllowHalf, AllowClear);
                return true;
            case Key.Left when !isRtl:
            case Key.Right when isRtl:
                Value = NaviusRatingMath.StepDown(Value, AllowHalf, AllowClear);
                return true;
            case Key.Home:
                Value = 1m;
                return true;
            case Key.End:
                Value = Max;
                return true;
            case Key.Back:
            case Key.Delete:
                if (AllowClear)
                {
                    Value = null;
                }

                return true;
            case Key.D1 or Key.D2 or Key.D3 or Key.D4 or Key.D5 or Key.D6 or Key.D7 or Key.D8 or Key.D9:
                Value = NaviusRatingMath.Digit(key - Key.D0, Max);
                return true;
            case Key.Space:
            case Key.Enter:
                // Activates (selects) the focused star. In the web contract this falls out of the
                // item being a native <button>; here NaviusRatingItem is a plain Control, so it must
                // be wired explicitly. The focused star in the roving model is always the one at
                // FocusIndex(Value), so selecting its whole Index value (re-selecting clears when
                // AllowClear) matches native button-click semantics.
                Value = NaviusRatingMath.Select(NaviusRatingMath.FocusIndex(Value, Max), Value, AllowClear);
                return true;
            default:
                return false;
        }
    }

    /// <summary>Click/Space-Enter equivalent: selects the item's whole value (never a half value).</summary>
    internal void SelectItem(NaviusRatingItem item)
    {
        if (!IsEnabled || ReadOnly)
        {
            return;
        }

        Value = NaviusRatingMath.Select(item.Index, Value, AllowClear);
    }

    internal NaviusRatingItem? GetItem(int oneBasedIndex)
    {
        if (_itemsHost is null || oneBasedIndex < 1 || oneBasedIndex > _itemsHost.Children.Count)
        {
            return null;
        }

        return _itemsHost.Children[oneBasedIndex - 1] as NaviusRatingItem;
    }

    private static void OnMaxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusRating)d).RebuildItems();

    private static object CoerceMax(DependencyObject d, object baseValue) => Math.Max(1, (int)baseValue);

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var rating = (NaviusRating)d;
        rating.RefreshItemStates();
        rating.RaiseEvent(new RoutedPropertyChangedEventArgs<decimal?>(
            (decimal?)e.OldValue, (decimal?)e.NewValue, ValueChangedEvent));
    }

    private void RebuildItems()
    {
        if (_itemsHost is null)
        {
            return;
        }

        _itemsHost.Children.Clear();
        for (var i = 1; i <= Max; i++)
        {
            _itemsHost.Children.Add(new NaviusRatingItem { Index = i });
        }

        RefreshItemStates();
    }

    private void RefreshItemStates()
    {
        if (_itemsHost is null)
        {
            return;
        }

        var effective = _hoverValue ?? Value ?? 0m;
        var tabStopIndex = NaviusRatingMath.FocusIndex(Value, Max);

        foreach (var child in _itemsHost.Children)
        {
            if (child is not NaviusRatingItem item)
            {
                continue;
            }

            item.SetCurrentValue(NaviusRatingItem.FillStateProperty, FillStateFor(item.Index, effective));
            item.SetCurrentValue(NaviusRatingItem.IsHighlightedProperty, _hoverValue is not null);
            item.IsTabStop = item.Index == tabStopIndex;
        }
    }

    private static NaviusRatingFillState FillStateFor(int index, decimal effective)
    {
        if (effective >= index)
        {
            return NaviusRatingFillState.Full;
        }

        if (effective >= index - 0.5m)
        {
            return NaviusRatingFillState.Half;
        }

        return NaviusRatingFillState.Empty;
    }

    private void OnItemSelectRequested(object? sender, NaviusRatingSelectEventArgs e)
    {
        if (!IsEnabled || ReadOnly || e.Source is not NaviusRatingItem item)
        {
            return;
        }

        var candidate = AllowHalf && e.IsHalf == true ? item.Index - 0.5m : item.Index;
        Value = NaviusRatingMath.Select(candidate, Value, AllowClear);
        item.Focus();
    }

    private void OnItemHoverChanged(object? sender, NaviusRatingSelectEventArgs e)
    {
        if (!IsEnabled || ReadOnly)
        {
            return;
        }

        if (e.IsHalf is null)
        {
            _hoverValue = null;
        }
        else if (e.Source is NaviusRatingItem item)
        {
            _hoverValue = AllowHalf && e.IsHalf == true ? item.Index - 0.5m : item.Index;
        }

        RefreshItemStates();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!HandleKey(e.Key))
        {
            return;
        }

        FocusResultingItem();
        e.Handled = true;
    }

    private void FocusResultingItem() => GetItem(NaviusRatingMath.FocusIndex(Value, Max))?.Focus();
}
