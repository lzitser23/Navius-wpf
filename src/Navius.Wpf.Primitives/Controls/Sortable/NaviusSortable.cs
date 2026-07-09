using System.Collections.Specialized;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Navius.Wpf.Primitives.Controls.Sortable;

/// <summary>
/// Tier B (custom lookless control): an <see cref="ItemsControl"/> (NOT a Selector, since Sortable
/// has no "selected" concept) that owns the ordered key list and drives the contract's two
/// interaction models, the APG "grab and move" keyboard reducer and native WPF pointer drag. See
/// docs/parity/sortable.md "WPF strategy". The pure grab-and-move state transitions live in
/// <see cref="SortableKeyboardReducer"/> so they are unit-testable without an STA Application;
/// this control just wires PreviewKeyDown and DragDrop to them and reorders its own item containers.
///
/// Deviation from the web contract: the visually-hidden <c>role="status" aria-live</c> announcer div
/// is replaced by <see cref="AutomationPeer.RaiseNotificationEvent"/> (the same WPF-native swap the
/// Toast family makes) for grab/move/drop/cancel announcements. Cross-list transfer between separate
/// containers is out of scope, matching the contract. See docs/parity/sortable.md
/// "WPF implementation notes".
/// </summary>
public class NaviusSortable : ItemsControl
{
    /// <summary>DataObject format used to carry the dragged <see cref="NaviusSortableItem"/> reference.</summary>
    internal const string DragFormat = "Navius.Wpf.Sortable.Item";

    public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
        nameof(Values), typeof(IReadOnlyList<string>), typeof(NaviusSortable),
        new PropertyMetadata(null, OnValuesChanged));

    public static readonly DependencyProperty DefaultValuesProperty = DependencyProperty.Register(
        nameof(DefaultValues), typeof(IReadOnlyList<string>), typeof(NaviusSortable), new PropertyMetadata(null));

    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation), typeof(NaviusSortableOrientation), typeof(NaviusSortable),
        new PropertyMetadata(NaviusSortableOrientation.Vertical));

    public static readonly DependencyProperty DisabledProperty = DependencyProperty.Register(
        nameof(Disabled), typeof(bool), typeof(NaviusSortable),
        new PropertyMetadata(false, OnDisabledChanged));

    public static readonly DependencyProperty IsDraggingProperty = DependencyProperty.Register(
        nameof(IsDragging), typeof(bool), typeof(NaviusSortable), new PropertyMetadata(false));

    public static readonly RoutedEvent ValuesChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValuesChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NaviusSortable));

    public static readonly RoutedEvent OnReorderEvent = EventManager.RegisterRoutedEvent(
        nameof(OnReorder), RoutingStrategy.Bubble,
        typeof(EventHandler<SortableReorderEventArgs>), typeof(NaviusSortable));

    private bool _initialized;
    private bool _isSyncing;
    private string? _activeKey;
    private string? _grabbedKey;
    private int _oldIndexAtGrab = -1;
    private IReadOnlyList<string>? _orderAtGrab;

    static NaviusSortable()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusSortable), new FrameworkPropertyMetadata(typeof(NaviusSortable)));
    }

    public NaviusSortable()
    {
        Focusable = false;
        AllowDrop = true;
        PreviewKeyDown += OnPreviewKeyDown;
        Loaded += (_, _) => EnsureInitialized();
    }

    /// <summary>Ordered item keys (controlled; bind for @bind-Values parity). Mirrors the item order.</summary>
    public IReadOnlyList<string>? Values
    {
        get => (IReadOnlyList<string>?)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    /// <summary>Uncontrolled initial order; consulted once at load when <see cref="Values"/> is unset.</summary>
    public IReadOnlyList<string>? DefaultValues
    {
        get => (IReadOnlyList<string>?)GetValue(DefaultValuesProperty);
        set => SetValue(DefaultValuesProperty, value);
    }

    /// <summary>Drives pointer-drag nearest-slot math; keyboard nav stays linear for every value.</summary>
    public NaviusSortableOrientation Orientation
    {
        get => (NaviusSortableOrientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>When true, both keyboard reorder and pointer drag are inert (contract's data-disabled).</summary>
    public bool Disabled
    {
        get => (bool)GetValue(DisabledProperty);
        set => SetValue(DisabledProperty, value);
    }

    /// <summary>True while a pointer drag or keyboard grab is active (contract's root data-dragging).</summary>
    public bool IsDragging
    {
        get => (bool)GetValue(IsDraggingProperty);
        set => SetValue(IsDraggingProperty, value);
    }

    /// <summary>Fired on every committed order mutation: pointer drop, keyboard move, keyboard drop, Escape restore.</summary>
    public event RoutedEventHandler ValuesChanged
    {
        add => AddHandler(ValuesChangedEvent, value);
        remove => RemoveHandler(ValuesChangedEvent, value);
    }

    /// <summary>Fired once per committed reorder (pointer or keyboard drop) when the item's index actually changed.</summary>
    public event EventHandler<SortableReorderEventArgs> OnReorder
    {
        add => AddHandler(OnReorderEvent, value);
        remove => RemoveHandler(OnReorderEvent, value);
    }

    /// <summary>The key currently keyboard-grabbed, or null when not grabbing (exposed for tests).</summary>
    public string? GrabbedKey => _grabbedKey;

    protected override bool IsItemItsOwnContainerOverride(object item) => item is NaviusSortableItem;

    protected override DependencyObject GetContainerForItemOverride() => new NaviusSortableItem();

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusSortableAutomationPeer(this);

    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);
        if (_isSyncing || !_initialized)
        {
            return;
        }

        // Item added/removed after seeding: keep Values in step with the live container order.
        SetCurrentValue(ValuesProperty, CurrentOrder());
        RefreshItemStates();
    }

    // --- Keyboard: APG grab-and-move, testable without real KeyEventArgs ---

    /// <summary>
    /// Routes one key for the given focused item per the contract's keyboard table; returns whether
    /// it was consumed. Public (rather than internal) for direct unit-testability, the same tradeoff
    /// NaviusRating.HandleKey makes.
    /// </summary>
    public bool HandleItemKey(NaviusSortableItem item, Key key)
    {
        if (Disabled || !IsEnabled)
        {
            return false;
        }

        var grabbing = _grabbedKey is not null;
        var isRtl = FlowDirection == FlowDirection.RightToLeft;

        switch (key)
        {
            case Key.Space:
            case Key.Enter:
                return grabbing ? DropGrabbed() : Grab(item);
            case Key.Escape:
                return grabbing && CancelGrab();
            case Key.Down:
                return grabbing ? MoveGrabbed(SortableMove.Forward) : RoveFocus(item, SortableMove.Forward);
            case Key.Up:
                return grabbing ? MoveGrabbed(SortableMove.Backward) : RoveFocus(item, SortableMove.Backward);
            case Key.Right:
                return Step(item, grabbing, isRtl ? SortableMove.Backward : SortableMove.Forward);
            case Key.Left:
                return Step(item, grabbing, isRtl ? SortableMove.Forward : SortableMove.Backward);
            case Key.Home:
                return grabbing ? MoveGrabbed(SortableMove.First) : RoveFocus(item, SortableMove.First);
            case Key.End:
                return grabbing ? MoveGrabbed(SortableMove.Last) : RoveFocus(item, SortableMove.Last);
            default:
                return false;
        }
    }

    /// <summary>Grabs the focused item (records the original order for Escape/OnReorder). Returns false if it cannot grab.</summary>
    public bool Grab(NaviusSortableItem item)
    {
        if (Disabled || !IsEnabled || item.Disabled || _grabbedKey is not null)
        {
            return false;
        }

        _orderAtGrab = CurrentOrder();
        _oldIndexAtGrab = _orderAtGrab.ToList().IndexOf(item.Value);
        _grabbedKey = item.Value;
        _activeKey = item.Value;
        IsDragging = true;
        RefreshItemStates();
        FocusKey(item.Value);
        Announce($"Grabbed {item.AccessibleLabel}. Use the arrow keys to move, space to drop, escape to cancel.");
        return true;
    }

    /// <summary>Moves the grabbed item one enabled slot (or to first/last). Fires ValuesChanged, never OnReorder.</summary>
    public bool MoveGrabbed(SortableMove move)
    {
        if (_grabbedKey is null)
        {
            return false;
        }

        var order = CurrentOrder();
        var index = order.ToList().IndexOf(_grabbedKey);
        var result = SortableKeyboardReducer.Move(order, IsKeyDisabled, index, move);
        if (!result.Moved)
        {
            return true; // consumed; nowhere to move
        }

        ApplyOrderInternal(result.Order);
        SetCurrentValue(ValuesProperty, result.Order.ToList());
        RaiseValuesChanged();
        RefreshItemStates();
        FocusKey(_grabbedKey);

        var (position, count) = PositionOf(_grabbedKey);
        var item = ItemFor(_grabbedKey);
        Announce($"Moved {item?.AccessibleLabel ?? _grabbedKey}. Position {position} of {count}.");
        return true;
    }

    /// <summary>Commits the grab: clears grab state and fires OnReorder if the index changed.</summary>
    public bool DropGrabbed()
    {
        if (_grabbedKey is null)
        {
            return false;
        }

        var key = _grabbedKey;
        var newIndex = CurrentOrder().ToList().IndexOf(key);
        var oldIndex = _oldIndexAtGrab;
        var item = ItemFor(key);

        _grabbedKey = null;
        IsDragging = false;
        _orderAtGrab = null;
        _oldIndexAtGrab = -1;
        RefreshItemStates();
        FocusKey(key);

        var (position, count) = PositionOf(key);
        Announce($"Dropped {item?.AccessibleLabel ?? key}. Position {position} of {count}.");

        if (oldIndex >= 0 && newIndex >= 0 && oldIndex != newIndex)
        {
            RaiseReorder(oldIndex, newIndex);
        }

        return true;
    }

    /// <summary>Cancels the grab: restores the order captured at grab time; never fires OnReorder.</summary>
    public bool CancelGrab()
    {
        if (_grabbedKey is null)
        {
            return false;
        }

        var key = _grabbedKey;
        var restore = _orderAtGrab;
        _grabbedKey = null;
        IsDragging = false;
        _oldIndexAtGrab = -1;

        if (restore is not null && !restore.SequenceEqual(CurrentOrder()))
        {
            ApplyOrderInternal(restore);
            SetCurrentValue(ValuesProperty, restore.ToList());
            RaiseValuesChanged();
        }

        _orderAtGrab = null;
        RefreshItemStates();
        FocusKey(key);

        var item = ItemFor(key);
        var (position, count) = PositionOf(key);
        Announce($"Reorder cancelled. {item?.AccessibleLabel ?? key} returned to position {position} of {count}.");
        return true;
    }

    private bool Step(NaviusSortableItem item, bool grabbing, SortableMove move) =>
        grabbing ? MoveGrabbed(move) : RoveFocus(item, move);

    private bool RoveFocus(NaviusSortableItem item, SortableMove move)
    {
        var order = CurrentOrder();
        var index = order.ToList().IndexOf(item.Value);
        var target = SortableKeyboardReducer.Rove(order, IsKeyDisabled, index, move);
        _activeKey = order[target];
        RefreshItemStates();
        FocusKey(_activeKey);
        return true;
    }

    // --- Pointer drag ---

    protected override void OnDragOver(DragEventArgs e)
    {
        base.OnDragOver(e);
        if (Disabled || !e.Data.GetDataPresent(DragFormat))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        e.Effects = DragDropEffects.Move;
        e.Handled = true;
        UpdateDropTarget(NearestIndex(e.GetPosition(this)));
    }

    protected override void OnDragLeave(DragEventArgs e)
    {
        base.OnDragLeave(e);
        UpdateDropTarget(-1);
    }

    protected override void OnDrop(DragEventArgs e)
    {
        base.OnDrop(e);
        UpdateDropTarget(-1);

        if (Disabled || !e.Data.GetDataPresent(DragFormat) ||
            e.Data.GetData(DragFormat) is not NaviusSortableItem dragged)
        {
            return;
        }

        // Cross-list transfer is out of scope: ignore a drop whose source is another container.
        if (!Items.Contains(dragged))
        {
            return;
        }

        var order = CurrentOrder();
        var oldIndex = order.ToList().IndexOf(dragged.Value);
        var targetIndex = NearestIndex(e.GetPosition(this));
        if (oldIndex < 0 || targetIndex < 0 || targetIndex == oldIndex)
        {
            return;
        }

        var newOrder = new List<string>(order);
        newOrder.Remove(dragged.Value);
        var targetKey = order[targetIndex];
        var pos = newOrder.IndexOf(targetKey);
        if (pos < 0)
        {
            pos = newOrder.Count;
        }

        var insertAt = oldIndex < targetIndex ? pos + 1 : pos;
        insertAt = Math.Clamp(insertAt, 0, newOrder.Count);
        newOrder.Insert(insertAt, dragged.Value);

        ApplyOrderInternal(newOrder);
        SetCurrentValue(ValuesProperty, newOrder);
        RaiseValuesChanged();
        RefreshItemStates();
        FocusKey(dragged.Value);

        var (position, count) = PositionOf(dragged.Value);
        Announce($"Dropped {dragged.AccessibleLabel}. Position {position} of {count}.");

        var newIndex = newOrder.IndexOf(dragged.Value);
        if (newIndex != oldIndex)
        {
            RaiseReorder(oldIndex, newIndex);
        }

        e.Handled = true;
    }

    /// <summary>
    /// Nearest realized item to a point by Euclidean distance between the point and each item's
    /// center. This is the pragmatic single heuristic for every orientation: for Vertical/Horizontal
    /// the off-axis distance is near constant so the on-axis coordinate dominates, and for
    /// <see cref="NaviusSortableOrientation.Grid"/> it is exactly the doc's "nearest cell by 2D
    /// distance" requirement.
    /// </summary>
    private int NearestIndex(Point point)
    {
        var order = CurrentOrder();
        var best = -1;
        var bestDistance = double.MaxValue;
        for (var i = 0; i < order.Count; i++)
        {
            var item = ItemFor(order[i]);
            if (item is null || !item.IsVisible)
            {
                continue;
            }

            Point center;
            try
            {
                center = item.TranslatePoint(new Point(item.ActualWidth / 2, item.ActualHeight / 2), this);
            }
            catch (InvalidOperationException)
            {
                continue; // not in this control's visual tree
            }

            var dx = center.X - point.X;
            var dy = center.Y - point.Y;
            var distance = (dx * dx) + (dy * dy);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        return best;
    }

    private void UpdateDropTarget(int targetIndex)
    {
        var order = CurrentOrder();
        for (var i = 0; i < order.Count; i++)
        {
            ItemFor(order[i])?.SetCurrentValue(NaviusSortableItem.IsDropTargetProperty, i == targetIndex);
        }
    }

    // --- Order plumbing ---

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        var seed = Values ?? DefaultValues;
        var current = CurrentOrder();

        if (seed is { Count: > 0 })
        {
            var ordered = seed.Where(current.Contains).ToList();
            foreach (var key in current)
            {
                if (!ordered.Contains(key))
                {
                    ordered.Add(key);
                }
            }

            ApplyOrderInternal(ordered);
            SetCurrentValue(ValuesProperty, ordered);
        }
        else
        {
            SetCurrentValue(ValuesProperty, current.ToList());
        }

        _activeKey = FirstEnabledKey();
        RefreshItemStates();
    }

    private static void OnValuesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var sortable = (NaviusSortable)d;
        if (sortable._isSyncing || !sortable._initialized)
        {
            return;
        }

        if (e.NewValue is IReadOnlyList<string> keys)
        {
            sortable.ApplyOrderInternal(keys);
            sortable.RefreshItemStates();
        }
    }

    private static void OnDisabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusSortable)d).RefreshItemStates();

    /// <summary>Reorders the item containers in-place to match <paramref name="keys"/>. Guarded against re-entrancy.</summary>
    private void ApplyOrderInternal(IReadOnlyList<string> keys)
    {
        _isSyncing = true;
        try
        {
            for (var target = 0; target < keys.Count; target++)
            {
                var found = -1;
                for (var j = target; j < Items.Count; j++)
                {
                    if (Items[j] is NaviusSortableItem candidate && candidate.Value == keys[target])
                    {
                        found = j;
                        break;
                    }
                }

                if (found >= 0 && found != target)
                {
                    var moved = Items[found];
                    Items.RemoveAt(found);
                    Items.Insert(target, moved);
                }
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }

    /// <summary>Pushes roving-tabindex, grabbed, and PositionInSet/SizeOfSet onto every item.</summary>
    internal void RefreshItemStates()
    {
        var order = CurrentOrder();
        var count = order.Count;

        if (_activeKey is null || !order.Contains(_activeKey) || IsKeyDisabled(_activeKey))
        {
            _activeKey = FirstEnabledKey();
        }

        var tabStopKey = _grabbedKey ?? _activeKey;

        for (var i = 0; i < order.Count; i++)
        {
            var item = ItemFor(order[i]);
            if (item is null)
            {
                continue;
            }

            item.SetCurrentValue(AutomationProperties.PositionInSetProperty, i + 1);
            item.SetCurrentValue(AutomationProperties.SizeOfSetProperty, count);
            item.SetCurrentValue(NaviusSortableItem.IsKeyboardGrabbedProperty, order[i] == _grabbedKey);
            item.IsTabStop = order[i] == tabStopKey && !item.Disabled && !Disabled;
        }
    }

    private IReadOnlyList<string> CurrentOrder()
    {
        var order = new List<string>(Items.Count);
        foreach (var raw in Items)
        {
            if (raw is NaviusSortableItem item)
            {
                order.Add(item.Value);
            }
        }

        return order;
    }

    private NaviusSortableItem? ItemFor(string key)
    {
        foreach (var raw in Items)
        {
            if (raw is NaviusSortableItem item && item.Value == key)
            {
                return item;
            }
        }

        return null;
    }

    private bool IsKeyDisabled(string key) => ItemFor(key)?.Disabled ?? false;

    private string? FirstEnabledKey()
    {
        var order = CurrentOrder();
        var index = SortableKeyboardReducer.FirstEnabled(order, IsKeyDisabled);
        return index < 0 ? null : order[index];
    }

    /// <summary>1-based position of a key over all items (disabled included), and the total item count.</summary>
    private (int Position, int Count) PositionOf(string key)
    {
        var order = CurrentOrder();
        return (order.ToList().IndexOf(key) + 1, order.Count);
    }

    private void FocusKey(string? key)
    {
        if (key is null)
        {
            return;
        }

        ItemFor(key)?.Focus();
    }

    private void RaiseValuesChanged() => RaiseEvent(new RoutedEventArgs(ValuesChangedEvent, this));

    private void RaiseReorder(int oldIndex, int newIndex) =>
        RaiseEvent(new SortableReorderEventArgs(OnReorderEvent, this, oldIndex, newIndex));

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var item = FindItemFromSource(e.OriginalSource as DependencyObject);
        if (item is null)
        {
            return;
        }

        if (HandleItemKey(item, e.Key))
        {
            e.Handled = true;
        }
    }

    private NaviusSortableItem? FindItemFromSource(DependencyObject? source)
    {
        var current = source;
        while (current is not null && current != this)
        {
            if (current is NaviusSortableItem item)
            {
                return item;
            }

            current = current is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(current)
                : LogicalTreeHelper.GetParent(current);
        }

        return null;
    }

    /// <summary>
    /// Contract calls for a hidden aria-live announcer region; this raises the WPF-native UIA
    /// notification event instead (see the class remarks and the Toast family's identical swap).
    /// Requires Windows 10 1709+ and a listening AT; a no-op otherwise.
    /// </summary>
    private void Announce(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var peer = UIElementAutomationPeer.FromElement(this) ?? UIElementAutomationPeer.CreatePeerForElement(this);
        peer?.RaiseNotificationEvent(
            AutomationNotificationKind.Other,
            AutomationNotificationProcessing.CurrentThenMostRecent,
            text,
            Guid.NewGuid().ToString());
    }
}
