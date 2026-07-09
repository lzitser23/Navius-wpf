using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Navius.Wpf.Primitives.Controls.Tabs;

/// <summary>
/// Tier A: derives from the native TabControl, which already ships
/// TabControlAutomationPeer/TabItemAutomationPeer mapping to UIA SelectionPattern /
/// role="tab" / role="tabpanel" for free. The contract's four-part split
/// (NaviusTabs/NaviusTabsList/NaviusTabsTab/NaviusTabsPanel) collapses to two WPF types:
/// this root (owning the List's arrow-key navigation too, since WPF's TabPanel is not a
/// separately addressable public part) and NaviusTabItem (unifying Tab trigger + Panel
/// content, since TabItem.Header/Content already live on one object). See tabs.md's WPF
/// implementation notes for the full collapse rationale, including why KeepMounted is
/// dropped.
///
/// WPF's own directional navigation does not know about Loop, ActivationMode, or RTL
/// mirroring, so it is switched off entirely and arrow/Home/End/Enter/Space are
/// reimplemented here, the same "own the keyboard model explicitly" shape as
/// NaviusRadioGroup.
/// </summary>
public class NaviusTabs : TabControl
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(string),
        typeof(NaviusTabs),
        new PropertyMetadata(null, OnValueChanged));

    public static readonly DependencyProperty ActivationModeProperty = DependencyProperty.Register(
        nameof(ActivationMode),
        typeof(string),
        typeof(NaviusTabs),
        new PropertyMetadata("automatic"));

    public static readonly DependencyProperty LoopProperty = DependencyProperty.Register(
        nameof(Loop),
        typeof(bool),
        typeof(NaviusTabs),
        new PropertyMetadata(true));

    public static readonly DependencyProperty DirProperty = DependencyProperty.Register(
        nameof(Dir),
        typeof(string),
        typeof(NaviusTabs),
        new PropertyMetadata(null));

    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation),
        typeof(string),
        typeof(NaviusTabs),
        new PropertyMetadata("horizontal"));

    private static readonly DependencyPropertyKey ActivationDirectionPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(ActivationDirection),
        typeof(string),
        typeof(NaviusTabs),
        new PropertyMetadata("none"));

    public static readonly DependencyProperty ActivationDirectionProperty = ActivationDirectionPropertyKey.DependencyProperty;

    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueChanged),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(NaviusTabs));

    private bool _isSyncing;

    static NaviusTabs()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusTabs),
            new FrameworkPropertyMetadata(typeof(NaviusTabs)));
    }

    public NaviusTabs()
    {
        KeyboardNavigation.SetDirectionalNavigation(this, KeyboardNavigationMode.None);
        PreviewKeyDown += HandlePreviewKeyDown;
    }

    /// <summary>Controlled/uncontrolled selected tab value; also driven by native selection (click).</summary>
    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>"automatic": focusing a tab via keyboard activates it. "manual": arrows move focus only.</summary>
    public string ActivationMode
    {
        get => (string)GetValue(ActivationModeProperty);
        set => SetValue(ActivationModeProperty, value);
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

    /// <summary>"horizontal" or "vertical"; drives arrow-key axis (no native TabControl equivalent).</summary>
    public string Orientation
    {
        get => (string)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>Read-only: one of "none"/"left"/"right"/"up"/"down", computed from the old/new selected index.</summary>
    public string ActivationDirection => (string)GetValue(ActivationDirectionProperty);

    public event RoutedEventHandler ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);

        // XAML sets the Value attribute before child TabItems are parsed into Items, so an
        // uncontrolled/initial Value would otherwise never find its match; re-sync as items
        // arrive so a matching late-added item still gets selected.
        SyncSelectionFromValue();
    }

    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        base.OnSelectionChanged(e);

        if (_isSyncing)
        {
            return;
        }

        var newValue = (SelectedItem as NaviusTabItem)?.Value;
        ComputeActivationDirection(Value, newValue);

        if (!string.Equals(Value, newValue, StringComparison.Ordinal))
        {
            _isSyncing = true;
            try
            {
                Value = newValue;
            }
            finally
            {
                _isSyncing = false;
            }

            RaiseEvent(new RoutedEventArgs(ValueChangedEvent, this));
        }
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusTabs)d).SyncSelectionFromValue();

    private void SyncSelectionFromValue()
    {
        if (_isSyncing)
        {
            return;
        }

        var target = Items.OfType<NaviusTabItem>()
            .FirstOrDefault(i => string.Equals(i.Value, Value, StringComparison.Ordinal));

        if (target is not null && !ReferenceEquals(SelectedItem, target))
        {
            _isSyncing = true;
            try
            {
                SelectedItem = target;
            }
            finally
            {
                _isSyncing = false;
            }
        }
    }

    private void ComputeActivationDirection(string? oldValue, string? newValue)
    {
        var items = Items.OfType<NaviusTabItem>().ToList();
        var oldIndex = items.FindIndex(i => string.Equals(i.Value, oldValue, StringComparison.Ordinal));
        var newIndex = items.FindIndex(i => string.Equals(i.Value, newValue, StringComparison.Ordinal));

        if (oldIndex < 0 || newIndex < 0 || oldIndex == newIndex)
        {
            SetValue(ActivationDirectionPropertyKey, "none");
            return;
        }

        var isVertical = string.Equals(Orientation, "vertical", StringComparison.OrdinalIgnoreCase);
        var forward = newIndex > oldIndex;
        SetValue(ActivationDirectionPropertyKey, isVertical
            ? (forward ? "down" : "up")
            : (forward ? "right" : "left"));
    }

    private void HandlePreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!IsEnabled)
        {
            return;
        }

        var tabs = Items.OfType<NaviusTabItem>().Where(t => t.IsEnabled).ToList();
        if (tabs.Count == 0)
        {
            return;
        }

        if (e.Key is Key.Enter or Key.Space)
        {
            if (FocusManager.GetFocusedElement(this) is NaviusTabItem { IsEnabled: true } focused)
            {
                focused.IsSelected = true;
                e.Handled = true;
            }

            return;
        }

        var isVertical = string.Equals(Orientation, "vertical", StringComparison.OrdinalIgnoreCase);
        var isRtl = string.Equals(Dir, "rtl", StringComparison.OrdinalIgnoreCase)
            || (Dir is null && FlowDirection == FlowDirection.RightToLeft);

        NaviusTabItem? target = e.Key switch
        {
            Key.Right when !isVertical => Move(tabs, isRtl ? -1 : 1),
            Key.Left when !isVertical => Move(tabs, isRtl ? 1 : -1),
            Key.Down when isVertical => Move(tabs, 1),
            Key.Up when isVertical => Move(tabs, -1),
            Key.Home => tabs.FirstOrDefault(),
            Key.End => tabs.LastOrDefault(),
            _ => null,
        };

        if (target is null)
        {
            return;
        }

        FocusManager.SetFocusedElement(this, target);

        // Automatic mode: moving focus also selects the target (native TabControl's own
        // default behavior, reproduced explicitly since directional navigation is off).
        // Manual mode: only roving focus moves; selection requires Enter/Space/click.
        if (!string.Equals(ActivationMode, "manual", StringComparison.OrdinalIgnoreCase))
        {
            target.IsSelected = true;
        }

        e.Handled = true;
    }

    private NaviusTabItem? Move(List<NaviusTabItem> tabs, int delta)
    {
        var focused = FocusManager.GetFocusedElement(this) as NaviusTabItem;
        var currentIndex = focused is null ? tabs.IndexOf(SelectedItem as NaviusTabItem ?? tabs[0]) : tabs.IndexOf(focused);
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        var index = currentIndex;
        for (var step = 0; step < tabs.Count; step++)
        {
            index += delta;

            if (Loop)
            {
                index = ((index % tabs.Count) + tabs.Count) % tabs.Count;
            }
            else if (index < 0 || index >= tabs.Count)
            {
                return null;
            }

            if (tabs[index].IsEnabled)
            {
                return tabs[index];
            }
        }

        return null;
    }
}
