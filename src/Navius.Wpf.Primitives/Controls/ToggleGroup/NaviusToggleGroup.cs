using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Navius.Wpf.Primitives.Controls.Internal;

namespace Navius.Wpf.Primitives.Controls.ToggleGroup;

/// <summary>
/// Tier B (lookless custom control), following the same "root owns state, discovers Item
/// descendants via the logical tree" shape as NaviusRadioGroup. Single mode keeps native
/// ToggleButton toggle-off-by-clicking-again behavior (unlike RadioButton, a ToggleButton
/// can already go from checked back to unchecked on its own), so mutual exclusion is the
/// only thing this root adds on top for single mode: when one item becomes checked, every
/// other item is unchecked. That combination reproduces the contract's ComputeNext exactly
/// ("clicking the pressed item clears the selection; clicking a different item replaces
/// it") without any deselect-override logic of its own.
///
/// Unlike NaviusRadioGroup, arrow keys here only move focus (never selection) per the
/// contract's own keyboard table; Space/Enter (see NaviusToggleGroupItem) is what actually
/// toggles. RovingFocus=false drops the roving-tabstop/arrow-key model entirely and makes
/// every enabled item independently tabbable, since WPF's ListBox-style composite-widget
/// navigation has no per-item "opt out" and a plain items collection is a better fit for
/// that mode than trying to bend a Selector to it.
///
/// Disabled is not reimplemented as its own named parameter: the root's native IsEnabled
/// is reused, explicitly pushed down onto every item (see the constructor), since WPF's
/// IsEnabled does not automatically cascade through a ContentControl's logical Content.
/// </summary>
public class NaviusToggleGroup : ContentControl
{
    public static readonly DependencyProperty MultipleProperty = DependencyProperty.Register(
        nameof(Multiple),
        typeof(bool),
        typeof(NaviusToggleGroup),
        new PropertyMetadata(false));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(IReadOnlyList<string>),
        typeof(NaviusToggleGroup),
        new PropertyMetadata(Array.Empty<string>(), OnValueChanged));

    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation),
        typeof(string),
        typeof(NaviusToggleGroup),
        new PropertyMetadata("horizontal"));

    public static readonly DependencyProperty RovingFocusProperty = DependencyProperty.Register(
        nameof(RovingFocus),
        typeof(bool),
        typeof(NaviusToggleGroup),
        new PropertyMetadata(true, OnRovingFocusChanged));

    public static readonly DependencyProperty LoopProperty = DependencyProperty.Register(
        nameof(Loop),
        typeof(bool),
        typeof(NaviusToggleGroup),
        new PropertyMetadata(true));

    public static readonly DependencyProperty DirProperty = DependencyProperty.Register(
        nameof(Dir),
        typeof(string),
        typeof(NaviusToggleGroup),
        new PropertyMetadata(null));

    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueChanged),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(NaviusToggleGroup));

    private bool _isSyncing;

    static NaviusToggleGroup()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusToggleGroup),
            new FrameworkPropertyMetadata(typeof(NaviusToggleGroup)));
    }

    public NaviusToggleGroup()
    {
        Focusable = false;
        KeyboardNavigation.SetDirectionalNavigation(this, KeyboardNavigationMode.None);
        AddHandler(ToggleButton.CheckedEvent, new RoutedEventHandler(OnItemChecked));
        AddHandler(ToggleButton.UncheckedEvent, new RoutedEventHandler(OnItemUnchecked));
        PreviewKeyDown += HandlePreviewKeyDown;

        // UIElement.IsEnabled does not automatically cascade through a ContentControl's
        // logical Content the way it does through a Panel's Children, so it is pushed down
        // onto every item explicitly. Known simplification: unlike the contract's
        // "Disabled || Context.Disabled" (a per-item disabled flag independent of the
        // group), re-enabling the group also re-enables every item, so an item disabled
        // only for its own reasons does not survive a group disable/enable cycle.
        IsEnabledChanged += (_, _) =>
        {
            foreach (var item in LogicalTreeWalker.Descendants<NaviusToggleGroupItem>(this))
            {
                item.IsEnabled = IsEnabled;
            }

            UpdateRovingTabStops();
        };
    }

    /// <summary>true: many items can be pressed at once. false (default): single, radio-like.</summary>
    public bool Multiple
    {
        get => (bool)GetValue(MultipleProperty);
        set => SetValue(MultipleProperty, value);
    }

    /// <summary>Controlled pressed set.</summary>
    public IReadOnlyList<string> Value
    {
        get => (IReadOnlyList<string>)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>"horizontal" or "vertical"; drives the roving-focus arrow-key axis.</summary>
    public string Orientation
    {
        get => (string)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>true (default): single Tab stop, arrows move roving focus. false: every enabled item is independently tabbable.</summary>
    public bool RovingFocus
    {
        get => (bool)GetValue(RovingFocusProperty);
        set => SetValue(RovingFocusProperty, value);
    }

    public bool Loop
    {
        get => (bool)GetValue(LoopProperty);
        set => SetValue(LoopProperty, value);
    }

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
        new NaviusToggleGroupAutomationPeer(this);

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusToggleGroup)d).SyncCheckedFromValue();

    private static void OnRovingFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusToggleGroup)d).UpdateRovingTabStops();

    private void OnItemChecked(object sender, RoutedEventArgs e)
    {
        if (_isSyncing || e.OriginalSource is not NaviusToggleGroupItem item)
        {
            return;
        }

        if (Multiple)
        {
            if (!Value.Contains(item.Value))
            {
                ApplyValue(Value.Append(item.Value).ToList());
            }
        }
        else
        {
            _isSyncing = true;
            try
            {
                foreach (var other in LogicalTreeWalker.Descendants<NaviusToggleGroupItem>(this))
                {
                    if (!ReferenceEquals(other, item))
                    {
                        other.IsChecked = false;
                    }
                }
            }
            finally
            {
                _isSyncing = false;
            }

            ApplyValue(new List<string> { item.Value });
        }

        UpdateRovingTabStops();
    }

    private void OnItemUnchecked(object sender, RoutedEventArgs e)
    {
        if (_isSyncing || e.OriginalSource is not NaviusToggleGroupItem item)
        {
            return;
        }

        ApplyValue(Value.Where(v => !string.Equals(v, item.Value, StringComparison.Ordinal)).ToList());
        UpdateRovingTabStops();
    }

    private void ApplyValue(List<string> next)
    {
        if (Value.SequenceEqual(next))
        {
            return;
        }

        Value = next;
        RaiseEvent(new RoutedEventArgs(ValueChangedEvent, this));
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
            foreach (var item in LogicalTreeWalker.Descendants<NaviusToggleGroupItem>(this))
            {
                item.IsChecked = Value.Contains(item.Value);
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
        var items = LogicalTreeWalker.Descendants<NaviusToggleGroupItem>(this).ToList();
        if (items.Count == 0)
        {
            return;
        }

        if (!RovingFocus)
        {
            foreach (var item in items)
            {
                item.IsTabStop = item.IsEnabled;
            }

            return;
        }

        var tabStop = items.FirstOrDefault(i => i.IsChecked == true && i.IsEnabled)
            ?? items.FirstOrDefault(i => i.IsEnabled);

        foreach (var item in items)
        {
            item.IsTabStop = ReferenceEquals(item, tabStop);
        }
    }

    private void HandlePreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!IsEnabled || !RovingFocus)
        {
            return;
        }

        var items = LogicalTreeWalker.Descendants<NaviusToggleGroupItem>(this).Where(i => i.IsEnabled).ToList();
        if (items.Count == 0)
        {
            return;
        }

        var isVertical = string.Equals(Orientation, "vertical", StringComparison.OrdinalIgnoreCase);
        var isRtl = string.Equals(Dir, "rtl", StringComparison.OrdinalIgnoreCase)
            || (Dir is null && FlowDirection == FlowDirection.RightToLeft);

        NaviusToggleGroupItem? target = e.Key switch
        {
            Key.Right when !isVertical => Move(items, isRtl ? -1 : 1),
            Key.Left when !isVertical => Move(items, isRtl ? 1 : -1),
            Key.Down when isVertical => Move(items, 1),
            Key.Up when isVertical => Move(items, -1),
            Key.Home => items.FirstOrDefault(),
            Key.End => items.LastOrDefault(),
            _ => null,
        };

        if (target is null)
        {
            return;
        }

        foreach (var item in items)
        {
            item.IsTabStop = ReferenceEquals(item, target);
        }

        FocusManager.SetFocusedElement(this, target);
        e.Handled = true;
    }

    private NaviusToggleGroupItem? Move(List<NaviusToggleGroupItem> items, int delta)
    {
        var focused = FocusManager.GetFocusedElement(this) as NaviusToggleGroupItem;
        var currentIndex = focused is null ? -1 : items.IndexOf(focused);
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
}

internal sealed class NaviusToggleGroupAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusToggleGroupAutomationPeer(NaviusToggleGroup owner) : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override string GetClassNameCore() => nameof(NaviusToggleGroup);
}
