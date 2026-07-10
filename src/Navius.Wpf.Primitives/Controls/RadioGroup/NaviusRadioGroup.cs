using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Navius.Wpf.Primitives.Controls.RadioGroup;

/// <summary>
/// Tier B (custom lookless control). Native RadioButton + GroupName gives basic mutual
/// exclusion and automatic directional navigation, but not Loop=false clamping, ReadOnly,
/// RTL-mirrored horizontal arrows, or an undefined-by-default Orientation, so this
/// reimplements the contract's group-owns-keyboard-model as an explicit PreviewKeyDown
/// handler on a lookless ContentControl (WPF's own directional navigation is switched off
/// via KeyboardNavigation.DirectionalNavigation="None" so it cannot fight this handler).
///
/// Disabled is not reimplemented as a new property: WPF's IsEnabled already cascades to
/// descendants, so disabling the group disables every item for free (and each item's own
/// IsEnabled combines with it automatically, matching the contract's
/// "IsDisabled = Disabled || Context.Disabled").
///
/// Orientation is kept as a string? property for API parity with the contract, but has no
/// behavioral effect: the contract's own keyboard table maps ArrowDown/Right to "next" and
/// ArrowUp/Left to "prev" unconditionally, and WPF has no aria-orientation equivalent to
/// drive from it; child layout is entirely up to how the consumer arranges its content.
/// </summary>
public class NaviusRadioGroup : ContentControl
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(string),
        typeof(NaviusRadioGroup),
        new PropertyMetadata(null, OnValueChanged));

    public static readonly DependencyProperty RequiredProperty = DependencyProperty.Register(
        nameof(Required),
        typeof(bool),
        typeof(NaviusRadioGroup),
        new PropertyMetadata(false));

    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation),
        typeof(string),
        typeof(NaviusRadioGroup),
        new PropertyMetadata(null));

    public static readonly DependencyProperty LoopProperty = DependencyProperty.Register(
        nameof(Loop),
        typeof(bool),
        typeof(NaviusRadioGroup),
        new PropertyMetadata(true));

    public static readonly DependencyProperty DirProperty = DependencyProperty.Register(
        nameof(Dir),
        typeof(string),
        typeof(NaviusRadioGroup),
        new PropertyMetadata(null));

    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueChanged),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(NaviusRadioGroup));

    private bool _isSyncing;

    static NaviusRadioGroup()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusRadioGroup),
            new FrameworkPropertyMetadata(typeof(NaviusRadioGroup)));
    }

    public NaviusRadioGroup()
    {
        Focusable = false;
        KeyboardNavigation.SetDirectionalNavigation(this, KeyboardNavigationMode.None);
        AddHandler(ToggleButton.CheckedEvent, new RoutedEventHandler(OnItemChecked));
        PreviewKeyDown += HandlePreviewKeyDown;
    }

    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool Required
    {
        get => (bool)GetValue(RequiredProperty);
        set => SetValue(RequiredProperty, value);
    }

    /// <summary>Undefined by default; never forced. See class remarks.</summary>
    public string? Orientation
    {
        get => (string?)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public bool Loop
    {
        get => (bool)GetValue(LoopProperty);
        set => SetValue(LoopProperty, value);
    }

    /// <summary>Reading direction; falls back to FlowDirection when unset.</summary>
    public string? Dir
    {
        get => (string?)GetValue(DirProperty);
        set => SetValue(DirProperty, value);
    }

    public event RoutedEventHandler ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        SyncCheckedFromValue();
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new NaviusRadioGroupAutomationPeer(this);

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusRadioGroup)d).SyncCheckedFromValue();

    private void OnItemChecked(object sender, RoutedEventArgs e)
    {
        if (_isSyncing || e.OriginalSource is not NaviusRadioGroupItem item)
        {
            return;
        }

        if (!string.Equals(Value, item.Value, StringComparison.Ordinal))
        {
            Value = item.Value;
            RaiseEvent(new RoutedEventArgs(ValueChangedEvent, this));
        }

        UpdateRovingTabStops();
    }

    private void SyncCheckedFromValue()
    {
        if (_isSyncing)
        {
            return;
        }

        _isSyncing = true;
        try
        {
            foreach (var item in FindDescendants<NaviusRadioGroupItem>(this))
            {
                item.IsChecked = string.Equals(item.Value, Value, StringComparison.Ordinal);
            }
        }
        finally
        {
            _isSyncing = false;
        }

        UpdateRovingTabStops();
    }

    private void UpdateRovingTabStops()
    {
        var items = FindDescendants<NaviusRadioGroupItem>(this).ToList();
        if (items.Count == 0)
        {
            return;
        }

        var tabStop = items.FirstOrDefault(i => i.IsChecked == true && i.IsEnabled)
            ?? items.FirstOrDefault(i => i.IsEnabled);

        foreach (var item in items)
        {
            item.IsTabStop = ReferenceEquals(item, tabStop);
        }
    }

    // Named distinctly from UIElement.OnPreviewKeyDown (a different, single-argument
    // virtual method) so reflection-based lookups by name stay unambiguous.
    private void HandlePreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!IsEnabled)
        {
            return;
        }

        var items = FindDescendants<NaviusRadioGroupItem>(this).ToList();
        if (items.Count == 0)
        {
            return;
        }

        var isRtl = string.Equals(Dir, "rtl", StringComparison.OrdinalIgnoreCase)
            || (Dir is null && FlowDirection == FlowDirection.RightToLeft);

        NaviusRadioGroupItem? target = e.Key switch
        {
            Key.Down => Move(items, 1),
            Key.Up => Move(items, -1),
            Key.Right => Move(items, isRtl ? -1 : 1),
            Key.Left => Move(items, isRtl ? 1 : -1),
            Key.Home => items.FirstOrDefault(i => i.IsEnabled),
            Key.End => items.LastOrDefault(i => i.IsEnabled),
            _ => null,
        };

        if (target is null)
        {
            return;
        }

        target.IsChecked = true;
        target.Focus();
        e.Handled = true;
    }

    private NaviusRadioGroupItem? Move(List<NaviusRadioGroupItem> items, int delta)
    {
        var currentIndex = items.FindIndex(i => i.IsKeyboardFocused || i.IsChecked == true);
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        var index = currentIndex;
        for (var step = 0; step < items.Count; step++)
        {
            index += delta;

            if (Loop)
            {
                index = ((index % items.Count) + items.Count) % items.Count;
            }
            else if (index < 0 || index >= items.Count)
            {
                return null;
            }

            if (items[index].IsEnabled)
            {
                return items[index];
            }
        }

        return null;
    }

    /// <summary>
    /// Walks the logical tree (not visual) so descendants are discoverable immediately
    /// after Content is assigned, without requiring a layout pass or a live
    /// PresentationSource (matters for unit tests and for XAML parse-time ordering).
    /// </summary>
    private static IEnumerable<T> FindDescendants<T>(DependencyObject root) where T : DependencyObject
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is not DependencyObject childObj)
            {
                continue;
            }

            if (childObj is T match)
            {
                yield return match;
            }

            foreach (var descendant in FindDescendants<T>(childObj))
            {
                yield return descendant;
            }
        }
    }
}

internal sealed class NaviusRadioGroupAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusRadioGroupAutomationPeer(NaviusRadioGroup owner) : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override string GetClassNameCore() => nameof(NaviusRadioGroup);
}
